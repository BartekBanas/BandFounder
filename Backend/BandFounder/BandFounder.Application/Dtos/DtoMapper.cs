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
}