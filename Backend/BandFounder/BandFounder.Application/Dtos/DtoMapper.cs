using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Dtos.Messages;
using BandFounder.Application.Dtos.Spotify;
using BandFounder.Domain.Entities;

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

    public static IEnumerable<AccountDto> ToDto(this IEnumerable<Account> accounts)
    {
        return accounts.Select(account => account.ToDto());
    }

    public static SpotifyCredentialsDto ToDto(this SpotifyCredentials spotifyCredentials)
    {
        return new SpotifyCredentialsDto
        {
            AccessToken = spotifyCredentials.AccessToken,
            RefreshToken = spotifyCredentials.RefreshToken,
            ExpirationDate = spotifyCredentials.ExpirationDate
        };
    }

    public static IEnumerable<SpotifyCredentialsDto> ToDto(this IEnumerable<SpotifyCredentials> spotifyCredentials)
    {
        return spotifyCredentials.Select(credentials => credentials.ToDto());
    }

    public static IEnumerable<MusicProjectListingDto> ToDto(this IEnumerable<MusicProjectListing> musicProjectListings)
    {
        return musicProjectListings.Select(listing => listing.ToDto());
    }

    public static MusicProjectListingDto ToDto(this MusicProjectListing musicProjectListing)
    {
        return new MusicProjectListingDto
        {
            Id = musicProjectListing.Id,
            Name = musicProjectListing.Name,
            GenreName = musicProjectListing.GenreName,
            Description = musicProjectListing.Description,
            Type = musicProjectListing.Type.ToString(),
            MusicianSlots = musicProjectListing.MusicianSlots
                .Select(slot => new MusicianSlotDto
                {
                    Id = slot.Id,
                    Role = slot.Role.RoleName,
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
}