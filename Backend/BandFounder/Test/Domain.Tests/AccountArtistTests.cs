using System.Runtime.CompilerServices;
using BandFounder.Domain.Entities;

namespace Domain.Tests;

public class AccountArtistTests
{
    [Test]
    public void HoldArtist_LinksArtistWithGivenSource()
    {
        var account = CreateAccount();
        var artist = CreateArtist("radiohead");

        var isNew = account.HoldArtist(artist, ArtistSource.Manual());

        Assert.That(isNew, Is.True);
        Assert.That(account.Artists, Has.Count.EqualTo(1));
        Assert.That(account.ArtistLinks, Has.Count.EqualTo(1));
        Assert.That(account.ArtistLinks[0].Sources, Has.Count.EqualTo(1));
        Assert.That(account.ArtistLinks[0].Sources[0].ToSource(), Is.EqualTo(ArtistSource.Manual()));
    }

    [Test]
    public void HoldArtist_AddsAnotherSourceWithoutDuplicatingTheArtist()
    {
        var account = CreateAccount();
        var artist = CreateArtist("radiohead");
        account.HoldArtist(artist, ArtistSource.Manual());

        var isNew = account.HoldArtist(artist, ArtistSource.SpotifySignIn());

        Assert.That(isNew, Is.False);
        Assert.That(account.Artists, Has.Count.EqualTo(1));
        Assert.That(account.ArtistLinks, Has.Count.EqualTo(1));
        Assert.That(account.ArtistLinks[0].Sources.Select(source => source.ToSource()),
            Is.EquivalentTo(new[] { ArtistSource.Manual(), ArtistSource.SpotifySignIn() }));
    }

    [Test]
    public void HoldArtist_DoesNotDuplicateTheSameSource()
    {
        var account = CreateAccount();
        var artist = CreateArtist("radiohead");
        account.HoldArtist(artist, ArtistSource.Manual());

        account.HoldArtist(artist, ArtistSource.Manual());

        Assert.That(account.ArtistLinks[0].Sources, Has.Count.EqualTo(1));
    }

    [Test]
    public void HoldArtist_RecordsALinkedPlaylistSourceByPlaylistId()
    {
        var account = CreateAccount();
        var artist = CreateArtist("radiohead");
        var playlistSource = ArtistSource.FromLinkedPlaylist("playlist-1");

        account.HoldArtist(artist, playlistSource);
        account.HoldArtist(artist, ArtistSource.FromLinkedPlaylist("playlist-2"));

        Assert.That(account.Artists, Has.Count.EqualTo(1));
        Assert.That(account.ArtistLinks[0].Sources.Select(source => source.ToSource()),
            Is.EquivalentTo(new[]
            {
                playlistSource,
                ArtistSource.FromLinkedPlaylist("playlist-2")
            }));
    }

    [Test]
    public void FromLinkedPlaylist_RequiresPlaylistId()
    {
        var ex = Assert.Throws<ArgumentException>(() => ArtistSource.FromLinkedPlaylist("  "));

        Assert.That(ex!.ParamName, Is.EqualTo("playlistId"));
    }

    [Test]
    public void HoldArtist_CreatesLinkWhenSkipNavigationAlreadyHasArtist()
    {
        var account = CreateAccount();
        var artist = CreateArtist("radiohead");
        account.Artists.Add(artist);

        var isNew = account.HoldArtist(artist, ArtistSource.Manual());

        Assert.That(isNew, Is.True);
        Assert.That(account.Artists, Has.Count.EqualTo(1));
        Assert.That(account.ArtistLinks, Has.Count.EqualTo(1));
        Assert.That(account.ArtistLinks[0].Sources[0].ToSource(), Is.EqualTo(ArtistSource.Manual()));
        Assert.That(account.HeldArtists().Single().Id, Is.EqualTo(artist.Id));
    }

    [Test]
    public void HoldArtist_TreatsExistingLinkAsAlreadyHeldWhenSkipNavigationIsEmpty()
    {
        var account = CreateAccount();
        var artist = CreateArtist("radiohead");
        account.ArtistLinks.Add(new AccountArtist
        {
            AccountId = account.Id,
            ArtistId = artist.Id,
            Artist = artist,
            Account = account
        });
        account.ArtistLinks[0].AddSource(ArtistSource.Manual());

        var isNew = account.HoldArtist(artist, ArtistSource.SpotifySignIn());

        Assert.That(isNew, Is.False);
        Assert.That(account.Artists, Is.Empty);
        Assert.That(account.ArtistLinks, Has.Count.EqualTo(1));
        Assert.That(account.ArtistLinks[0].Sources.Select(source => source.ToSource()),
            Is.EquivalentTo(new[] { ArtistSource.Manual(), ArtistSource.SpotifySignIn() }));
    }

    [Test]
    public void From_RejectsUnknownKind()
    {
        Assert.Throws<SwitchExpressionException>(() => ArtistSource.From((ArtistSourceKind)42, null));
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

    private static Artist CreateArtist(string id)
    {
        return new Artist
        {
            Id = id,
            Name = "Radiohead"
        };
    }
}
