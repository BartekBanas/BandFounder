using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Dtos.Messages;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure.Spotify.Dto;
using ArtistDto = BandFounder.Application.Dtos.Listings.ArtistDto;

namespace BandFounder.Application.Dtos;

public static class DtoMapper
{
    public static AccountDto ToDto(this Account account)
    {
        return new AccountDto
        {
            Id = account.Id.ToString(),
            Name = account.Name,
            Email = account.Email
        };
    }

    public static IEnumerable<AccountDetailedDto> ToDetailedDto(this IEnumerable<Account> accounts)
    {
        return accounts.Select(account => account.ToDetailedDto());
    }
    
    public static AccountDetailedDto ToDetailedDto(this Account account)
    {
        return new AccountDetailedDto
        {
            Name = account.Name,
            Email = account.Email,
            SpotifyTokens = account.SpotifyTokens is not null ? new SpotifyTokensDto()
            {
                AccessToken = account.SpotifyTokens.AccessToken,
                RefreshToken = account.SpotifyTokens.RefreshToken,
                ExpirationDate = account.SpotifyTokens.ExpirationDate
            } : null,
            MusicianRoles = account.MusicianRoles.Select(role => role.Name).ToList(),
            Artists = account.Artists.Select(artist => artist.Name).ToList()
        };
    }

    public static IEnumerable<AccountDto> ToDto(this IEnumerable<Account> accounts)
    {
        return accounts.Select(account => account.ToDto());
    }

    public static IEnumerable<ListingDto> ToDto(this IEnumerable<Listing> musicProjectListings)
    {
        return musicProjectListings.Select(listing => listing.ToDto());
    }

    public static ListingDto ToDto(this Listing listing)
    {
        return new ListingDto
        {
            Id = listing.Id,
            OwnerId = listing.OwnerId,
            Name = listing.Name,
            Genre = listing.GenreName,
            Description = listing.Description,
            Type = listing.Type.ToString(),
            MusicianSlots = listing.MusicianSlots
                .Select(slot => new MusicianSlotDto
                {
                    Id = slot.Id,
                    Role = slot.Role.Name,
                    Status = slot.Status.ToString()
                }).ToList()
        };
    }
    
    public static IEnumerable<MessageDto> ToDto(this IEnumerable<Message> messages)
    {
        return messages.Select(message => message.ToDto());
    }

    public static MessageDto ToDto(this Message message)
    {
        return new MessageDto()
        {
            Id = message.Id,
            SenderId = message.SenderId,
            Content = message.Content,
            SentDate = message.SentDate
        };
    }
    
    public static IEnumerable<ChatRoomDto> ToDto(this IEnumerable<Chatroom> chatrooms)
    {
        return chatrooms.Select(chatroom => chatroom.ToDto());
    }

    public static ChatRoomDto ToDto(this Chatroom chatroom)
    {
        return new ChatRoomDto()
        {
            Id = chatroom.Id,
            Name = chatroom.Name,
            MembersIds = chatroom.Members.Select(member => member.Id).ToList()
        };
    }
    
    public static IEnumerable<ArtistDto> ToDto(this IEnumerable<Artist> artists)
    {
        return artists.Select(artist => artist.ToDto());
    }

    public static ArtistDto ToDto(this Artist artist)
    {
        return new ArtistDto()
        {
            Id = artist.Id,
            Name = artist.Name,
            Genres = artist.Genres.Select(genre => genre.Name).ToList()
        };
    }
}