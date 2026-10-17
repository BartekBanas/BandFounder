using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Backup;

public static class AccountBackupRestore
{
    public static void ApplyMusicProfile(
        Account account,
        AccountBackup backup,
        IReadOnlyDictionary<string, Artist> artistsByName)
    {
        foreach (var playlist in backup.LinkedPlaylists ?? [])
        {
            account.LinkedPlaylists.Add(new LinkedPlaylist
            {
                AccountId = account.Id,
                SpotifyId = playlist.SpotifyId,
                Name = playlist.Name,
                LinkedAt = playlist.LinkedAt == default ? DateTime.UtcNow : playlist.LinkedAt
            });
        }

        foreach (var link in LinksToRestore(backup))
        {
            var artist = artistsByName[link.Name];
            foreach (var source in link.Sources)
            {
                account.HoldArtist(artist, source.ToSource());
            }
        }
    }

    public static IEnumerable<string> ArtistNamesToRestore(AccountBackup backup)
    {
        return LinksToRestore(backup).Select(link => link.Name);
    }

    private static IEnumerable<AccountArtistBackup> LinksToRestore(AccountBackup backup)
    {
        if (backup.ArtistLinks is { Count: > 0 })
        {
            foreach (var link in backup.ArtistLinks)
            {
                yield return link.Sources is { Count: > 0 }
                    ? link
                    : new AccountArtistBackup
                    {
                        Name = link.Name,
                        Sources = [new ArtistSourceBackup { Kind = ArtistSourceKind.Manual }]
                    };
            }

            yield break;
        }

        foreach (var name in backup.Artists ?? [])
        {
            yield return new AccountArtistBackup
            {
                Name = name,
                Sources = [new ArtistSourceBackup { Kind = ArtistSourceKind.Manual }]
            };
        }
    }
}
