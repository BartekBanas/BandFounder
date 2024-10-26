using BandFounder.Application.Dtos;
using BandFounder.Domain;
using BandFounder.Domain.Entities;

namespace BandFounder.Application.Services.Spotify;

public interface ISpotifyContentManager
{
    Task<Dictionary<string, int>> GetWagedGenres(Guid? userId = null);
    Task<List<ArtistDto>> SaveRelevantArtists();
    Task<List<ArtistDto>> RetrieveSpotifyUsersArtistsAsync();
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

    public async Task<List<ArtistDto>> SaveRelevantArtists()
    {
        var userArtists = await RetrieveSpotifyUsersArtistsAsync();
        var userId = _authenticationService.GetUserId();
        var account = await _accountRepository.GetOneRequiredAsync(userId);
        var savedArtists = new List<ArtistDto>();

        foreach (var artistDto in userArtists)
        {
            var existingArtist = await _artistRepository.GetOneAsync(artistDto.Id);

            if (existingArtist == null)
            {
                var newArtist = await CreateNewArtistAsync(artistDto);
                await _artistRepository.CreateAsync(newArtist);
                savedArtists.Add(artistDto);
                account.Artists.Add(newArtist);
            }
        }

        await _accountRepository.SaveChangesAsync();
        return savedArtists;
    }

    public async Task<List<ArtistDto>> RetrieveSpotifyUsersArtistsAsync()
    {
        var topArtists = await _spotifyContentRetriever.GetTopArtistsAsync();
        var followedArtists = await _spotifyContentRetriever.GetFollowedArtistsAsync();
        return topArtists.Concat(followedArtists).DistinctBy(artist => artist.Id).ToList();
    }

    private async Task<Artist> CreateNewArtistAsync(ArtistDto artistDto)
    {
        var newArtist = new Artist
        {
            Id = artistDto.Id,
            Name = artistDto.Name,
            Popularity = artistDto.Popularity,
            Genres = []
        };

        foreach (var genreName in artistDto.Genres)
        {
            var genre = await _genreRepository.GetOrCreateAsync(genreName);
            
            newArtist.Genres.Add(genre);
        }
        
        return newArtist;
    }
}