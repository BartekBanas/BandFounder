using System.Text;
using System.Text.Json;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure.Errors;
using BandFounder.Infrastructure.Spotify.Dto;

namespace BandFounder.Infrastructure.Spotify.Services;

public interface ISpotifyTokenService
{
    Task CreateTokenSpotifyCredentials(SpotifyConnectionDto dto, Guid userId);
    Task<SpotifyCredentialsDto> GetSpotifyTokenCredentials(Guid userId);
    Task<string> GetAccessTokenAsync(Guid userId);
}

public class SpotifyTokenService : ISpotifyTokenService
{
    private const string SpotifyRefreshTokenUrl = "https://accounts.spotify.com/api/token";
    
    private readonly IRepository<SpotifyCredentials> _credentialsRepository;
    private readonly IRepository<Account> _accountRepository;

    public SpotifyTokenService(
        IRepository<SpotifyCredentials> credentialsRepository,
        IRepository<Account> accountRepository)
    {
        _credentialsRepository = credentialsRepository;
        _accountRepository = accountRepository;
    }

    public async Task CreateTokenSpotifyCredentials(SpotifyConnectionDto dto, Guid userId)
    {
        var account = await _accountRepository.GetOneRequiredAsync(userId);
        
        var existingCredential = await _credentialsRepository.GetOneAsync(userId);
        if (existingCredential is not null)
        {
            throw new SpotifyAccountAlreadyConnectedException();
        }

        var tokenExpirationDate = DateTime.UtcNow.AddSeconds(dto.Duration - 60);

        var newCredentials = new SpotifyCredentials
        {
            AccountId = account.Id,
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            ExpirationDate = tokenExpirationDate,
            Account = account
        };

        await _credentialsRepository.CreateAsync(newCredentials);

        await _credentialsRepository.SaveChangesAsync();
    }

    public async Task<SpotifyCredentialsDto> GetSpotifyTokenCredentials(Guid userId)
    {
        var spotifyCredentials = await _credentialsRepository.GetOneAsync(userId);

        if (spotifyCredentials is null)
        {
            throw new SpotifyAccountNotLinkedError();
        }

        return spotifyCredentials.ToDto();
    }

    public async Task<string> GetAccessTokenAsync(Guid userId)
    {
        var spotifyCredentials = await _credentialsRepository.GetOneAsync(userId);

        if (spotifyCredentials is null)
        {
            throw new SpotifyAccountNotLinkedError();
        }

        // Check if the stored token is still valid
        if (DateTime.UtcNow < spotifyCredentials.ExpirationDate)
        {
            return spotifyCredentials.AccessToken;
        }
        else
        {
            return await RefreshTokenAsync(userId, spotifyCredentials.RefreshToken);
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

        await SaveRefreshedAccessTokenAsync(userId, spotifyAccessCredentials.AccessToken, spotifyAccessCredentials.Duration);

        return spotifyAccessCredentials.AccessToken;
    }

    private async Task<string> GetAuthenticationHeader()
    {
        var spotifyAppCredentials = await new SpotifyAppCredentialsService().LoadCredentials();
        
        return Convert.ToBase64String(
           Encoding.UTF8.GetBytes($"{spotifyAppCredentials.ClientId}:{spotifyAppCredentials.ClientSecret}"));
    }

    private async Task SaveRefreshedAccessTokenAsync(Guid userId, string accessToken, int duration)
    {
        var spotifyCredentials = await _credentialsRepository.GetOneRequiredAsync(userId);

        spotifyCredentials.AccessToken = accessToken;
        spotifyCredentials.ExpirationDate = DateTime.UtcNow.AddSeconds(duration - 60);

        await _credentialsRepository.SaveChangesAsync();
    }
}