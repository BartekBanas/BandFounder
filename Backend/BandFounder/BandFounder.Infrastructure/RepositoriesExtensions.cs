using BandFounder.Domain.Entities;

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
            throw new ArgumentException("Role name cannot be empty or whitespace");
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
    
    public static async Task<Artist> GetOrCreateAsync(this IRepository<Artist> artistRepository, IRepository<Genre> genreRepository,
        string artistName, List<string>? genres = null, int popularity = 0, string? id = null)
    {
        if (string.IsNullOrWhiteSpace(artistName))
        {
            throw new ArgumentException("Artist name cannot be empty or whitespace");
        }
        
        var artistEntity = await artistRepository.GetOneAsync(artist => artist.Name == artistName,
            includeProperties: nameof(Artist.Genres));

        if (artistEntity is not null) // Check for artist's lacking properties
        {
            if (artistEntity.Popularity == 0 && popularity > 0)
            {
                artistEntity.Popularity = popularity;
            }
            
            if (artistEntity.Genres.Count == 0 && genres is not null)
            {
                foreach (var genreName in genres)
                {
                    var genre = await genreRepository.GetOrCreateAsync(genreName);

                    artistEntity.Genres.Add(genre);
                }
            }
            
            return artistEntity;
        }
        else // Create a new artist
        {
            var newArtist = new Artist
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Name = artistName,
                Popularity = popularity
            };
            
            foreach (var genreName in genres ?? [])
            {
                var genre = await genreRepository.GetOrCreateAsync(genreName);
                
                newArtist.Genres.Add(genre);
            }
            
            await artistRepository.CreateAsync(newArtist);
            return newArtist;
        }
    }

    public static async Task<Artist> GetOrCreateAsync(this IRepository<Artist> artistRepository, string artistName)
    {
        if (string.IsNullOrWhiteSpace(artistName))
        {
            throw new ArgumentException("Artist name cannot be empty or whitespace");
        }
        
        var artistEntity = await artistRepository.GetOneAsync(artist => artist.Name == artistName,
            includeProperties: nameof(Artist.Genres));

        if (artistEntity is not null)
        {
            return artistEntity;
        }

        var newArtist = new Artist
        {
            Id = Guid.NewGuid().ToString(),
            Name = artistName
        };
        
        await artistRepository.CreateAsync(newArtist);
        return newArtist;
    }

    public static async Task<IEnumerable<Artist>> GetOrCreateAsync(this IRepository<Artist> artistRepository,
        IEnumerable<string> artistNames)
    {
        var artists = new List<Artist>();
        
        foreach (var artistName in artistNames)
        {
            artists.Add(await artistRepository.GetOrCreateAsync(artistName));
        }
        
        return artists;
    }

    public static string NormalizeName(this string input)
    {
        // Separators that require capitalization after them
        var separators = new[] { ' ', '&', '-' };

        var result = new System.Text.StringBuilder();
        var capitalizeNext = true;

        foreach (var ch in input.Trim())
        {
            if (separators.Contains(ch))
            {
                result.Append(ch); // Add the separator to the result
                capitalizeNext = true; // Next character should be capitalized
            }
            else
            {
                result.Append(capitalizeNext ? char.ToUpper(ch) : char.ToLower(ch));
                capitalizeNext = false; // Reset capitalization flag after a letter
            }
        }

        // Remove extra spaces between words and return the final result
        return System.Text.RegularExpressions.Regex.Replace(result.ToString(), @"\s+", " ");
    }
}