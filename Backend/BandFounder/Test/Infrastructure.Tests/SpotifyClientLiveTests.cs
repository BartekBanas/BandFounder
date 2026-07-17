using System.Text.Json;
using System.Text.Json.Serialization;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Exceptions;
using BandFounder.Infrastructure.Spotify.Services;

namespace Infrastructure.Tests;

[Category("LiveSpotify")]
public class SpotifyClientLiveTests
{
    private const string RefreshTokenEnv = "SPOTIFY_REFRESH_TOKEN";
    private const string CredentialsFileName = "spotifyLiveTestCredentials.json";
    private const string AppCredentialsFileName = "spotifyAppCredentials.json";
    private const string RefreshTokenExpirationDocsUrl =
        "https://developer.spotify.com/blog/2026-06-18-refresh-token-expiration";

    private SpotifyClient _client = null!;
    private SpotifyAppCredentials _credentials;
    private string _refreshToken = null!;

    [SetUp]
    public async Task SetUp()
    {
        var appCredentials = await TryLoadAppCredentialsAsync();
        var refreshToken = ResolveRefreshToken();

        if (appCredentials is null || string.IsNullOrWhiteSpace(refreshToken))
        {
            Assert.Ignore(
                "Live Spotify secrets not configured. Ensure BandFounder.Api/spotifyAppCredentials.json exists, " +
                $"and set {RefreshTokenEnv} or create {CredentialsFileName} from " +
                $"{Path.ChangeExtension(CredentialsFileName, ".example.json")}.");
        }

        _credentials = appCredentials.Value;
        _refreshToken = refreshToken;
        _client = new SpotifyClient();
    }

    [Test]
    public async Task RefreshToken_ReturnsAccessToken()
    {
        var tokens = await RefreshAccessTokenAsync();

        Assert.That(tokens.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(tokens.ExpiresIn, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetTopArtists_ReturnsDeserializableArtists()
    {
        var tokens = await RefreshAccessTokenAsync();

        var artists = await _client.GetTopArtistsAsync(tokens.AccessToken, limit: 5);

        Assert.That(artists, Is.Not.Null);
        Assert.That(artists.All(HasValidArtistIdentity), Is.True);
    }

    [Test]
    public async Task GetFollowedArtists_ReturnsDeserializableArtists()
    {
        var tokens = await RefreshAccessTokenAsync();

        var artists = await _client.GetFollowedArtistsAsync(tokens.AccessToken);

        Assert.That(artists, Is.Not.Null);
        Assert.That(artists.All(HasValidArtistIdentity), Is.True);
    }

    private async Task<SpotifyTokensResponse> RefreshAccessTokenAsync()
    {
        try
        {
            return await _client.RefreshTokenAsync(_refreshToken, _credentials);
        }
        catch (SpotifyRefreshTokenExpiredException)
        {
            Assert.Fail(
                "Spotify refresh token has expired (invalid_grant). " +
                "Re-authorize via the app OAuth flow, then update " +
                $"{RefreshTokenEnv} or {CredentialsFileName}. " +
                $"See {RefreshTokenExpirationDocsUrl}");
            throw; // unreachable; satisfies compiler
        }
    }

    private static bool HasValidArtistIdentity(SpotifyArtistDto artist) =>
        !string.IsNullOrWhiteSpace(artist.Id) && !string.IsNullOrWhiteSpace(artist.Name);

    private static string? ResolveRefreshToken()
    {
        var fromEnv = Environment.GetEnvironmentVariable(RefreshTokenEnv);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        return TryLoadRefreshTokenFromFile();
    }

    private static string? TryLoadRefreshTokenFromFile()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, CredentialsFileName);
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        var credentials = JsonSerializer.Deserialize<LiveSpotifyTestCredentials>(json);
        return string.IsNullOrWhiteSpace(credentials?.RefreshToken) ? null : credentials.RefreshToken;
    }

    private static async Task<SpotifyAppCredentials?> TryLoadAppCredentialsAsync()
    {
        var path = FindAppCredentialsPath();
        if (path is null)
        {
            return null;
        }

        try
        {
            var service = new SpotifyAppCredentialsService(path);
            return await service.LoadCredentials();
        }
        catch
        {
            return null;
        }
    }

    private static string? FindAppCredentialsPath()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "BandFounder.Api", AppCredentialsFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private sealed class LiveSpotifyTestCredentials
    {
        [JsonPropertyName("RefreshToken")] public string? RefreshToken { get; init; }
    }
}