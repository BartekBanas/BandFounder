using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Listings;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;

namespace BandFounder.Application.Services;

public interface IContentService
{
    Task<IEnumerable<string>> GetGenresAsync();
    Task<IEnumerable<ArtistDto>> GetArtistsAsync();
    Task<IEnumerable<string>> GetMusicianRoles();
}

public class ContentService : IContentService
{
    private readonly IRepository<Genre> _genreRepository;
    private readonly IRepository<Artist> _artistRepository;
    private readonly IRepository<MusicianRole> _musicianRoleRepository;

    public ContentService(
        IRepository<Genre> genreRepository, 
        IRepository<Artist> artistRepository, 
        IRepository<MusicianRole> musicianRoleRepository)
    {
        _genreRepository = genreRepository;
        _artistRepository = artistRepository;
        _musicianRoleRepository = musicianRoleRepository;
    }
    
    public async Task<IEnumerable<string>> GetGenresAsync()
    {
        var genres = await _genreRepository.GetAsync();
        return genres.Select(genre => genre.Name);
    }
    
    public async Task<IEnumerable<ArtistDto>> GetArtistsAsync()
    {
        return (await _artistRepository.GetAsync(includeProperties: nameof(Artist.Genres))).ToDto();
    }
    
    public async Task<IEnumerable<string>> GetMusicianRoles()
    {
        var roles = await _musicianRoleRepository.GetAsync();
        return roles.Select(role => role.Name);
    }
}