using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using BandFounder.Infrastructure.Errors;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;

namespace BandFounder.Application.Services;

public interface ISpotifyConnectionService
{
    Task LinkAccountToSpotify(SpotifyConnectionDto dto, Guid userId);
    Task CreateSpotifyTokens(SpotifyTokensDto spotifyTokens, Guid userId);
    Task<SpotifyTokensDto> GetSpotifyTokens(Guid userId);
    Task<string> GetAccessTokenAsync(Guid userId);
    Task<List<SpotifyArtistDto>> SaveRelevantArtists(Guid userId);
    Task<List<SpotifyArtistDto>> GetTopArtistsAsync(Guid userId, int limit = 50);
    Task<List<SpotifyArtistDto>> GetFollowedArtistsAsync(Guid userId);
}

public class SpotifyConnectionService(
    ISpotifyClient spotifyClient,
    IRepository<SpotifyTokens> spotifyTokensRepository,
    IRepository<Artist> artistRepository,
    IRepository<Account> accountRepository,
    IRepository<Genre> genreRepository)
    : ISpotifyConnectionService
{
    public async Task LinkAccountToSpotify(SpotifyConnectionDto dto, Guid userId)
    {
        var spotifyAppCredentials = await new SpotifyAppCredentialsService().LoadCredentials();
        
        var tokensResponse = await spotifyClient.RequestAccessTokenAsync(dto, spotifyAppCredentials);
        
        var tokensDto = new SpotifyTokensDto
        {
            AccessToken = tokensResponse.AccessToken,
            RefreshToken = tokensResponse.RefreshToken,
            ExpirationDate = DateTime.UtcNow.AddSeconds(tokensResponse.ExpiresIn - 60)
        };
        
        await CreateSpotifyTokens(tokensDto, userId);
        await SaveRelevantArtists(userId);
    }
    
    public async Task CreateSpotifyTokens(SpotifyTokensDto spotifyTokens, Guid userId)
    {
        var account = await accountRepository.GetOneRequiredAsync(userId);
        
        var existingTokens = await spotifyTokensRepository.GetOneAsync(userId);
        if (existingTokens is not null)
        {
            throw new SpotifyAccountAlreadyConnectedException();
        }
        
        var newSpotifyTokens = new SpotifyTokens
        {
            AccountId = account.Id,
            AccessToken = spotifyTokens.AccessToken,
            RefreshToken = spotifyTokens.RefreshToken,
            ExpirationDate = spotifyTokens.ExpirationDate,
            Account = account
        };
        
        await spotifyTokensRepository.CreateAsync(newSpotifyTokens);
        await spotifyTokensRepository.SaveChangesAsync();
    }
    
    public async Task<SpotifyTokensDto> GetSpotifyTokens(Guid userId)
    {
        var spotifyTokens = await spotifyTokensRepository.GetOneAsync(userId);
        if (spotifyTokens is null)
        {
            throw new SpotifyAccountNotLinkedError();
        }

        return spotifyTokens.ToDto();
    }

    public async Task<string> GetAccessTokenAsync(Guid userId)
    {
        var spotifyTokens = await spotifyTokensRepository.GetOneAsync(userId);
        if (spotifyTokens is null)
        {
            throw new SpotifyAccountNotLinkedError();
        }

        // Check if the stored token is still valid
        if (DateTime.UtcNow < spotifyTokens.ExpirationDate)
        {
            return spotifyTokens.AccessToken;
        }
        else
        {
            return await RefreshTokenAsync(userId, spotifyTokens.RefreshToken);
        }
    }
    
    private async Task<string> RefreshTokenAsync(Guid userId, string refreshToken)
    {
        var spotifyAppCredentials = await new SpotifyAppCredentialsService().LoadCredentials();
        
        var refreshedTokens = await spotifyClient.RefreshTokenAsync(refreshToken, spotifyAppCredentials);
        
        await UpdateRefreshedAccessTokenAsync(userId, refreshedTokens.AccessToken, refreshedTokens.ExpiresIn);
        
        return refreshedTokens.AccessToken;
    }

    private async Task UpdateRefreshedAccessTokenAsync(Guid userId, string accessToken, int duration)
    {
        var spotifyTokens = await spotifyTokensRepository.GetOneRequiredAsync(userId);

        spotifyTokens.AccessToken = accessToken;
        spotifyTokens.ExpirationDate = DateTime.UtcNow.AddSeconds(duration - 60);

        await spotifyTokensRepository.SaveChangesAsync();
    }

    public async Task<List<SpotifyArtistDto>> SaveRelevantArtists(Guid userId)
    {
        var userArtists = await RetrieveSpotifyUsersArtistsAsync(userId);
        var account = await accountRepository.GetOneRequiredAsync(key: userId,
            keyPropertyName: nameof(Account.Id), includeProperties: nameof(Account.Artists));
        
        var savedArtists = new List<SpotifyArtistDto>();

        foreach (var artistDto in userArtists)
        {
            var artistEntity = await artistRepository.GetOrCreateAsync(genreRepository,
                artistDto.Name, artistDto.Genres, artistDto.Popularity, artistDto.Id);

            if (account.Artists.All(artist => artist.Id != artistEntity.Id))
            {
                account.Artists.Add(artistEntity);
                savedArtists.Add(artistDto);
            }
        }

        await accountRepository.SaveChangesAsync();
        return savedArtists;
    }

    public async Task<List<SpotifyArtistDto>> RetrieveSpotifyUsersArtistsAsync(Guid userId)
    {
        var topArtists = await GetTopArtistsAsync(userId);
        var followedArtists = await GetFollowedArtistsAsync(userId);
        
        return topArtists.Concat(followedArtists).DistinctBy(artist => artist.Id).ToList();
    }

    public async Task<List<SpotifyArtistDto>> GetTopArtistsAsync(Guid userId, int limit = 50)
    {
        var accessToken = await GetAccessTokenAsync(userId);
        return await spotifyClient.GetTopArtistsAsync(accessToken, limit);
    }

    public async Task<List<SpotifyArtistDto>> GetFollowedArtistsAsync(Guid userId)
    {
        var accessToken = await GetAccessTokenAsync(userId);
        return await spotifyClient.GetFollowedArtistsAsync(accessToken);
    }
}