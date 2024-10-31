using BandFounder.Domain.Entities;

namespace BandFounder.Infrastructure.Spotify.Dto;

public static class DtoMappingExtensions
{
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