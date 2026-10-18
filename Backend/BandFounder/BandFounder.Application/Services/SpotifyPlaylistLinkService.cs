using System.Globalization;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using BandFounder.Infrastructure.Spotify;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;

namespace BandFounder.Application.Services;

public abstract record SpotifyPlaylistLinkResult
{
    public sealed record Linked(string PlaylistName, int NewArtistCount, bool TruncatedAtTrackCap)
        : SpotifyPlaylistLinkResult
    {
        public string Message
        {
            get
            {
                var summary = $"Linked {PlaylistName}: {NewArtistCount} new artists added";
                if (!TruncatedAtTrackCap)
                {
                    return summary;
                }

                var trackCap = SpotifyPlaylistReadLimits.MaxTracks.ToString("N0", CultureInfo.InvariantCulture);
                return $"{summary}. Import stopped at {trackCap} tracks.";
            }
        }
    }

    public sealed record Duplicate : SpotifyPlaylistLinkResult;

    public sealed record CapReached : SpotifyPlaylistLinkResult;

    public sealed record NotReadable : SpotifyPlaylistLinkResult;

    public sealed record InvalidInput : SpotifyPlaylistLinkResult;
}

public static class SpotifyPlaylistLinkErrors
{
    public const string Duplicate = "This playlist is already linked to your account.";

    public static readonly string CapReached =
        $"You can link up to {LinkedPlaylist.MaxPerAccount} playlists.";
}

public interface ISpotifyPlaylistLinkService
{
    Task<SpotifyPlaylistLinkResult> LinkAsync(Guid accountId, string input);

    Task<bool> UnlinkAsync(Guid accountId, string spotifyId);
}

