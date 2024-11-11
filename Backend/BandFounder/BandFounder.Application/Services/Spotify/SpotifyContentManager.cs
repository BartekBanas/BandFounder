using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;

namespace BandFounder.Application.Services.Spotify;

public interface ISpotifyContentManager
{
    Task<Dictionary<string, int>> GetWagedGenres(Guid? userId = null);
    Task<List<SpotifyArtistDto>> SaveRelevantArtists();
    Task<List<SpotifyArtistDto>> RetrieveSpotifyUsersArtistsAsync();
}

public class SpotifyContentManager : ISpotifyContentManager
{
    private readonly ISpotifyContentRetriever _spotifyContentRetriever;
    private readonly IAuthenticationService _authenticationService;

    private readonly IRepository<Artist> _artistRepository;
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<Genre> _genreRepository;

    public SpotifyContentManager(
        ISpotifyContentRetriever spotifyContentRetriever,
        IAuthenticationService authenticationService,
        IRepository<Artist> artistRepository,
        IRepository<Account> accountRepository,
        IRepository<Genre> genreRepository)
    {
        _spotifyContentRetriever = spotifyContentRetriever;
        _authenticationService = authenticationService;
        _artistRepository = artistRepository;
        _accountRepository = accountRepository;
        _genreRepository = genreRepository;
    }
    
    public async Task<Dictionary<string, int>> GetWagedGenres(Guid? userId = null)
    {
        var targetUserId = userId ?? _authenticationService.GetUserId();

        var account = await _accountRepository.GetOneRequiredAsync(
            targetUserId, nameof(Artist.Id), "Artists", "Artists.Genres");
        
        var wagedGenres = new Dictionary<string, int>();

        foreach (var genre in account.Artists.SelectMany(artist => artist.Genres))
        {
            if (!wagedGenres.TryAdd(genre.Name, 1))
            {
                wagedGenres[genre.Name]++;
            }
        }

        var sortedWagedGenres = wagedGenres
            .OrderByDescending(genre => genre.Value)
            .ToDictionary(genre => genre.Key, genre => genre.Value);

        return sortedWagedGenres;
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