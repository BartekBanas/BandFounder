using System.Text;
using System.Text.Json;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure.Errors;
using BandFounder.Infrastructure.Spotify.Dto;

namespace BandFounder.Infrastructure.Spotify.Services;

public interface ISpotifyTokenService
{
    Task CreateSpotifyTokens(SpotifyConnectionDto dto, Guid userId);
    Task<SpotifyTokensDto> GetSpotifyTokens(Guid userId);
    Task<string> GetAccessTokenAsync(Guid userId);
}

public class SpotifyTokenService : ISpotifyTokenService
{
    private const string SpotifyRefreshTokenUrl = "https://accounts.spotify.com/api/token";
    
    private readonly IRepository<SpotifyTokens> _spotifyTokensRepository;
    private readonly IRepository<Account> _accountRepository;

    public SpotifyTokenService(
        IRepository<SpotifyTokens> spotifyTokensRepository,
        IRepository<Account> accountRepository)
    {
        _spotifyTokensRepository = spotifyTokensRepository;
        _accountRepository = accountRepository;
    }

    public async Task CreateSpotifyTokens(SpotifyConnectionDto dto, Guid userId)
    {
        var account = await _accountRepository.GetOneRequiredAsync(userId);
        
        var existingTokens = await _spotifyTokensRepository.GetOneAsync(userId);
        if (existingTokens is not null)
        {
            throw new SpotifyAccountAlreadyConnectedException();
        }

        var tokenExpirationDate = DateTime.UtcNow.AddSeconds(dto.Duration - 60);

        var newSpotifyTokens = new SpotifyTokens
        {
            AccountId = account.Id,
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            ExpirationDate = tokenExpirationDate,
            Account = account
        };

        await _spotifyTokensRepository.CreateAsync(newSpotifyTokens);
        await _spotifyTokensRepository.SaveChangesAsync();
    }

    public async Task<SpotifyTokensDto> GetSpotifyTokens(Guid userId)
    {
        var spotifyTokens = await _spotifyTokensRepository.GetOneAsync(userId);
        if (spotifyTokens is null)
        {
            throw new SpotifyAccountNotLinkedError();
        }

        return spotifyTokens.ToDto();
    }

    public async Task<string> GetAccessTokenAsync(Guid userId)
    {
        var spotifyTokens = await _spotifyTokensRepository.GetOneAsync(userId);
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
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, SpotifyRefreshTokenUrl);

        var authenticationHeaderValue = await GetAuthenticationHeader();
        request.Headers.Add("Authorization", $"Basic {authenticationHeaderValue}");

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        request.Content = content;

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var spotifyAccessCredentials = JsonSerializer.Deserialize<TokenRefreshResponse>(responseContent);

        if (spotifyAccessCredentials is null)
        {
            throw new Exception(); // TODO get more creative
        }

        await UpdateRefreshedAccessTokenAsync(userId, spotifyAccessCredentials.AccessToken, spotifyAccessCredentials.Duration);

        return spotifyAccessCredentials.AccessToken;
    }

    private async Task<string> GetAuthenticationHeader()
    {
        var spotifyAppCredentials = await new SpotifyAppCredentialsService().LoadCredentials();
        
        return Convert.ToBase64String(
           Encoding.UTF8.GetBytes($"{spotifyAppCredentials.ClientId}:{spotifyAppCredentials.ClientSecret}"));
    }

    private async Task UpdateRefreshedAccessTokenAsync(Guid userId, string accessToken, int duration)
    {
        var spotifyTokens = await _spotifyTokensRepository.GetOneRequiredAsync(userId);

        spotifyTokens.AccessToken = accessToken;
        spotifyTokens.ExpirationDate = DateTime.UtcNow.AddSeconds(duration - 60);

        await _spotifyTokensRepository.SaveChangesAsync();
    }
}