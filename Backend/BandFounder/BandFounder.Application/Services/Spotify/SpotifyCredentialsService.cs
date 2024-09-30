using System.Text;
using System.Text.Json;
using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Spotify;
using BandFounder.Domain;
using BandFounder.Domain.Entities;

namespace BandFounder.Application.Services.Spotify;

public interface ISpotifyCredentialsService
{
    Task CreateSpotifyCredentials(SpotifyAuthorizationDto dto);
    Task<SpotifyCredentialsDto> GetSpotifyCredentials();
    Task<string> GetAccessTokenAsync();
}

public class SpotifyCredentialsService : ISpotifyCredentialsService
{
    private readonly IRepository<SpotifyCredentials> _credentialsRepository;
    private readonly IRepository<Account> _accountRepository;
    private readonly IUserAuthenticationService _authenticationService;

    public SpotifyCredentialsService(
        IRepository<SpotifyCredentials> credentialsRepository,
        IRepository<Account> accountRepository,
        IUserAuthenticationService authenticationService)
    {
        _credentialsRepository = credentialsRepository;
        _accountRepository = accountRepository;
        _authenticationService = authenticationService;
    }

    public async Task CreateSpotifyCredentials(SpotifyAuthorizationDto dto)
    {
        var userId = _authenticationService.GetUserId();
        var account = await _accountRepository.GetOneRequiredAsync(userId);

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

    public async Task<SpotifyCredentialsDto> GetSpotifyCredentials()
    {
        var userId = _authenticationService.GetUserId();
        var spotifyCredentials = await _credentialsRepository.GetOneAsync(userId);

        return spotifyCredentials!.ToDto();
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var userId = _authenticationService.GetUserId();
        var spotifyCredentials = await _credentialsRepository.GetOneAsync(userId);

        if (spotifyCredentials is null)
        {
            throw new InvalidOperationException("Your account hasn't been connected to a spotify account");
        }

        // Check if the stored token is still valid
        if (DateTime.UtcNow < spotifyCredentials.ExpirationDate)
        {
            return spotifyCredentials.AccessToken;
        }
        else
        {
            return await RefreshTokenAsync(spotifyCredentials.RefreshToken);
        }
    }

    private async Task<string> RefreshTokenAsync(string refreshToken)
    {
        var spotifyAppCredentials = await new SpotifyAppCredentialsManager().LoadCredentials();

        const string url = "https://accounts.spotify.com/api/token";
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url);

        var authenticationHeaderValue =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{spotifyAppCredentials.ClientId}:{spotifyAppCredentials.ClientSecret}"));
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

        await SaveRefreshedAccessTokenAsync(spotifyAccessCredentials.AccessToken, spotifyAccessCredentials.Duration);

        return spotifyAccessCredentials.AccessToken;
    }

    private async Task SaveRefreshedAccessTokenAsync(string accessToken, int duration)
    {
        var userId = _authenticationService.GetUserId();
        var spotifyCredentials = await _credentialsRepository.GetOneRequiredAsync(userId);

        spotifyCredentials.AccessToken = accessToken;
        spotifyCredentials.ExpirationDate = DateTime.UtcNow.AddSeconds(duration - 60);

        await _credentialsRepository.SaveChangesAsync();
    }
}