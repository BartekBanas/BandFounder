using BandFounder.Application.Dtos.Accounts;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using BandFounder.Infrastructure.Spotify;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Exceptions;
using BandFounder.Infrastructure.Spotify.Services;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace BandFounder.Application.Services;

public interface ISpotifyConnectionService
{
    Task LinkAccountToSpotify(SpotifyConnectionDto dto, Guid userId);
    Task CreateSpotifyTokens(SpotifyTokensDto spotifyTokens, Guid userId);
    Task<SpotifyTokensDto> GetSpotifyTokens(Guid userId);
    Task<string> GetAccessTokenAsync(Guid userId);
    Task<List<SpotifyArtistDto>> SaveRelevantArtists(Guid userId);
    Task<List<SpotifyArtistDto>> GetTopArtistsAsync(
        Guid userId,
        int limit = 50,
        string timeRange = SpotifyTimeRange.Default);
    Task<List<SpotifyTrackDto>> GetTopTracksAsync(
        Guid userId,
        int limit = 50,
        string timeRange = SpotifyTimeRange.Default);
    Task<List<SpotifyArtistDto>> GetFollowedArtistsAsync(Guid userId);
    Task<LikedTracksByGenreResponse> GetLikedTracksByGenreAsync(Guid userId, string genre);
    IAsyncEnumerable<LikedTracksByGenreProgressUpdate> ScanLikedTracksByGenreAsync(
        Guid userId,
        string genre,
        CancellationToken cancellationToken = default);
    Task<List<SpotifyPlaylistSummaryDto>> GetEditablePlaylistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<PlaylistContributorsPreviewDto> PreviewPlaylistContributorsAsync(
        Guid userId,
        string playlistId,
        CancellationToken cancellationToken = default);
    IAsyncEnumerable<PlaylistCopyProgressUpdate> StreamPlaylistCopyAsync(
        Guid userId,
        string sourcePlaylistId,
        IReadOnlyCollection<string> contributorIds,
        string? targetPlaylistId,
        string? newPlaylistName,
        CancellationToken cancellationToken = default);
}

