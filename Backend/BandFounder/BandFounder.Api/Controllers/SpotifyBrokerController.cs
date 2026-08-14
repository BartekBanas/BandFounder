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
            var error = CreateStreamError(ex);
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

    private static object CreateStreamError(Exception exception)
    {
        var status = exception switch
        {
            SpotifyReauthorizationRequiredException => StatusCodes.Status410Gone,
            SpotifyInsufficientScopeException => StatusCodes.Status403Forbidden,
            SpotifyRateLimitExceededException => StatusCodes.Status429TooManyRequests,
            SpotifyAccountNotLinkedException => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

        var message = status == StatusCodes.Status500InternalServerError
            ? "Something went wrong while scanning liked songs."
            : exception.Message;

        return new
        {
            Type = "error",
            Status = status,
            Message = message
        };
    }
}
