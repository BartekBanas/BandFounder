using System.Text.Json;
using BandFounder.Application.Dtos.Spotify;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Entities.Spotify;

namespace BandFounder.Application.Services.Spotify;

public interface ISpotifyContentService
{
    Task<List<ArtistDto>> SaveRelevantArtists();
    Task<List<ArtistDto>> GetTopArtistsAsync();
    Task<List<ArtistDto>> GetFollowedArtistsAsync();
}

public class SpotifyContentService : ISpotifyContentService
{
    private const string SpotifyTopArtistsUrl = "https://api.spotify.com/v1/me/top/artists?limit=50";
    private const string SpotifyFollowedArtistsUrl = "https://api.spotify.com/v1/me/following?type=artist";
    
    private readonly ISpotifyCredentialsService _credentialsService;
    private readonly IUserAuthenticationService _userAuthenticationService;

    private readonly IRepository<Artist> _artistRepository;
    private readonly IRepository<Account> _accountRepository;

    public SpotifyContentService(
        ISpotifyCredentialsService credentialsService,
        IUserAuthenticationService userAuthenticationService,
        IRepository<Artist> artistRepository, 
        IRepository<Account> accountRepository)
    {
        _credentialsService = credentialsService;
        _userAuthenticationService = userAuthenticationService;
        _artistRepository = artistRepository;
        _accountRepository = accountRepository;
    }

    public async Task<List<ArtistDto>> SaveRelevantArtists()
    {
        var topArtists = await GetTopArtistsAsync();
        var followedArtists = await GetFollowedArtistsAsync();

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
                    Genres = artistDto.Genres.Select(genre => new Genre { Name = genre }).ToList()
                };

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

        return savedArtists;
    }

    public async Task<List<ArtistDto>> GetTopArtistsAsync()
    {
        var accessToken = await _credentialsService.GetAccessTokenAsync();
        
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, SpotifyTopArtistsUrl);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await client.SendAsync(request);
        
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        
        var responseDto = JsonSerializer.Deserialize<TopArtistsResponse>(responseBody) ?? throw new InvalidOperationException();
        
        return responseDto.Items;
    }
    
    public async Task<List<ArtistDto>> GetFollowedArtistsAsync()
    {
        var accessToken = await _credentialsService.GetAccessTokenAsync();
        
        var url = SpotifyFollowedArtistsUrl;
        var followedArtists = new List<ArtistDto>();

        using var client = new HttpClient();
        do
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var responseDto = JsonSerializer.Deserialize<FollowedArtistsResponse>(responseBody) ?? throw new InvalidOperationException();

            followedArtists.AddRange(responseDto.Artists.Items);

            url = responseDto.Artists.Next;

        } while (!string.IsNullOrEmpty(url));

        return followedArtists;
    }
}