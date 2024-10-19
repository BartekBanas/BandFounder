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
}