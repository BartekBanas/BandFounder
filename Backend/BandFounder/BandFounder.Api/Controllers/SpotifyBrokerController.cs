using System.Text.Json;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Exceptions;
using BandFounder.Application.Services;
using BandFounder.Infrastructure.Spotify;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Exceptions;
using BandFounder.Infrastructure.Spotify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace BandFounder.Api.Controllers;

[Route("api")]
public class SpotifyBrokerController : ControllerBase
{
    private const int MaxGenreQueryLength = 50;
    private const int GenreScanTimeoutMilliseconds = 600_000;
    private const int PlaylistCopyTimeoutMilliseconds = 600_000;
    private const int MaxPlaylistNameLength = 100;
    private const int MaxSelectedContributors = 200;
    private const string InvalidPlaylistLinkMessage =
        "That doesn't look like a Spotify playlist link. Paste a link like https://open.spotify.com/playlist/…";
    private static readonly JsonSerializerOptions StreamJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ISpotifyConnectionService _spotifyConnectionService;
    private readonly IAuthenticationService _authenticationService;

    public SpotifyBrokerController(
        ISpotifyConnectionService spotifyConnectionService,
        IAuthenticationService authenticationService)
    {
        _spotifyConnectionService = spotifyConnectionService;
        _authenticationService = authenticationService;
    }

    [HttpGet("spotify/app/clientId")]
    public async Task<IActionResult> GetSpotifyAppCredentials()
    {
        var appCredentialsService = new SpotifyAppCredentialsService();
        var credentials = await appCredentialsService.LoadCredentials();

        return Ok(credentials.ClientId);
    }

    [Authorize]
    [HttpPost("spotify/tokens")]
    public async Task<IActionResult> ConnectToSpotify([FromBody] SpotifyConnectionDto dto)
    {
        var userId = _authenticationService.GetUserId();

        await _spotifyConnectionService.LinkAccountToSpotify(dto, userId);

        return Ok();
    }

    [Authorize]
    [Obsolete("Use /spotify/tokens instead. This endpoint is for development purposes only.")]
    [HttpPost("spotify/tokens/manual")]
    public async Task<IActionResult> AddSpotifyTokens([FromBody] SpotifyTokensDto dto)
    {
        await _spotifyConnectionService.CreateSpotifyTokens(dto, _authenticationService.GetUserId());

        return Ok();
    }

    [Authorize]
    [HttpGet("spotify/tokens")]
    public async Task<IActionResult> GetSpotifyTokens()
    {
        var userId = _authenticationService.GetUserId();

        var credentialsDto = await _spotifyConnectionService.GetSpotifyTokens(userId);

        return Ok(credentialsDto);
    }

    [Authorize]
    [HttpPost("spotify/update-artists")]
    public async Task<IActionResult> UpdateArtistsFromSpotify()
    {
        var userId = _authenticationService.GetUserId();

        var newlyAddedArtists = await _spotifyConnectionService.SaveRelevantArtists(userId);

        return Ok(newlyAddedArtists);
    }

