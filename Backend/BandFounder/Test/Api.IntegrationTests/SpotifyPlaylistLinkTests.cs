using System.Net;
using System.Net.Http.Json;
using Api.IntegrationTests.Infrastructure;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using BandFounder.Infrastructure;
using BandFounder.Infrastructure.Spotify;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

[TestFixture]
public class SpotifyPlaylistLinkTests : IntegrationTestBase
{
    [Test]
    public async Task Link_TenthPlaylist_ReturnsLinked()
    {
        var token = await RegisterAsync("linktenth", "linktenth@example.com");
        AuthenticateAs(token);
        var me = await ReadJsonAsync<AccountDto>(await Client.GetAsync("/api/accounts/me"));
        var accountId = Guid.Parse(me.Id);
        await SeedLinkedPlaylistsAsync(accountId, count: LinkedPlaylist.MaxPerAccount - 1);

        var response = await Client.PostAsJsonAsync("/api/spotify/playlists/link", new
        {
            input = StubSpotifyPlaylists.Url(LinkedPlaylist.MaxPerAccount - 1)
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
    }

    [Test]
    public async Task Link_TwoParallelRequestsAtCap_ReturnLinkedAndCapReached()
    {
        var token = await RegisterAsync("linkcap", "linkcap@example.com");
        AuthenticateAs(token);
        var me = await ReadJsonAsync<AccountDto>(await Client.GetAsync("/api/accounts/me"));
        var accountId = Guid.Parse(me.Id);

        await SeedLinkedPlaylistsAsync(accountId, count: LinkedPlaylist.MaxPerAccount - 1);

        var first = Client.PostAsJsonAsync("/api/spotify/playlists/link", new
        {
            input = StubSpotifyPlaylists.Url(LinkedPlaylist.MaxPerAccount - 1)
        });
        var second = Client.PostAsJsonAsync("/api/spotify/playlists/link", new
        {
            input = StubSpotifyPlaylists.Url(LinkedPlaylist.MaxPerAccount)
        });

        var responses = await Task.WhenAll(first, second);
        var statuses = responses.Select(response => response.StatusCode).ToArray();

        Assert.That(statuses, Is.EquivalentTo(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));

        var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        var conflictBody = await conflict.Content.ReadAsStringAsync();
        Assert.That(conflictBody, Does.Contain(SpotifyPlaylistLinkErrors.CapReached));

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
        var linkedCount = await dbContext.LinkedPlaylists.CountAsync(playlist => playlist.AccountId == accountId);
        Assert.That(linkedCount, Is.EqualTo(LinkedPlaylist.MaxPerAccount));
    }

    [Test]
    public async Task Link_WhenPersistFailsAfterCatalogWrites_RollsBackArtistAndGenre()
    {
        var token = await RegisterAsync("linkrollback", "linkrollback@example.com");
        AuthenticateAs(token);

        var playlistId = RandomPlaylistId();
        var artistId = StubSpotifyPlaylists.ArtistId(playlistId);
        var genreName = StubSpotifyPlaylists.GenreName(artistId).NormalizeName();

        LinkedPlaylistPersistFault.FailNextLinkedPlaylistSave();

        var response = await Client.PostAsJsonAsync("/api/spotify/playlists/link", new
        {
            input = $"https://open.spotify.com/playlist/{playlistId}"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
        Assert.That(await dbContext.Artists.AnyAsync(artist => artist.Id == artistId), Is.False);
        Assert.That(await dbContext.Genres.AnyAsync(genre => genre.Name == genreName), Is.False);
        Assert.That(await dbContext.LinkedPlaylists.AnyAsync(), Is.False);
    }

    private async Task SeedLinkedPlaylistsAsync(Guid accountId, int count)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BandFounderDbContext>();
        var account = await dbContext.Accounts.SingleAsync(item => item.Id == accountId);
        for (var i = 0; i < count; i++)
        {
            dbContext.LinkedPlaylists.Add(new LinkedPlaylist
            {
                AccountId = accountId,
                Account = account,
                SpotifyId = StubSpotifyPlaylists.Id(i),
                Name = $"Existing {i}",
                LinkedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static string RandomPlaylistId()
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return string.Create(SpotifyPlaylistId.Length, alphabet, static (span, chars) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = chars[Random.Shared.Next(chars.Length)];
            }
        });
    }
}
