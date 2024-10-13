using BandFounder.Domain.Entities;

namespace BandFounder.Domain;

public static class RepositoriesExtensions
{
    public static async Task TryAdd(this IRepository<Genre> repository, string genreName)
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
            await repository.CreateAsync(new Genre { Name = normalizedGenreName });
        }
    }
    
    public static async Task<MusicianRole> GetOrCreateAsync(this IRepository<MusicianRole> repository, string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name cannot be empty or whitespace.");
        }

        var normalizedRoleName = NormalizeName(roleName);

        var existingRole = await repository.GetOneAsync(musicianRole => musicianRole.RoleName == normalizedRoleName);
        
        if (existingRole is null)
        {
            var newRole = new MusicianRole { RoleName = normalizedRoleName };
            await repository.CreateAsync(newRole);
            return newRole;
        }
        else
        {
            return existingRole;
        }
    }

    public static string NormalizeName(string input)
    {
        // Split by spaces, remove extra spaces, and capitalize each word
        var words = input.Trim()
            .Split([' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower());
    
        // Join words back into a single string with a single space separating them
        return string.Join(" ", words);
    }
}