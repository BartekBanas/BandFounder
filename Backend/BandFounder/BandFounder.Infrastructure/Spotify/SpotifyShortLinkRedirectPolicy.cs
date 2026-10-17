namespace BandFounder.Infrastructure.Spotify;

public static class SpotifyShortLinkRedirectPolicy
{
    public static bool IsSpotifyLink(Uri url) =>
        SpotifyPlaylistId.IsHttpOrHttps(url) && SpotifyPlaylistId.HostnameOf(url) == "spotify.link";

    public static bool IsOpenSpotify(Uri url) =>
        SpotifyPlaylistId.IsHttpOrHttps(url) && SpotifyPlaylistId.HostnameOf(url) == "open.spotify.com";

    public static bool CanFollow(Uri from, Uri to) => IsSpotifyLink(from) && IsOpenSpotify(to);
}
