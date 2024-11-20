using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;

namespace BandFounder.Application.Services;

public interface IContentService
{
    Task<IEnumerable<string>> GetGenresAsync();
    Task<Dictionary<string, int>> GetWagedGenres(Guid userId);
    Task<IEnumerable<ArtistDto>> GetArtistsAsync();
    Task<IEnumerable<string>> GetMusicianRoles();
}

public class ContentService(
    IRepository<Account> accountRepository,
    IRepository<Genre> genreRepository,
    IRepository<Artist> artistRepository,
    IRepository<MusicianRole> musicianRoleRepository)
    : IContentService
{
    public async Task<IEnumerable<string>> GetGenresAsync()
    {
        var genres = await genreRepository.GetAsync();
        return genres.Select(genre => genre.Name);
    }
    
    public async Task<Dictionary<string, int>> GetWagedGenres(Guid userId)
    {
        var account = await accountRepository.GetOneRequiredAsync(key: userId,
            keyPropertyName: nameof(Artist.Id), includeProperties: ["Artists", "Artists.Genres"]);
        
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
    
    public async Task<IEnumerable<ArtistDto>> GetArtistsAsync()
    {
        return (await artistRepository.GetAsync(includeProperties: nameof(Artist.Genres))).ToDto();
    }
    
    public async Task<IEnumerable<string>> GetMusicianRoles()
    {
        var roles = await musicianRoleRepository.GetAsync();
        return roles.Select(role => role.Name);
    }
}