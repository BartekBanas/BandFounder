using BandFounder.Application.Dtos.Spotify;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Entities.Spotify;

namespace BandFounder.Application.Services.Spotify;

public interface ISpotifyContentManager
{
    Task<List<ArtistDto>> SaveRelevantArtists();
}

public class SpotifyContentManager : ISpotifyContentManager
{
    private readonly ISpotifyContentRetriever _spotifyContentRetriever;
    private readonly IUserAuthenticationService _userAuthenticationService;
    
    private readonly IRepository<Artist> _artistRepository;
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<Genre> _genreRepository;

    public SpotifyContentManager(
        ISpotifyContentRetriever spotifyContentRetriever,
        IUserAuthenticationService userAuthenticationService, 
        IRepository<Artist> artistRepository,
        IRepository<Account> accountRepository,
        IRepository<Genre> genreRepository)
    {
        _spotifyContentRetriever = spotifyContentRetriever;
        _userAuthenticationService = userAuthenticationService;
        _artistRepository = artistRepository;
        _accountRepository = accountRepository;
        _genreRepository = genreRepository;
    }

    public async Task<List<ArtistDto>> SaveRelevantArtists()
    {
        var topArtists = await _spotifyContentRetriever.GetTopArtistsAsync();
        var followedArtists = await _spotifyContentRetriever.GetFollowedArtistsAsync();

        var userId = _userAuthenticationService.GetUserId();
        var account = await _accountRepository.GetOneRequiredAsync(userId);

        var usersArtists = topArtists.Concat(followedArtists).DistinctBy(artist => artist.Id).ToList();

        var savedArtists = new List<ArtistDto>();

        foreach (var artistDto in usersArtists)
        {
            // Check if artist already exists in the database
            var existingArtist = await _artistRepository.GetOneAsync(artistDto.Id);

            if (existingArtist == null)
            {
                // If the artist does not exist, create a new Artist entity
                var newArtist = new Artist
                {
                    Id = artistDto.Id,
                    Name = artistDto.Name,
                    Popularity = artistDto.Popularity,
                    Genres = []
                };

                // Check and add genres
                foreach (var genreName in artistDto.Genres)
                {
                    // Check if genre already exists
                    var existingGenre = await _genreRepository.GetOneAsync(genreName);
                    if (existingGenre == null)
                    {
                        // If the genre does not exist, create a new Genre entity
                        var newGenre = new Genre { Name = genreName };
                        await _genreRepository.CreateAsync(newGenre);
                        newArtist.Genres.Add(newGenre);
                    }
                    else
                    {
                        // If the genre already exists, add it to the artist's genres
                        newArtist.Genres.Add(existingGenre);
                    }
                }

                // Add the new artist to the DbSet
                await _artistRepository.CreateAsync(newArtist);
                savedArtists.Add(artistDto); // Add to saved list

                // Add new artist to the account's artist collection
                account.Artists.Add(newArtist);
            }
            else
            {
                // Artist already exists, add existing artist to the account's artist collection
                if (account.Artists.All(artist => artist.Id != existingArtist.Id))
                {
                    account.Artists.Add(existingArtist);
                }

                // Optionally add to savedArtists if you want to keep track of all relevant artists
                savedArtists.Add(artistDto);
            }
        }

        await _accountRepository.SaveChangesAsync();

        return savedArtists;
    }
}