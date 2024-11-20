using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;

namespace BandFounder.Application.Services;

public interface ISpotifyConnectionService
{
    Task LinkAccountToSpotify(SpotifyConnectionDto dto, Guid? accountId = null);
    Task<List<SpotifyArtistDto>> SaveRelevantArtists();
    Task<List<SpotifyArtistDto>> RetrieveSpotifyUsersArtistsAsync();
}

public class SpotifyConnectionService : ISpotifyConnectionService
{
    private readonly ISpotifyContentRetriever _spotifyContentRetriever;
    private readonly ISpotifyTokenService _spotifyTokenService;
    private readonly IAuthenticationService _authenticationService;

    private readonly IRepository<Artist> _artistRepository;
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<Genre> _genreRepository;

    public SpotifyConnectionService(
        ISpotifyContentRetriever spotifyContentRetriever,
        ISpotifyTokenService spotifyTokenService,
        IAuthenticationService authenticationService,
        IRepository<Artist> artistRepository,
        IRepository<Account> accountRepository,
        IRepository<Genre> genreRepository)
    {
        _spotifyContentRetriever = spotifyContentRetriever;
        _spotifyTokenService = spotifyTokenService;
        _authenticationService = authenticationService;
        _artistRepository = artistRepository;
        _accountRepository = accountRepository;
        _genreRepository = genreRepository;
    }

    public async Task LinkAccountToSpotify(SpotifyConnectionDto dto, Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();
        var tokensResponse = await _spotifyTokenService.RequestSpotifyTokens(dto);
        
        var tokensDto = new SpotifyTokensDto
        {
            AccessToken = tokensResponse.AccessToken,
            RefreshToken = tokensResponse.RefreshToken,
            ExpirationDate = DateTime.UtcNow.AddSeconds(tokensResponse.ExpiresIn - 60)
        };
        
        await _spotifyTokenService.CreateSpotifyTokens(tokensDto, (Guid)accountId);
        await SaveRelevantArtists();
    }

    public async Task<List<SpotifyArtistDto>> SaveRelevantArtists()
    {
        var userArtists = await RetrieveSpotifyUsersArtistsAsync();
        var userId = _authenticationService.GetUserId();
        var account = await _accountRepository.GetOneRequiredAsync(key: userId,
            keyPropertyName: nameof(Account.Id), includeProperties: nameof(Account.Artists));
        
        var savedArtists = new List<SpotifyArtistDto>();

        foreach (var artistDto in userArtists)
        {
            var artistEntity = await _artistRepository.GetOrCreateAsync(_genreRepository,
                artistDto.Name, artistDto.Genres, artistDto.Popularity, artistDto.Id);

            if (account.Artists.All(artist => artist.Id != artistEntity.Id))
            {
                account.Artists.Add(artistEntity);
                savedArtists.Add(artistDto);
            }
        }

        await _accountRepository.SaveChangesAsync();
        return savedArtists;
    }

    public async Task<List<SpotifyArtistDto>> RetrieveSpotifyUsersArtistsAsync()
    {
        var userId = _authenticationService.GetUserId();
        
        var topArtists = await _spotifyContentRetriever.GetTopArtistsAsync(userId);
        var followedArtists = await _spotifyContentRetriever.GetFollowedArtistsAsync(userId);
        
        return topArtists.Concat(followedArtists).DistinctBy(artist => artist.Id).ToList();
    }
}