    [Authorize]
    [HttpGet("spotify/liked-tracks/by-genre")]
    [RequestTimeout(GenreScanTimeoutMilliseconds)]
    public async Task<IActionResult> GetLikedTracksByGenre([FromQuery] string? genre)
    {
        var trimmedGenre = genre?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedGenre))
        {
            throw new BadRequestException("Genre query is required.");
        }

        if (trimmedGenre.Length > MaxGenreQueryLength)
        {
            throw new BadRequestException($"Genre query must be at most {MaxGenreQueryLength} characters.");
        }

        var userId = _authenticationService.GetUserId();
        var result = await _spotifyConnectionService.GetLikedTracksByGenreAsync(userId, trimmedGenre);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("spotify/liked-tracks/by-genre/stream")]
    [RequestTimeout(GenreScanTimeoutMilliseconds)]
    public async Task GetLikedTracksByGenreStream([FromQuery] string? genre)
    {
        var trimmedGenre = genre?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedGenre))
        {
            throw new BadRequestException("Genre query is required.");
        }

        if (trimmedGenre.Length > MaxGenreQueryLength)
        {
            throw new BadRequestException($"Genre query must be at most {MaxGenreQueryLength} characters.");
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/x-ndjson";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            var userId = _authenticationService.GetUserId();
            await foreach (var update in _spotifyConnectionService.ScanLikedTracksByGenreAsync(
                               userId,
                               trimmedGenre,
                               HttpContext.RequestAborted))
            {
                await WriteStreamMessageAsync(update);
            }
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            // The client navigated away or cancelled the request.
        }
        catch (Exception ex) when (Response.HasStarted)
        {
            var error = CreateStreamError(ex, "Something went wrong while scanning liked songs.");
            await WriteStreamMessageAsync(error);
        }
    }

    [Authorize]
    [HttpGet("spotify/playlists/mine")]
    public async Task<IActionResult> GetMyEditablePlaylists()
    {
        var userId = _authenticationService.GetUserId();
        var playlists = await _spotifyConnectionService.GetEditablePlaylistsAsync(userId, HttpContext.RequestAborted);
        return Ok(playlists);
    }

    [Authorize]
    [HttpGet("spotify/playlists/preview")]
    [RequestTimeout(PlaylistCopyTimeoutMilliseconds)]
    public async Task<IActionResult> PreviewPlaylistContributors([FromQuery] string? playlistId)
    {
        if (!SpotifyPlaylistId.TryParse(playlistId, out var parsedPlaylistId))
        {
            throw new BadRequestException(InvalidPlaylistLinkMessage);
        }

        var userId = _authenticationService.GetUserId();
        var preview = await _spotifyConnectionService.PreviewPlaylistContributorsAsync(
            userId,
            parsedPlaylistId,
            HttpContext.RequestAborted);
        return Ok(preview);
    }

    [Authorize]
    [HttpPost("spotify/playlists/copy/stream")]
    [RequestTimeout(PlaylistCopyTimeoutMilliseconds)]
    public async Task CopyPlaylistContributorsStream([FromBody] PlaylistCopyRequestDto? request)
    {
        if (!SpotifyPlaylistId.TryParse(request?.SourcePlaylistId, out var sourcePlaylistId))
        {
            throw new BadRequestException(InvalidPlaylistLinkMessage);
        }

        var contributorIds = (request!.ContributorIds ?? [])
            .Select(id => id?.Trim() ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (contributorIds.Count == 0)
        {
            throw new BadRequestException("Pick at least one contributor.");
        }

        if (contributorIds.Count > MaxSelectedContributors)
        {
            throw new BadRequestException($"Pick at most {MaxSelectedContributors} contributors.");
        }

        string? targetPlaylistId = null;
        var newPlaylistName = request.NewPlaylistName?.Trim();
        if (!string.IsNullOrWhiteSpace(request.TargetPlaylistId))
        {
            if (!SpotifyPlaylistId.TryParse(request.TargetPlaylistId, out targetPlaylistId))
            {
                throw new BadRequestException("The target playlist id isn't valid.");
            }

            if (targetPlaylistId == sourcePlaylistId)
            {
                throw new BadRequestException("Pick a target that's different from the source playlist.");
            }

            newPlaylistName = null;
        }
        else if (string.IsNullOrWhiteSpace(newPlaylistName))
        {
            throw new BadRequestException("Name the new playlist or pick an existing one.");
        }
        else if (newPlaylistName.Length > MaxPlaylistNameLength)
        {
            throw new BadRequestException($"Playlist names can be at most {MaxPlaylistNameLength} characters.");
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/x-ndjson";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            var userId = _authenticationService.GetUserId();
            await foreach (var update in _spotifyConnectionService.StreamPlaylistCopyAsync(
                               userId,
                               sourcePlaylistId,
                               contributorIds,
                               targetPlaylistId,
                               newPlaylistName,
                               HttpContext.RequestAborted))
            {
                await WriteStreamMessageAsync(update);
            }
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            // The client stopped the copy or navigated away.
        }
        catch (Exception ex) when (Response.HasStarted)
        {
            var error = CreateStreamError(ex, "Something went wrong while copying songs.");
            await WriteStreamMessageAsync(error);
        }
    }

    [Authorize]
    [HttpGet("accounts/{accountId:guid}/artists/spotify/top")]
    public async Task<IActionResult> GetUsersSpotifyTopArtists(
        [FromRoute] Guid accountId,
        [FromQuery] string? timeRange = null,
        [FromQuery] int? limit = null)
    {
        var resolvedLimit = SpotifyTimeRange.ClampLimit(limit, defaultLimit: 10);
        var artistDtoList = await _spotifyConnectionService.GetTopArtistsAsync(
            accountId,
            resolvedLimit,
            SpotifyTimeRange.Normalize(timeRange));
        var artists = artistDtoList.Select(artist => new TopArtistDto
        {
            Id = artist.Id,
            Name = artist.Name,
            ImageUrl = artist.Images
                .OrderBy(image => Math.Abs((image.Width ?? 0) - 320))
                .FirstOrDefault()?.Url,
            Genres = artist.Genres
        }).ToList();

        return Ok(artists);
    }

    [Authorize]
    [HttpGet("accounts/{accountId:guid}/tracks/spotify/top")]
    public async Task<IActionResult> GetUsersSpotifyTopTracks(
        [FromRoute] Guid accountId,
        [FromQuery] string? timeRange = null,
        [FromQuery] int? limit = null)
    {
        var resolvedLimit = SpotifyTimeRange.ClampLimit(limit, defaultLimit: 50);
        var trackDtoList = await _spotifyConnectionService.GetTopTracksAsync(
            accountId,
            resolvedLimit,
            SpotifyTimeRange.Normalize(timeRange));
        var tracks = trackDtoList.Select(track => new TopTrackDto
        {
            Id = track.Id,
            Name = track.Name,
            ImageUrl = track.Album?.Images
                .OrderBy(image => Math.Abs((image.Width ?? 0) - 64))
                .FirstOrDefault()?.Url,
            ArtistNames = track.Artists.Select(artist => artist.Name).ToList()
        }).ToList();

        return Ok(tracks);
    }

    [Authorize]
    [HttpGet("accounts/{accountId:guid}/artists/spotify/followed")]
    public async Task<IActionResult> GetUsersSpotifyFollowedArtists([FromRoute] Guid accountId)
    {
        var artistDtoList = await _spotifyConnectionService.GetFollowedArtistsAsync(accountId);
        var artists = artistDtoList.Select(artist => artist.Name).ToList();

        return Ok(artists);
    }

    private async Task WriteStreamMessageAsync(object message)
    {
        var json = JsonSerializer.Serialize(message, StreamJsonOptions);
        await Response.WriteAsync(json, HttpContext.RequestAborted);
        await Response.WriteAsync("\n", HttpContext.RequestAborted);
        await Response.Body.FlushAsync(HttpContext.RequestAborted);
    }

    private static object CreateStreamError(Exception exception, string fallbackMessage)
    {
        var status = exception switch
        {
            SpotifyReauthorizationRequiredException => StatusCodes.Status410Gone,
            SpotifyInsufficientScopeException => StatusCodes.Status403Forbidden,
            SpotifyRateLimitExceededException => StatusCodes.Status429TooManyRequests,
            SpotifyAccountNotLinkedException => StatusCodes.Status422UnprocessableEntity,
            SpotifyResourceUnavailableException => StatusCodes.Status404NotFound,
            SpotifyRequestFailedException => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

        var message = status == StatusCodes.Status500InternalServerError
            ? fallbackMessage
            : exception.Message;

        return new
        {
            Type = "error",
            Status = status,
            Message = message
        };
    }
}