public class SpotifyPlaylistLinkService(
    ISpotifyPlaylistReadService playlistReadService,
    ISpotifyPlaylistReadTokenProvider tokenProvider,
    ISpotifyClient spotifyClient,
    IRepository<Account> accountRepository,
    IRepository<Artist> artistRepository,
    IRepository<Genre> genreRepository,
    IUnitOfWork unitOfWork)
    : ISpotifyPlaylistLinkService
{
    private static readonly string[] AccountIncludes =
    [
        nameof(Account.Artists),
        nameof(Account.ArtistLinks),
        "ArtistLinks.Sources",
        nameof(Account.LinkedPlaylists)
    ];

    public async Task<SpotifyPlaylistLinkResult> LinkAsync(Guid accountId, string input)
    {
        var earlyReject = await TryRejectKnownPlaylistAsync(accountId, input);
        if (earlyReject is not null)
        {
            return earlyReject;
        }

        var read = await playlistReadService.ReadAsync(input);
        return read switch
        {
            SpotifyPlaylistReadResult.Success success => await LinkReadPlaylistAsync(accountId, success.Playlist),
            SpotifyPlaylistReadResult.NotReadable => new SpotifyPlaylistLinkResult.NotReadable(),
            SpotifyPlaylistReadResult.InvalidInput => new SpotifyPlaylistLinkResult.InvalidInput(),
        };
    }

    private async Task<SpotifyPlaylistLinkResult?> TryRejectKnownPlaylistAsync(Guid accountId, string input)
    {
        return SpotifyPlaylistParser.Parse(input) switch
        {
            SpotifyPlaylistParseResult.PlaylistId playlistId =>
                RejectIfCannotLink(await LoadAccountAsync(accountId), playlistId.Id.Value),
            SpotifyPlaylistParseResult.ShortLink => null,
            SpotifyPlaylistParseResult.Invalid => null,
        };
    }

    private async Task<SpotifyPlaylistLinkResult> LinkReadPlaylistAsync(
        Guid accountId,
        SpotifyPlaylistRead playlist)
    {
        var uniqueArtists = playlist.Artists
            .Where(artist => !string.IsNullOrWhiteSpace(artist.Id) && !string.IsNullOrWhiteSpace(artist.Name))
            .DistinctBy(artist => artist.Id)
            .ToList();
        var spotifyArtists = await LoadSpotifyArtistsAsync(uniqueArtists.Select(artist => artist.Id).ToList());

        SpotifyPlaylistLinkResult? result = null;
        await unitOfWork.ExecuteSerializableTransactionAsync(async () =>
        {
            result = await PersistLinkedPlaylistAsync(accountId, playlist, uniqueArtists, spotifyArtists);
        });

        return result!;
    }

    public async Task<bool> UnlinkAsync(Guid accountId, string spotifyId)
    {
        var account = await LoadAccountAsync(accountId);
        if (!account.UnlinkPlaylist(spotifyId))
        {
            return false;
        }

        await accountRepository.SaveChangesAsync();
        return true;
    }

    private async Task<SpotifyPlaylistLinkResult> PersistLinkedPlaylistAsync(
        Guid accountId,
        SpotifyPlaylistRead playlist,
        IReadOnlyList<SpotifyPlaylistArtist> uniqueArtists,
        IReadOnlyDictionary<string, SpotifyArtistDto> spotifyArtists)
    {
        var account = await LoadAccountAsync(accountId);
        var reject = RejectIfCannotLink(account, playlist.SpotifyId);
        if (reject is not null)
        {
            return reject;
        }

        var artistEntities = await artistRepository.GetOrCreateManyAsync(
            genreRepository,
            uniqueArtists.Select(artist => ToArtistData(artist, spotifyArtists.GetValueOrDefault(artist.Id))).ToList());

        account.LinkedPlaylists.Add(new LinkedPlaylist
        {
            AccountId = account.Id,
            Account = account,
            SpotifyId = playlist.SpotifyId,
            Name = playlist.Name,
            LinkedAt = DateTime.UtcNow
        });

        var source = ArtistSource.FromLinkedPlaylist(playlist.SpotifyId);
        var newArtistCount = 0;
        foreach (var artist in artistEntities)
        {
            if (account.HoldArtist(artist, source))
            {
                newArtistCount++;
            }
        }

        await accountRepository.SaveChangesAsync();
        return new SpotifyPlaylistLinkResult.Linked(playlist.Name, newArtistCount, playlist.TruncatedAtTrackCap);
    }

    /// <summary>Prefers the full Spotify profile. Falls back to the name listed on the playlist.</summary>
    private static ArtistData ToArtistData(SpotifyPlaylistArtist playlistArtist, SpotifyArtistDto? spotifyArtist) =>
        new(playlistArtist.Id, string.IsNullOrWhiteSpace(spotifyArtist?.Name) ? playlistArtist.Name : spotifyArtist.Name)
        {
            Genres = spotifyArtist?.Genres ?? [],
            Popularity = spotifyArtist?.Popularity ?? 0
        };

    private Task<Account> LoadAccountAsync(Guid accountId) =>
        accountRepository.GetOneRequiredAsync(
            key: accountId,
            keyPropertyName: nameof(Account.Id),
            includeProperties: AccountIncludes);

    private static SpotifyPlaylistLinkResult? RejectIfCannotLink(Account account, string spotifyId)
    {
        if (account.LinkedPlaylists.Any(linked => linked.SpotifyId == spotifyId))
        {
            return new SpotifyPlaylistLinkResult.Duplicate();
        }

        if (account.LinkedPlaylists.Count >= LinkedPlaylist.MaxPerAccount)
        {
            return new SpotifyPlaylistLinkResult.CapReached();
        }

        return null;
    }

    private async Task<IReadOnlyDictionary<string, SpotifyArtistDto>> LoadSpotifyArtistsAsync(
        IReadOnlyList<string> artistIds)
    {
        if (artistIds.Count == 0)
        {
            return new Dictionary<string, SpotifyArtistDto>();
        }

        var accessToken = await tokenProvider.GetAccessTokenAsync();
        var artists = await spotifyClient.GetArtistsByIdsAsync(accessToken, artistIds);
        return artists
            .Where(artist => !string.IsNullOrWhiteSpace(artist.Id))
            .DistinctBy(artist => artist.Id)
            .ToDictionary(artist => artist.Id);
    }
}
