using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Dtos.Chatrooms;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Application.Dtos.Messages;
using BandFounder.Domain.Entities;
using ArtistDto = BandFounder.Application.Dtos.Metadata.ArtistDto;

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
                    Status = slot.Status.ToString(),
                    AssigneeId = slot.AssigneeId
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
    
    public static IEnumerable<ChatroomDto> ToDto(this IEnumerable<Chatroom> chatrooms)
    {
        return chatrooms.Select(chatroom => chatroom.ToDto());
    }

    public static ChatroomDto ToDto(this Chatroom chatroom)
    {
        return new ChatroomDto()
        {
            Id = chatroom.Id,
            Type = chatroom.ChatRoomType,
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