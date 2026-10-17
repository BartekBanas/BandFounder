using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Backup;
using BandFounder.Domain.Entities;

namespace Services.Tests;

public class AccountBackupRestoreTests
{
    [Test]
    public void ToBackupDto_IncludesLinkedPlaylistsAndArtistSources()
    {
        var account = CreateAccount();
        var artist = new Artist { Id = "radiohead", Name = "Radiohead" };
        account.HoldArtist(artist, ArtistSource.Manual());
        account.HoldArtist(artist, ArtistSource.FromLinkedPlaylist("playlist-1"));
        account.LinkedPlaylists.Add(new LinkedPlaylist
        {
            AccountId = account.Id,
            SpotifyId = "playlist-1",
            Name = "Road trip",
            LinkedAt = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc)
        });

        var backup = account.ToBackupDto();

        Assert.That(backup.Artists, Is.EqualTo(new[] { "Radiohead" }));
        Assert.That(backup.LinkedPlaylists, Has.Count.EqualTo(1));
        Assert.That(backup.LinkedPlaylists![0].SpotifyId, Is.EqualTo("playlist-1"));
        Assert.That(backup.LinkedPlaylists[0].Name, Is.EqualTo("Road trip"));
        Assert.That(backup.ArtistLinks, Has.Count.EqualTo(1));
        Assert.That(backup.ArtistLinks![0].Name, Is.EqualTo("Radiohead"));
        Assert.That(backup.ArtistLinks[0].Sources.Select(source => source.ToSource()),
            Is.EquivalentTo(new[]
            {
                ArtistSource.Manual(),
                ArtistSource.FromLinkedPlaylist("playlist-1")
            }));
    }

    [Test]
    public void ToBackupDto_ProjectsLegacyArtistNamesFromLinks()
    {
        var account = CreateAccount();
        var artist = new Artist { Id = "radiohead", Name = "Radiohead" };
        account.HoldArtist(artist, ArtistSource.Manual());
        account.Artists.Clear();

        var backup = account.ToBackupDto();

        Assert.That(account.Artists, Is.Empty);
        Assert.That(backup.Artists, Is.EqualTo(new[] { "Radiohead" }));
        Assert.That(backup.ArtistLinks, Has.Count.EqualTo(1));
    }

    [Test]
    public void ApplyMusicProfile_TreatsLegacyArtistNamesAsManual()
    {
        var account = CreateAccount();
        var artist = new Artist { Id = "radiohead", Name = "Radiohead" };
        var backup = new AccountBackup
        {
            Name = account.Name,
            Email = account.Email,
            Artists = ["Radiohead"]
        };

        AccountBackupRestore.ApplyMusicProfile(
            account,
            backup,
            new Dictionary<string, Artist> { ["Radiohead"] = artist });

        Assert.That(account.LinkedPlaylists, Is.Empty);
        Assert.That(account.Artists, Has.Count.EqualTo(1));
        Assert.That(account.ArtistLinks[0].Sources[0].ToSource(), Is.EqualTo(ArtistSource.Manual()));
    }

    [Test]
    public void ApplyMusicProfile_RestoresLinkedPlaylistsAndSources()
    {
        var account = CreateAccount();
        var artist = new Artist { Id = "radiohead", Name = "Radiohead" };
        var linkedAt = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
        var backup = new AccountBackup
        {
            Name = account.Name,
            Email = account.Email,
            Artists = ["Radiohead"],
            LinkedPlaylists =
            [
                new LinkedPlaylistBackup
                {
                    SpotifyId = "playlist-1",
                    Name = "Road trip",
                    LinkedAt = linkedAt
                }
            ],
            ArtistLinks =
            [
                new AccountArtistBackup
                {
                    Name = "Radiohead",
                    Sources =
                    [
                        new ArtistSourceBackup { Kind = ArtistSourceKind.SpotifySignIn },
                        new ArtistSourceBackup
                        {
                            Kind = ArtistSourceKind.LinkedPlaylist,
                            LinkedPlaylistId = "playlist-1"
                        }
                    ]
                }
            ]
        };

        AccountBackupRestore.ApplyMusicProfile(
            account,
            backup,
            new Dictionary<string, Artist> { ["Radiohead"] = artist });

        Assert.That(account.LinkedPlaylists, Has.Count.EqualTo(1));
        Assert.That(account.LinkedPlaylists[0].SpotifyId, Is.EqualTo("playlist-1"));
        Assert.That(account.LinkedPlaylists[0].Name, Is.EqualTo("Road trip"));
        Assert.That(account.LinkedPlaylists[0].LinkedAt, Is.EqualTo(linkedAt));
        Assert.That(account.ArtistLinks[0].Sources.Select(source => source.ToSource()),
            Is.EquivalentTo(new[]
            {
                ArtistSource.SpotifySignIn(),
                ArtistSource.FromLinkedPlaylist("playlist-1")
            }));
    }

    [Test]
    public void ArtistNamesToRestore_PrefersArtistLinksWhenPresent()
    {
        var backup = new AccountBackup
        {
            Name = "tester",
            Email = "tester@example.com",
            Artists = ["Legacy Name"],
            ArtistLinks = [new AccountArtistBackup { Name = "Radiohead" }]
        };

        Assert.That(AccountBackupRestore.ArtistNamesToRestore(backup), Is.EqualTo(new[] { "Radiohead" }));
    }

    private static Account CreateAccount()
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            Name = "tester",
            Email = "tester@example.com",
            PasswordHash = "hash"
        };
    }
}