public class SpotifyConnectionService(
    ISpotifyClient spotifyClient,
    IRepository<SpotifyTokens> spotifyTokensRepository,
    IRepository<Artist> artistRepository,
    IRepository<Account> accountRepository,
    IRepository<Genre> genreRepository,
    ISpotifyAppCredentialsService spotifyAppCredentialsService)
    : ISpotifyConnectionService
{
    public const string UnknownContributorId = "";
    private const int MaxContributorProfileLookups = 25;

    public async Task LinkAccountToSpotify(SpotifyConnectionDto dto, Guid userId)
    {
        var spotifyAppCredentials = await spotifyAppCredentialsService.LoadCredentials();

        var tokensResponse = await spotifyClient.RequestAccessTokenAsync(dto, spotifyAppCredentials);

        var tokensDto = new SpotifyTokensDto
        {
            AccessToken = tokensResponse.AccessToken,
            RefreshToken = tokensResponse.RefreshToken,
            ExpirationDate = DateTime.UtcNow.AddSeconds(tokensResponse.ExpiresIn - 10)
        };

        await UpsertSpotifyTokens(tokensDto, userId);
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
            throw new SpotifyAccountNotLinkedException();
        }

        return spotifyTokens.ToDto();
    }

    public async Task<string> GetAccessTokenAsync(Guid userId)
    {
        var spotifyTokens = await spotifyTokensRepository.GetOneAsync(userId);
        if (spotifyTokens is null)
        {
            throw new SpotifyAccountNotLinkedException();
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
        var spotifyAppCredentials = await spotifyAppCredentialsService.LoadCredentials();

        try
        {
            var refreshedTokens = await spotifyClient.RefreshTokenAsync(refreshToken, spotifyAppCredentials);

            await UpdateRefreshedAccessTokenAsync(userId, refreshedTokens.AccessToken, refreshedTokens.ExpiresIn);

            if (!string.IsNullOrEmpty(refreshedTokens.RefreshToken))
            {
                var spotifyTokens = await spotifyTokensRepository.GetOneRequiredAsync(userId);
                spotifyTokens.RefreshToken = refreshedTokens.RefreshToken;
                await spotifyTokensRepository.SaveChangesAsync();
            }

            return refreshedTokens.AccessToken;
        }
        catch (SpotifyRefreshTokenExpiredException)
        {
            await spotifyTokensRepository.DeleteOneAsync(userId);
            await spotifyTokensRepository.SaveChangesAsync();
            throw new SpotifyReauthorizationRequiredException();
        }
    }

    private async Task UpsertSpotifyTokens(SpotifyTokensDto spotifyTokens, Guid userId)
    {
        var existingTokens = await spotifyTokensRepository.GetOneAsync(userId);
        if (existingTokens is not null)
        {
            existingTokens.AccessToken = spotifyTokens.AccessToken;
            existingTokens.RefreshToken = spotifyTokens.RefreshToken;
            existingTokens.ExpirationDate = spotifyTokens.ExpirationDate;
            await spotifyTokensRepository.SaveChangesAsync();
            return;
        }

        var account = await accountRepository.GetOneRequiredAsync(userId);

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

    public async Task<List<SpotifyArtistDto>> GetTopArtistsAsync(
        Guid userId,
        int limit = 50,
        string timeRange = SpotifyTimeRange.Default)
    {
        var accessToken = await GetAccessTokenAsync(userId);
        return await spotifyClient.GetTopArtistsAsync(
            accessToken,
            SpotifyTimeRange.ClampLimit(limit),
            SpotifyTimeRange.Normalize(timeRange));
    }

    public async Task<List<SpotifyTrackDto>> GetTopTracksAsync(
        Guid userId,
        int limit = 50,
        string timeRange = SpotifyTimeRange.Default)
    {
        var accessToken = await GetAccessTokenAsync(userId);
        return await spotifyClient.GetTopTracksAsync(
            accessToken,
            SpotifyTimeRange.ClampLimit(limit),
            SpotifyTimeRange.Normalize(timeRange));
    }

    public async Task<List<SpotifyArtistDto>> GetFollowedArtistsAsync(Guid userId)
    {
        var accessToken = await GetAccessTokenAsync(userId);
        return await spotifyClient.GetFollowedArtistsAsync(accessToken);
    }

    public async Task<LikedTracksByGenreResponse> GetLikedTracksByGenreAsync(Guid userId, string genre)
    {
        var accessToken = await GetAccessTokenAsync(userId);
        var savedTracks = await spotifyClient.GetSavedTracksAsync(accessToken);

        var artistIds = savedTracks.Tracks
            .SelectMany(track => track.Artists)
            .Select(artist => artist.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var artists = await spotifyClient.GetArtistsByIdsAsync(accessToken, artistIds);
        var genresByArtistId = artists.ToDictionary(
            artist => artist.Id,
            artist => (IReadOnlyList<string>)artist.Genres,
            StringComparer.Ordinal);

        var matchingTracks = new List<LikedTrackDto>();
        foreach (var track in savedTracks.Tracks)
        {
            if (!SpotifyGenreMatcher.TrackMatchesGenre(
                    track.Artists.Select(artist => artist.Id),
                    genresByArtistId,
                    genre))
            {
                continue;
            }

            matchingTracks.Add(CreateLikedTrack(track, genresByArtistId));
        }

        return new LikedTracksByGenreResponse
        {
            Tracks = matchingTracks,
            ScannedTrackCount = savedTracks.Tracks.Count,
            RemainingTrackCount = savedTracks.Truncated
                ? Math.Max(0, savedTracks.TotalAvailable - savedTracks.Tracks.Count)
                : 0,
            TotalAvailable = savedTracks.TotalAvailable,
            Truncated = savedTracks.Truncated
        };
    }

    public async IAsyncEnumerable<LikedTracksByGenreProgressUpdate> ScanLikedTracksByGenreAsync(
        Guid userId,
        string genre,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(userId);
        var genresByArtistId = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var matchingTracks = new List<LikedTrackDto>();
        var scannedTrackCount = 0;
        var totalAvailable = 0;
        var truncated = false;

        await foreach (var page in spotifyClient.GetSavedTracksPagesAsync(
                           accessToken,
                           cancellationToken: cancellationToken))
        {
            totalAvailable = page.TotalAvailable;
            truncated = page.Truncated;

            var newArtistIds = page.Tracks
                .SelectMany(track => track.Artists)
                .Select(artist => artist.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .Where(id => !genresByArtistId.ContainsKey(id))
                .ToList();

            foreach (var artistId in newArtistIds)
            {
                // Cache missing artists as empty too, so later pages do not request them again.
                genresByArtistId[artistId] = [];
            }

            if (newArtistIds.Count > 0)
            {
                var artists = await spotifyClient.GetArtistsByIdsAsync(accessToken, newArtistIds);
                foreach (var artist in artists)
                {
                    genresByArtistId[artist.Id] = artist.Genres;
                }
            }

            foreach (var track in page.Tracks)
            {
                scannedTrackCount++;
                if (!SpotifyGenreMatcher.TrackMatchesGenre(
                        track.Artists.Select(artist => artist.Id),
                        genresByArtistId,
                        genre))
                {
                    continue;
                }

                matchingTracks.Add(CreateLikedTrack(track, genresByArtistId));
            }

            yield return new LikedTracksByGenreProgressUpdate
            {
                Type = "progress",
                ScannedTrackCount = scannedTrackCount,
                RemainingTrackCount = page.HasMore
                    ? Math.Max(0, totalAvailable - scannedTrackCount)
                    : 0,
                TotalAvailable = totalAvailable,
                FoundMatchCount = matchingTracks.Count,
                Truncated = truncated
            };
        }

        yield return new LikedTracksByGenreProgressUpdate
        {
            Type = "complete",
            Tracks = matchingTracks,
            ScannedTrackCount = scannedTrackCount,
            RemainingTrackCount = truncated
                ? Math.Max(0, totalAvailable - scannedTrackCount)
                : 0,
            TotalAvailable = totalAvailable,
            FoundMatchCount = matchingTracks.Count,
            Truncated = truncated
        };
    }

    public async Task<List<SpotifyPlaylistSummaryDto>> GetEditablePlaylistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(userId);
        var currentUser = await spotifyClient.GetCurrentUserAsync(accessToken, cancellationToken);
        var playlists = await spotifyClient.GetCurrentUserPlaylistsAsync(accessToken, cancellationToken);

        return playlists
            .Where(playlist => playlist.Collaborative || IsOwnedBy(playlist, currentUser))
            .Select(playlist => ToPlaylistSummary(playlist, currentUser))
            .ToList();
    }

    public async Task<PlaylistContributorsPreviewDto> PreviewPlaylistContributorsAsync(
        Guid userId,
        string playlistId,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(userId);
        var currentUser = await spotifyClient.GetCurrentUserAsync(accessToken, cancellationToken);
        var playlist = await spotifyClient.GetPlaylistAsync(accessToken, playlistId, cancellationToken);

        var trackCountByContributor = new Dictionary<string, int>(StringComparer.Ordinal);
        var trackCount = 0;
        var skippedItemCount = 0;

        await foreach (var page in spotifyClient.GetPlaylistTracksPagesAsync(
                           accessToken,
                           playlistId,
                           cancellationToken))
        {
            trackCount += page.Entries.Count;
            skippedItemCount += page.ScannedItemCount - page.Entries.Count;

            foreach (var entry in page.Entries)
            {
                var contributorId = entry.AddedById ?? UnknownContributorId;
                trackCountByContributor[contributorId] = trackCountByContributor.GetValueOrDefault(contributorId) + 1;
            }
        }

        var displayNames = await ResolveContributorNamesAsync(
            accessToken,
            trackCountByContributor.Keys,
            currentUser,
            playlist.Owner,
            cancellationToken);

        var contributors = trackCountByContributor
            .Select(pair => new PlaylistContributorDto
            {
                Id = pair.Key,
                DisplayName = displayNames[pair.Key],
                IsCurrentUser = pair.Key == currentUser.Id,
                TrackCount = pair.Value
            })
            .OrderByDescending(contributor => contributor.TrackCount)
            .ThenBy(contributor => contributor.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PlaylistContributorsPreviewDto
        {
            Playlist = ToPlaylistSummary(playlist, currentUser, trackCount + skippedItemCount),
            Contributors = contributors,
            TrackCount = trackCount,
            SkippedItemCount = skippedItemCount
        };
    }

    public async IAsyncEnumerable<PlaylistCopyProgressUpdate> StreamPlaylistCopyAsync(
        Guid userId,
        string sourcePlaylistId,
        IReadOnlyCollection<string> contributorIds,
        string? targetPlaylistId,
        string? newPlaylistName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(userId);
        var currentUser = await spotifyClient.GetCurrentUserAsync(accessToken, cancellationToken);
        var selectedContributors = contributorIds.ToHashSet(StringComparer.Ordinal);

        // Fetch both playlists' metadata up front so a bad link fails before any reading or writing.
        var source = await spotifyClient.GetPlaylistAsync(accessToken, sourcePlaylistId, cancellationToken);
        var existingTarget = targetPlaylistId is null
            ? null
            : await spotifyClient.GetPlaylistAsync(accessToken, targetPlaylistId, cancellationToken);

        var tracksToCopy = new List<(string Key, string Uri)>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var selectedTrackCount = 0;
        var scannedSourceItems = 0;

        await foreach (var page in spotifyClient.GetPlaylistTracksPagesAsync(
                           accessToken,
                           sourcePlaylistId,
                           cancellationToken))
        {
            scannedSourceItems += page.ScannedItemCount;

            foreach (var entry in page.Entries)
            {
                if (!selectedContributors.Contains(entry.AddedById ?? UnknownContributorId))
                {
                    continue;
                }

                selectedTrackCount++;
                var key = CreateDedupKey(entry.Track);
                if (seenKeys.Add(key))
                {
                    tracksToCopy.Add((key, $"spotify:track:{entry.Track.Id}"));
                }
            }

            yield return new PlaylistCopyProgressUpdate
            {
                Type = "progress",
                Phase = PlaylistCopyPhase.ReadingSource,
                Processed = scannedSourceItems,
                Total = Math.Max(page.TotalAvailable, scannedSourceItems),
                SelectedTrackCount = selectedTrackCount
            };
        }

        var existingTargetKeys = new HashSet<string>(StringComparer.Ordinal);
        var scannedTargetItems = 0;

        if (existingTarget is not null)
        {
            await foreach (var page in spotifyClient.GetPlaylistTracksPagesAsync(
                               accessToken,
                               existingTarget.Id,
                               cancellationToken))
            {
                scannedTargetItems += page.ScannedItemCount;
                foreach (var entry in page.Entries)
                {
                    existingTargetKeys.Add(CreateDedupKey(entry.Track));
                }

                yield return new PlaylistCopyProgressUpdate
                {
                    Type = "progress",
                    Phase = PlaylistCopyPhase.ReadingTarget,
                    Processed = scannedTargetItems,
                    Total = Math.Max(page.TotalAvailable, scannedTargetItems),
                    SelectedTrackCount = selectedTrackCount
                };
            }
        }

        var urisToAdd = tracksToCopy
            .Where(track => !existingTargetKeys.Contains(track.Key))
            .Select(track => track.Uri)
            .ToList();
        var skippedDuplicateCount = selectedTrackCount - urisToAdd.Count;

        if (existingTarget is null && urisToAdd.Count == 0)
        {
            yield return new PlaylistCopyProgressUpdate
            {
                Type = "complete",
                SelectedTrackCount = selectedTrackCount,
                SkippedDuplicateCount = skippedDuplicateCount
            };
            yield break;
        }

        var target = existingTarget ?? await spotifyClient.CreatePlaylistAsync(
            accessToken,
            newPlaylistName!,
            $"Songs copied from \"{source.Name}\".",
            cancellationToken);
        var createdTarget = existingTarget is null;
        var addedCount = 0;

        PlaylistCopyProgressUpdate CreateComplete(string? failureMessage) => new()
        {
            Type = "complete",
            SelectedTrackCount = selectedTrackCount,
            AddedCount = addedCount,
            SkippedDuplicateCount = skippedDuplicateCount,
            TargetPlaylist = ToPlaylistSummary(target, currentUser, scannedTargetItems + addedCount),
            CreatedTarget = createdTarget,
            FailureMessage = failureMessage
        };

        foreach (var batch in urisToAdd.Chunk(SpotifyClient.PlaylistAddBatchSize))
        {
            Exception? failure = null;
            try
            {
                await spotifyClient.AddTracksToPlaylistAsync(accessToken, target.Id, batch, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failure = ex;
            }

            if (failure is not null)
            {
                // Nothing has changed in Spotify yet, so surface it as a normal error (e.g. reconnect prompts).
                if (addedCount == 0 && !createdTarget)
                {
                    ExceptionDispatchInfo.Capture(failure).Throw();
                }

                yield return CreateComplete(DescribeCopyFailure(failure));
                yield break;
            }

            addedCount += batch.Length;
            yield return new PlaylistCopyProgressUpdate
            {
                Type = "progress",
                Phase = PlaylistCopyPhase.Adding,
                Processed = addedCount,
                Total = urisToAdd.Count,
                SelectedTrackCount = selectedTrackCount,
                AddedCount = addedCount,
                SkippedDuplicateCount = skippedDuplicateCount,
                TargetPlaylist = ToPlaylistSummary(target, currentUser, scannedTargetItems + addedCount),
                CreatedTarget = createdTarget
            };
        }

        yield return CreateComplete(null);
    }

    private async Task<Dictionary<string, string>> ResolveContributorNamesAsync(
        string accessToken,
        IEnumerable<string> contributorIds,
        SpotifyUserDto currentUser,
        SpotifyUserDto? playlistOwner,
        CancellationToken cancellationToken)
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var lookupsLeft = MaxContributorProfileLookups;
        var profileLookupWorks = true;

        foreach (var contributorId in contributorIds)
        {
            if (contributorId == UnknownContributorId)
            {
                names[contributorId] = "Unknown contributor";
                continue;
            }

            if (contributorId == currentUser.Id)
            {
                names[contributorId] = NonBlankOr(currentUser.DisplayName, contributorId);
                continue;
            }

            if (contributorId == playlistOwner?.Id && !string.IsNullOrWhiteSpace(playlistOwner.DisplayName))
            {
                names[contributorId] = playlistOwner.DisplayName;
                continue;
            }

            string? displayName = null;
            if (profileLookupWorks && lookupsLeft > 0)
            {
                lookupsLeft--;
                var profile = await spotifyClient.TryGetUserProfileAsync(accessToken, contributorId, cancellationToken);
                // One failure means the endpoint is unavailable for this app; skip the remaining lookups.
                profileLookupWorks = profile is not null;
                displayName = profile?.DisplayName;
            }

            names[contributorId] = NonBlankOr(displayName, contributorId);
        }

        return names;
    }

    private static string DescribeCopyFailure(Exception exception)
    {
        return exception is SpotifyRequestFailedException
            or SpotifyResourceUnavailableException
            or SpotifyRateLimitExceededException
            or SpotifyInsufficientScopeException
            or SpotifyReauthorizationRequiredException
            ? exception.Message
            : "Spotify stopped accepting songs partway through.";
    }

    private static string CreateDedupKey(SpotifyTrackDto track)
    {
        return SpotifyTrackDedupKey.Create(track.Name, track.Artists.Select(artist => artist.Name));
    }

    private static bool IsOwnedBy(SpotifyPlaylistDto playlist, SpotifyUserDto user)
    {
        return playlist.Owner?.Id is { } ownerId && ownerId == user.Id;
    }

    private static string NonBlankOr(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static SpotifyPlaylistSummaryDto ToPlaylistSummary(
        SpotifyPlaylistDto playlist,
        SpotifyUserDto currentUser,
        int? trackCount = null)
    {
        return new SpotifyPlaylistSummaryDto
        {
            Id = playlist.Id,
            Name = playlist.Name,
            ImageUrl = playlist.Images?
                .OrderBy(image => Math.Abs((image.Width ?? 300) - 300))
                .FirstOrDefault()?.Url,
            OwnerName = playlist.Owner?.DisplayName ?? playlist.Owner?.Id,
            OwnedByMe = IsOwnedBy(playlist, currentUser),
            Collaborative = playlist.Collaborative,
            TrackCount = trackCount ?? playlist.ItemCount,
            Url = playlist.ExternalUrls?.Spotify ?? $"https://open.spotify.com/playlist/{playlist.Id}"
        };
    }

    private static LikedTrackDto CreateLikedTrack(
        SpotifyTrackDto track,
        IReadOnlyDictionary<string, IReadOnlyList<string>> genresByArtistId)
    {
        return new LikedTrackDto
        {
            Id = track.Id,
            Name = track.Name,
            AlbumImageUrl = track.Album?.Images
                .OrderBy(image => Math.Abs((image.Width ?? 0) - 64))
                .FirstOrDefault()?.Url,
            Artists = track.Artists.Select(artist => new LikedTrackArtistDto
            {
                Id = artist.Id,
                Name = artist.Name,
                Genres = artist.Id is not null
                         && genresByArtistId.TryGetValue(artist.Id, out var genres)
                    ? genres.ToList()
                    : []
            }).ToList()
        };
    }
}
