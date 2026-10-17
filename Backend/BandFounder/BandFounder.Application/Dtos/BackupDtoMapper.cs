using BandFounder.Application.Dtos.Backup;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure.Spotify.Dto;

namespace BandFounder.Application.Dtos;

public static class BackupDtoMapper
{
    public static IEnumerable<AccountBackup> ToBackupDto(this IEnumerable<Account> accounts)
    {
        return accounts.Select<Account, AccountBackup>(account => account.ToBackupDto());
    }

    public static AccountBackup ToBackupDto(this Account account)
    {
        return new AccountBackup
        {
            Name = account.Name,
            Email = account.Email,
            EmailOnNewMessage = account.NotificationPreferences.EmailOnNewMessage,
            EmailUnreadDelayMinutes = account.NotificationPreferences.EmailUnreadDelayMinutes,
            ProfilePicture = account.ProfilePicture?.ToBackupDto(),
            SpotifyTokens = account.SpotifyTokens is not null ? new SpotifyTokensDto()
            {
                AccessToken = account.SpotifyTokens.AccessToken,
                RefreshToken = account.SpotifyTokens.RefreshToken,
                ExpirationDate = account.SpotifyTokens.ExpirationDate
            } : null,
            MusicianRoles = account.MusicianRoles.Select(role => role.Name).ToList(),
            Artists = account.HeldArtists().Select(artist => artist.Name).ToList(),
            ArtistLinks = account.ArtistLinks.Select(link => new AccountArtistBackup
            {
                Name = link.Artist.Name,
                Sources = link.Sources.Select(source => new ArtistSourceBackup
                {
                    Kind = source.Kind,
                    LinkedPlaylistId = source.LinkedPlaylistId
                }).ToList()
            }).ToList(),
            LinkedPlaylists = account.LinkedPlaylists.Select(playlist => new LinkedPlaylistBackup
            {
                SpotifyId = playlist.SpotifyId,
                Name = playlist.Name,
                LinkedAt = playlist.LinkedAt
            }).ToList()
        };
    }
    
    public static IEnumerable<ArtistBackup> ToBackupDto(this IEnumerable<Artist> artists)
    {
        return artists.Select<Artist, ArtistBackup>(artist => artist.ToBackupDto());
    }
    
    public static ArtistBackup ToBackupDto(this Artist artist)
    {
        return new ArtistBackup
        {
            Id = artist.Id,
            Name = artist.Name,
            Genres = artist.Genres.Select(genre => genre.Name).ToList(),
            Popularity = artist.Popularity
        };
    }
    
    public static ProfilePictureBackup ToBackupDto(this ProfilePicture? profilePicture)
    {
        return new ProfilePictureBackup
        {
            MimeType = profilePicture.MimeType,
            ImageDataBase64 = Convert.ToBase64String(profilePicture.ImageData)
        };
    }
}