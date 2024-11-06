using BandFounder.Domain.Entities;
using BandFounder.Infrastructure.Spotify.Dto;

namespace BandFounder.Infrastructure;

public static class RepositoriesExtensions
{
    public static async Task<Genre> GetOrCreateAsync(this IRepository<Genre> repository, string genreName)
    {
        // Validate input: check if it's null, empty or contains only white spaces
        if (string.IsNullOrWhiteSpace(genreName))
        {
            throw new ArgumentException("Genre name cannot be empty or whitespace.");
        }

        // Normalize input: trim extra spaces and capitalize each word
        var normalizedGenreName = NormalizeName(genreName);

        // Check if genre already exists in the database
        var existingGenre = await repository.GetOneAsync(normalizedGenreName);
    
        // If genre does not exist, create a new genre
        if (existingGenre is null)
        {
            var newGenre = new Genre { Name = normalizedGenreName };
            await repository.CreateAsync(newGenre);
            
            return newGenre;
        }
        else
        {
            return existingGenre;
        }
    }
    
    public static async Task<MusicianRole> GetOrCreateAsync(this IRepository<MusicianRole> repository, string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name cannot be empty or whitespace.");
        }

        var normalizedRoleName = NormalizeName(roleName);

        var existingRole = await repository.GetOneAsync(musicianRole => musicianRole.Name == normalizedRoleName);
        
        if (existingRole is null)
        {
            var newRole = new MusicianRole { Name = normalizedRoleName };
            await repository.CreateAsync(newRole);
            return newRole;
        }
        else
        {
            return existingRole;
        }
    }
    
    public static async Task<Artist> GetOrCreateAsync(this IRepository<Artist> repository, 
        SpotifyArtistDto spotifyArtistDto, IRepository<Genre> genreRepository)
    {
        var artist = await repository.GetOneAsync(spotifyArtistDto.Id);
        
        if (artist is null) 
        {
            var newArtist = new Artist
            {
                Id = spotifyArtistDto.Id,
                Name = spotifyArtistDto.Name,
                Popularity = spotifyArtistDto.Popularity
            };
            
            foreach (var genreName in spotifyArtistDto.Genres)
            {
                var genre = await genreRepository.GetOrCreateAsync(genreName);
            
                newArtist.Genres.Add(genre);
            }
            
            await repository.CreateAsync(newArtist);
            return newArtist;
        }
        else
        {
            return artist;
        }
    }

    public static string NormalizeName(this string input)
    {
        // Split by spaces, remove extra spaces, and capitalize each word
        var words = input.Trim()
            .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower());
    
        // Join words back into a single string with a single space separating them
        return string.Join(" ", words);
    }
}