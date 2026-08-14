using BandFounder.Application.Dtos.Accounts;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using BandFounder.Infrastructure.Spotify;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Exceptions;
using BandFounder.Infrastructure.Spotify.Services;
using System.Runtime.CompilerServices;

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
