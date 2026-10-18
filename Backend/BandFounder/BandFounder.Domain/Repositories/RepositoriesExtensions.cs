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

    /// <summary>
    /// Loads every genre in one query and adds the missing ones. Does not save.
    /// The result is keyed by normalized name.
    /// </summary>
    public static async Task<Dictionary<string, Genre>> GetOrCreateManyAsync(
        this IRepository<Genre> repository, IEnumerable<string> genreNames)
    {
        var names = genreNames
            .Select(name => string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("Genre name cannot be empty or whitespace.")
                : NormalizeName(name))
            .Distinct()
            .ToList();

        if (names.Count == 0)
        {
            return [];
        }

        var genres = (await repository.GetAsync(genre => names.Contains(genre.Name)))
            .ToDictionary(genre => genre.Name);

        foreach (var name in names.Where(name => !genres.ContainsKey(name)))
        {
            var genre = new Genre { Name = name };
            await repository.CreateAsync(genre);
            genres.Add(name, genre);
        }

        return genres;
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

    /// <summary>
    /// Matches each input to an existing artist (by Spotify id, then by name) or adds a new one,
    /// using a single query for the whole batch. Returns artists in input order. Does not save.
    /// Matching is against the catalog as loaded, so a rename done later by <see cref="Artist.Enrich"/>
    /// does not affect which artist a later input in the same batch resolves to.
    /// </summary>
    public static async Task<List<Artist>> ResolveManyAsync(
        this IRepository<Artist> artistRepository, IReadOnlyList<ArtistData> artistData)
    {
        if (artistData.Any(data => string.IsNullOrWhiteSpace(data.Name)))
        {
            throw new ArgumentException("Artist name cannot be empty or whitespace.");
        }

        if (artistData.Count == 0)
        {
            return [];
        }

        var spotifyIds = artistData.Select(data => data.SpotifyId).OfType<string>().Distinct().ToList();
        var names = artistData.Select(data => data.Name).Distinct().ToList();
        var known = (await artistRepository.GetAsync(
                artist => spotifyIds.Contains(artist.Id) || names.Contains(artist.Name),
                includeProperties: nameof(Artist.Genres)))
            .ToList();
        var byId = known.ToDictionary(artist => artist.Id);
        var byName = known.GroupBy(artist => artist.Name).ToDictionary(group => group.Key, group => group.First());

        var artists = new List<Artist>(artistData.Count);
        foreach (var data in artistData)
        {
            var artist = (data.SpotifyId is { } id ? byId.GetValueOrDefault(id) : null)
                         ?? byName.GetValueOrDefault(data.Name);

            if (artist is null)
            {
                artist = new Artist { Id = data.SpotifyId ?? Guid.NewGuid().ToString(), Name = data.Name };
                await artistRepository.CreateAsync(artist);
                byId.Add(artist.Id, artist);
                byName.Add(artist.Name, artist);
            }

            artists.Add(artist);
        }

        return artists;
    }

    /// <summary>
    /// Resolves the artists, then fills in popularity and genres that are still missing. Existing values are kept.
    /// Does not save. Returns artists in input order.
    /// </summary>
    public static async Task<List<Artist>> GetOrCreateManyAsync(
        this IRepository<Artist> artistRepository,
        IRepository<Genre> genreRepository,
        IReadOnlyList<ArtistData> artistData)
    {
        var artists = await artistRepository.ResolveManyAsync(artistData);
        var pairs = artists.Zip(artistData, (artist, data) => (Artist: artist, Data: data)).ToList();

        // Decided before enriching, so the first input for an artist is the one that supplies its genres.
        var needingGenres = pairs
            .Where(pair => pair.Artist.Genres.Count == 0)
            .DistinctBy(pair => pair.Artist)
            .ToList();
        var genres = await genreRepository.GetOrCreateManyAsync(needingGenres.SelectMany(pair => pair.Data.Genres));

        foreach (var (artist, data) in pairs)
        {
            artist.Enrich(data.Name, data.Popularity);
        }

        foreach (var (artist, data) in needingGenres)
        {
            artist.Genres.AddRange(data.Genres.Select(genre => genres[NormalizeName(genre)]).Distinct());
        }

        return artists;
    }

    public static async Task<Artist> GetOrCreateAsync(this IRepository<Artist> artistRepository, string artistName)
    {
        return (await artistRepository.GetOrCreateAsync(new[] { artistName })).Single();
    }

    public static async Task<IEnumerable<Artist>> GetOrCreateAsync(this IRepository<Artist> artistRepository,
        IEnumerable<string> artistNames)
    {
        var artists = await artistRepository.ResolveManyAsync(
            artistNames.Select(name => new ArtistData(null, name)).ToList());
        await artistRepository.SaveChangesAsync();

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
