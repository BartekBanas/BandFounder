using BandFounder.Domain.Entities;

namespace BandFounder.Infrastructure.Spotify.Dto;

public static class DtoMappingExtensions
{
    public static SpotifyTokensDto ToDto(this SpotifyTokens spotifyTokens)
    {
        return new SpotifyTokensDto
        {
            AccessToken = spotifyTokens.AccessToken,
            RefreshToken = spotifyTokens.RefreshToken,
            ExpirationDate = spotifyTokens.ExpirationDate
        };
    }

    public static IEnumerable<SpotifyTokensDto> ToDto(this IEnumerable<SpotifyTokens> spotifyTokens)
    {
        return spotifyTokens.Select(tokens => tokens.ToDto());
    }
}