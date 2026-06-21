using BandFounder.Domain.Entities;

namespace BandFounder.Domain.Repositories;

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
            await repository.SaveChangesAsync();
            
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
            await repository.SaveChangesAsync();
            
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

        var spotifyId = NormalizeSpotifyArtistId(id);

        if (spotifyId is not null)
        {
            var artistById = await artistRepository.GetOneAsync(artist => artist.Id == spotifyId,
                includeProperties: nameof(Artist.Genres));

            if (artistById is not null)
            {
                await EnrichArtistAsync(artistById, genreRepository, artistName, genres, popularity);
                return artistById;
            }
        }

        var artistByName = await artistRepository.GetOneAsync(artist => artist.Name == artistName,
            includeProperties: nameof(Artist.Genres));

        if (artistByName is not null)
        {
            await EnrichArtistAsync(artistByName, genreRepository, artistName, genres, popularity);
            return artistByName;
        }

        var newArtist = new Artist
        {
            Id = spotifyId ?? Guid.NewGuid().ToString(),
            Name = artistName,
            Popularity = popularity
        };

        foreach (var genreName in genres ?? [])
        {
            var genre = await genreRepository.GetOrCreateAsync(genreName);

            newArtist.Genres.Add(genre);
        }

        await artistRepository.CreateAsync(newArtist);
        await artistRepository.SaveChangesAsync();

        return newArtist;
    }

    private static async Task EnrichArtistAsync(Artist artist, IRepository<Genre> genreRepository,
        string artistName, List<string>? genres, int popularity)
    {
        if (artist.Name != artistName)
        {
            artist.Name = artistName;
        }

        if (artist.Popularity == 0 && popularity > 0)
        {
            artist.Popularity = popularity;
        }

        if (artist.Genres.Count == 0 && genres is { Count: > 0 })
        {
            foreach (var genreName in genres)
            {
                var genre = await genreRepository.GetOrCreateAsync(genreName);

                artist.Genres.Add(genre);
            }
        }
    }

    private static string? NormalizeSpotifyArtistId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        const string uriPrefix = "spotify:artist:";
        return id.StartsWith(uriPrefix, StringComparison.OrdinalIgnoreCase)
            ? id[uriPrefix.Length..]
            : id;
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
        await artistRepository.SaveChangesAsync();
        
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