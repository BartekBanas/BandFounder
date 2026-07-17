using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Domain.Entities;

namespace Api.IntegrationTests;

[TestFixture]
public class ListingTests : IntegrationTestBase
{
    [Test]
    public async Task CreateListing_ThenGetById_ReturnsListing()
    {
        var token = await RegisterAsync("owner1", "owner1@example.com");
        AuthenticateAs(token);

        var createResponse = await Client.PostAsJsonAsync("/api/listings", new
        {
            name = "Rock Band",
            genre = "Rock",
            type = ListingType.Band,
            description = "Looking for members",
            musicianSlots = new[]
            {
                new { role = "Guitarist", status = SlotStatus.Available },
                new { role = "Drummer", status = SlotStatus.Available }
            }
        });

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var myListingsResponse = await Client.GetAsync("/api/listings/me");
        Assert.That(myListingsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var myListings = await ReadJsonAsync<List<ListingDto>>(myListingsResponse);
        Assert.That(myListings, Has.Count.EqualTo(1));

        var listingId = myListings[0].Id;
        var getResponse = await Client.GetAsync($"/api/listings/{listingId}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var listing = await ReadJsonAsync<ListingDto>(getResponse);
        Assert.That(listing.Name, Is.EqualTo("Rock Band"));
        Assert.That(listing.MusicianSlots, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task DeleteListing_AsNonOwner_ReturnsForbidden()
    {
        var ownerToken = await RegisterAsync("owner2", "owner2@example.com");
        AuthenticateAs(ownerToken);

        await Client.PostAsJsonAsync("/api/listings", new
        {
            name = "Jazz Project",
            genre = "Jazz",
            type = ListingType.Band,
            description = "Jazz jam",
            musicianSlots = new[]
            {
                new { role = "Pianist", status = SlotStatus.Available },
                new { role = "Bassist", status = SlotStatus.Available }
            }
        });

        var myListings = await ReadJsonAsync<List<ListingDto>>(await Client.GetAsync("/api/listings/me"));
        var listingId = myListings[0].Id;

        var otherToken = await RegisterAsync("intruder", "intruder@example.com");
        AuthenticateAs(otherToken);

        var deleteResponse = await Client.DeleteAsync($"/api/listings/{listingId}");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task ContactOwner_CreatesDirectChatroom()
    {
        var ownerToken = await RegisterAsync("owner3", "owner3@example.com");
        AuthenticateAs(ownerToken);

        await Client.PostAsJsonAsync("/api/listings", new
        {
            name = "Metal Band",
            genre = "Metal",
            type = ListingType.Band,
            description = "Heavy stuff",
            musicianSlots = new[]
            {
                new { role = "Vocalist", status = SlotStatus.Available },
                new { role = "Guitarist", status = SlotStatus.Available }
            }
        });

        var ownerListings = await ReadJsonAsync<List<ListingDto>>(await Client.GetAsync("/api/listings/me"));
        var listingId = ownerListings[0].Id;

        var seekerToken = await RegisterAsync("seeker", "seeker@example.com");
        AuthenticateAs(seekerToken);

        var contactResponse = await Client.PostAsync($"/api/listings/{listingId}/contact", null);
        Assert.That(contactResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var chatroom = await ReadJsonAsync<ChatroomDto>(contactResponse);
        Assert.That(chatroom.Type, Is.EqualTo(ChatRoomType.Direct));
        Assert.That(chatroom.MembersIds, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task UpdateListing_AsOwner_Succeeds()
    {
        var token = await RegisterAsync("owner4", "owner4@example.com");
        AuthenticateAs(token);

        await Client.PostAsJsonAsync("/api/listings", new
        {
            name = "Original Name",
            genre = "Pop",
            type = ListingType.Band,
            description = "Original",
            musicianSlots = new[]
            {
                new { role = "Vocalist", status = SlotStatus.Available },
                new { role = "Keyboardist", status = SlotStatus.Available }
            }
        });

        var myListings = await ReadJsonAsync<List<ListingDto>>(await Client.GetAsync("/api/listings/me"));
        var listing = myListings[0];

        var updateResponse = await Client.PutAsJsonAsync($"/api/listings/{listing.Id}", new
        {
            name = "Updated Name",
            genre = "Pop",
            type = ListingType.Band,
            description = "Updated description",
            musicianSlots = listing.MusicianSlots.Select(slot => new
            {
                id = slot.Id,
                role = slot.Role,
                status = Enum.Parse<SlotStatus>(slot.Status)
            })
        });

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await ReadJsonAsync<ListingDto>(await Client.GetAsync($"/api/listings/{listing.Id}"));
        Assert.That(updated.Name, Is.EqualTo("Updated Name"));
        Assert.That(updated.Description, Is.EqualTo("Updated description"));
    }

    [Test]
    public async Task GetListingsFeed_ReturnsCreatedListing()
    {
        var token = await RegisterAsync("owner5", "owner5@example.com");
        AuthenticateAs(token);

        await Client.PostAsJsonAsync("/api/listings", new
        {
            name = "Feed Listing",
            genre = "Indie",
            type = ListingType.CollaborativeSong,
            description = "Collab",
            musicianSlots = new[]
            {
                new { role = "Producer", status = SlotStatus.Available },
                new { role = "Songwriter", status = SlotStatus.Available }
            }
        });

        var otherToken = await RegisterAsync("viewer", "viewer@example.com");
        AuthenticateAs(otherToken);

        var feedResponse = await Client.GetAsync("/api/listings");
        Assert.That(feedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var feed = await ReadJsonAsync<ListingsFeedDto>(feedResponse);
        Assert.That(feed.Listings.Any(item => item.Listing.Name == "Feed Listing"), Is.True);
    }
}