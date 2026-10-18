using System.Linq.Expressions;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using BandFounder.Infrastructure.Spotify.Dto;
using BandFounder.Infrastructure.Spotify.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Services.Tests;

[TestFixture]
public class SpotifyPlaylistLinkServiceTests
{
    private const string PlaylistId = "37i9dQZF1DXcBWIGoYBM5M";
    private const string PlaylistInput = "https://open.spotify.com/playlist/37i9dQZF1DXcBWIGoYBM5M";
    private const string OtherPlaylistId = "37i9dQZF1DX0XUsuxWHRQd";
    private const string OtherPlaylistInput = "https://open.spotify.com/playlist/37i9dQZF1DX0XUsuxWHRQd";
    private const string ShortLinkInput = "https://spotify.link/abc123";

    private ISpotifyPlaylistReadService _playlistReadService;
    private ISpotifyPlaylistReadTokenProvider _tokenProvider;
    private ISpotifyClient _spotifyClient;
    private IRepository<Account> _accountRepository;
    private IRepository<Artist> _artistRepository;
    private IRepository<Genre> _genreRepository;
    private IUnitOfWork _unitOfWork;
    private List<Artist> _artistCatalog;
    private List<Genre> _genreCatalog;
    private SpotifyPlaylistLinkService _service;
    private Guid _accountId;
    private Account _account;

    [SetUp]
    public void SetUp()
    {
        _playlistReadService = Substitute.For<ISpotifyPlaylistReadService>();
        _tokenProvider = Substitute.For<ISpotifyPlaylistReadTokenProvider>();
        _spotifyClient = Substitute.For<ISpotifyClient>();
        _accountRepository = Substitute.For<IRepository<Account>>();
        _artistRepository = Substitute.For<IRepository<Artist>>();
        _genreRepository = Substitute.For<IRepository<Genre>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _artistCatalog = [];
        _genreCatalog = [];
        _accountId = Guid.NewGuid();
        _account = NewAccount(_accountId);

        _tokenProvider.GetAccessTokenAsync().Returns("token");
        _spotifyClient.GetArtistsByIdsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns([]);
        _accountRepository.GetOneRequiredAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string[]>()).Returns(_account);
        StubArtistCatalog();
        _genreRepository.GetAsync(
                Arg.Any<Expression<Func<Genre, bool>>>(),
                Arg.Any<Func<IQueryable<Genre>, IOrderedQueryable<Genre>>>(),
                Arg.Any<string[]>())
            .Returns(call =>
            {
                var predicate = call.Arg<Expression<Func<Genre, bool>>>().Compile();
                return _genreCatalog.Where(predicate).ToList();
            });
        _genreRepository.CreateAsync(Arg.Any<Genre>()).Returns(call =>
        {
            var genre = call.Arg<Genre>();
            _genreCatalog.Add(genre);
            return genre;
        });
        _unitOfWork.ExecuteSerializableTransactionAsync(Arg.Any<Func<Task>>())
            .Returns(call => call.Arg<Func<Task>>()());

        _service = new SpotifyPlaylistLinkService(
            _playlistReadService,
            _tokenProvider,
            _spotifyClient,
            _accountRepository,
            _artistRepository,
            _genreRepository,
            _unitOfWork);
    }

    [Test]
    public async Task LinkAsync_PersistsPlaylistAndCountsNewArtists()
    {
        var held = new Artist { Id = "artist-held", Name = "Held" };
        _artistCatalog.Add(held);
        _account.HoldArtist(held, ArtistSource.Manual());
        ReadReturns(Playlist(
            artists:
            [
                new SpotifyPlaylistArtist { Id = "artist-held", Name = "Held" },
                new SpotifyPlaylistArtist { Id = "artist-new", Name = "New" }
            ],
            trackCount: 2));

        var result = await _service.LinkAsync(_accountId, PlaylistInput);

        Assert.That(result, Is.TypeOf<SpotifyPlaylistLinkResult.Linked>());
        var linked = (SpotifyPlaylistLinkResult.Linked)result;
        Assert.That(linked.PlaylistName, Is.EqualTo("Road trip"));
        Assert.That(linked.NewArtistCount, Is.EqualTo(1));
        Assert.That(linked.Message, Is.EqualTo("Linked Road trip: 1 new artists added"));
        Assert.That(_account.LinkedPlaylists, Has.Count.EqualTo(1));
        Assert.That(_account.LinkedPlaylists[0].SpotifyId, Is.EqualTo(PlaylistId));
        Assert.That(_account.LinkedPlaylists[0].Name, Is.EqualTo("Road trip"));
        Assert.That(_account.ArtistLinks, Has.Count.EqualTo(2));
        Assert.That(
            _account.ArtistLinks.Single(link => link.ArtistId == "artist-held").Sources.Select(source => source.ToSource()),
            Is.EquivalentTo(new[] { ArtistSource.Manual(), ArtistSource.FromLinkedPlaylist(PlaylistId) }));
        Assert.That(
            _account.ArtistLinks.Single(link => link.ArtistId == "artist-new").Sources.Select(source => source.ToSource()),
            Is.EquivalentTo(new[] { ArtistSource.FromLinkedPlaylist(PlaylistId) }));
        await _accountRepository.Received(1).SaveChangesAsync();
        await _unitOfWork.Received(1).ExecuteSerializableTransactionAsync(Arg.Any<Func<Task>>());
        await _accountRepository.Received(2).GetOneRequiredAsync(
            _accountId,
            nameof(Account.Id),
            Arg.Is<string[]>(includes =>
                includes.Contains("ArtistLinks.Sources")
                && includes.Contains(nameof(Account.LinkedPlaylists))));
    }

    [Test]
    public async Task LinkAsync_AddsArtistsThatHaveNoGenres()
    {
        ReadReturns(Playlist(
            artists: [new SpotifyPlaylistArtist { Id = "artist-1", Name = "No Genres" }],
            trackCount: 1));
        _spotifyClient.GetArtistsByIdsAsync("token", Arg.Any<IReadOnlyList<string>>())
            .Returns([new SpotifyArtistDto { Id = "artist-1", Name = "No Genres", Genres = [] }]);

        var result = await _service.LinkAsync(_accountId, PlaylistInput);

        var linked = (SpotifyPlaylistLinkResult.Linked)result;
        Assert.That(linked.NewArtistCount, Is.EqualTo(1));
        Assert.That(_account.ArtistLinks, Has.Count.EqualTo(1));
        await _genreRepository.DidNotReceive().CreateAsync(Arg.Any<Genre>());
    }

    [Test]
    public async Task LinkAsync_LinksEmptyPlaylistWithZeroArtists()
    {
        ReadReturns(Playlist(artists: [], trackCount: 0));

        var result = await _service.LinkAsync(_accountId, PlaylistInput);

        var linked = (SpotifyPlaylistLinkResult.Linked)result;
        Assert.That(linked.NewArtistCount, Is.EqualTo(0));
        Assert.That(linked.Message, Is.EqualTo("Linked Road trip: 0 new artists added"));
        Assert.That(_account.LinkedPlaylists, Has.Count.EqualTo(1));
        Assert.That(_account.ArtistLinks, Is.Empty);
        await _spotifyClient.DidNotReceive()
            .GetArtistsByIdsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());
        await _accountRepository.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task LinkAsync_NotesWhenReadStoppedAtTrackCap()
    {
        ReadReturns(Playlist(
            artists: [new SpotifyPlaylistArtist { Id = "artist-1", Name = "Artist" }],
            trackCount: 1000,
            truncatedAtTrackCap: true));

        var result = await _service.LinkAsync(_accountId, PlaylistInput);

        var linked = (SpotifyPlaylistLinkResult.Linked)result;
        Assert.That(linked.TruncatedAtTrackCap, Is.True);
        Assert.That(linked.Message, Is.EqualTo("Linked Road trip: 1 new artists added. Import stopped at 1,000 tracks."));
    }

    [Test]
    public async Task LinkAsync_RejectsDuplicatePlaylistIdBeforeReadingSpotify()
    {
        _account.LinkedPlaylists.Add(new LinkedPlaylist
        {
            AccountId = _accountId,
            SpotifyId = PlaylistId,
            Name = "Already"
        });

        var result = await _service.LinkAsync(_accountId, PlaylistInput);

        Assert.That(result, Is.TypeOf<SpotifyPlaylistLinkResult.Duplicate>());
        Assert.That(_account.LinkedPlaylists, Has.Count.EqualTo(1));
        Assert.That(_account.ArtistLinks, Is.Empty);
        await _playlistReadService.DidNotReceive().ReadAsync(Arg.Any<string>());
        await _accountRepository.DidNotReceive().SaveChangesAsync();
        await _artistRepository.DidNotReceive().CreateAsync(Arg.Any<Artist>());
        await _unitOfWork.DidNotReceive()
            .ExecuteSerializableTransactionAsync(Arg.Any<Func<Task>>());
    }

    [Test]
    public async Task LinkAsync_RejectsPlaylistIdAtCapBeforeReadingSpotify()
    {
        FillPlaylists(_account, LinkedPlaylist.MaxPerAccount);

        var result = await _service.LinkAsync(_accountId, PlaylistInput);

        Assert.That(result, Is.TypeOf<SpotifyPlaylistLinkResult.CapReached>());
        Assert.That(_account.LinkedPlaylists, Has.Count.EqualTo(LinkedPlaylist.MaxPerAccount));
        await _playlistReadService.DidNotReceive().ReadAsync(Arg.Any<string>());
        await _accountRepository.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task LinkAsync_ReadsShortLinkBeforeRejectingCap()
    {
        FillPlaylists(_account, LinkedPlaylist.MaxPerAccount);
        _playlistReadService.ReadAsync(ShortLinkInput)
            .Returns(new SpotifyPlaylistReadResult.Success(Playlist(artists: [], trackCount: 0)));

        var result = await _service.LinkAsync(_accountId, ShortLinkInput);

        Assert.That(result, Is.TypeOf<SpotifyPlaylistLinkResult.CapReached>());
        await _playlistReadService.Received(1).ReadAsync(ShortLinkInput);
        await _accountRepository.DidNotReceive().SaveChangesAsync();
        await _artistRepository.DidNotReceive().CreateAsync(Arg.Any<Artist>());
    }

    [Test]
    public async Task LinkAsync_RechecksCapOnTheAccountLoadedInsideTheTransaction()
    {
        var earlyAccount = NewAccount(_accountId);
        FillPlaylists(earlyAccount, LinkedPlaylist.MaxPerAccount - 1);
        var lockedAccount = NewAccount(_accountId);
        FillPlaylists(lockedAccount, LinkedPlaylist.MaxPerAccount);
        _accountRepository.GetOneRequiredAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string[]>())
            .Returns(earlyAccount, lockedAccount);
        ReadReturns(Playlist(
            artists: [new SpotifyPlaylistArtist { Id = "artist-1", Name = "Artist" }],
            trackCount: 1));

        var result = await _service.LinkAsync(_accountId, PlaylistInput);

        Assert.That(result, Is.TypeOf<SpotifyPlaylistLinkResult.CapReached>());
        await _playlistReadService.Received(1).ReadAsync(PlaylistInput);
        await _accountRepository.Received(2).GetOneRequiredAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string[]>());
        await _accountRepository.DidNotReceive().SaveChangesAsync();
        await _artistRepository.DidNotReceive().CreateAsync(Arg.Any<Artist>());
    }

    [Test]
    public async Task LinkAsync_RejectsASecondInsertOnceTheSameAccountIsAtCap()
    {
        FillPlaylists(_account, LinkedPlaylist.MaxPerAccount - 1);
        ReadReturns(Playlist(artists: [], trackCount: 0));
        _playlistReadService.ReadAsync(OtherPlaylistInput)
            .Returns(new SpotifyPlaylistReadResult.Success(Playlist(
                spotifyId: OtherPlaylistId,
                artists: [],
                trackCount: 0)));

        var first = await _service.LinkAsync(_accountId, PlaylistInput);
        var second = await _service.LinkAsync(_accountId, OtherPlaylistInput);

        Assert.That(first, Is.TypeOf<SpotifyPlaylistLinkResult.Linked>());
        Assert.That(second, Is.TypeOf<SpotifyPlaylistLinkResult.CapReached>());
        Assert.That(_account.LinkedPlaylists, Has.Count.EqualTo(LinkedPlaylist.MaxPerAccount));
        await _playlistReadService.DidNotReceive().ReadAsync(OtherPlaylistInput);
        await _accountRepository.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task LinkAsync_RollsBackTheTransactionWhenAccountSaveFailsAfterCreatingAnArtist()
    {
        var phases = new List<string>();
        var rolledBack = false;
        ReadReturns(Playlist(
            artists: [new SpotifyPlaylistArtist { Id = "artist-1", Name = "Artist" }],
            trackCount: 1));
        _spotifyClient.GetArtistsByIdsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(_ =>
            {
                phases.Add("spotify");
                return new List<SpotifyArtistDto>();
            });
        _unitOfWork.ExecuteSerializableTransactionAsync(Arg.Any<Func<Task>>())
            .Returns(async call =>
            {
                phases.Add("db");
                try
                {
                    await call.Arg<Func<Task>>()();
                }
                catch
                {
                    rolledBack = true;
                    throw;
                }
            });
        _accountRepository.SaveChangesAsync()
            .ThrowsAsync(new InvalidOperationException("account save failed"));

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.LinkAsync(_accountId, PlaylistInput));

        Assert.That(rolledBack, Is.True);
        Assert.That(phases, Is.EqualTo(new[] { "spotify", "db" }));
        await _artistRepository.Received(1).CreateAsync(Arg.Any<Artist>());
        await _unitOfWork.Received(1)
            .ExecuteSerializableTransactionAsync(Arg.Any<Func<Task>>());
    }

    [Test]
    public async Task LinkAsync_HoldsEachUniqueArtistOnce()
    {
        ReadReturns(Playlist(
            artists:
            [
                new SpotifyPlaylistArtist { Id = "artist-1", Name = "Artist" },
                new SpotifyPlaylistArtist { Id = "artist-1", Name = "Artist" }
            ],
            trackCount: 2));

        var result = await _service.LinkAsync(_accountId, PlaylistInput);

        var linked = (SpotifyPlaylistLinkResult.Linked)result;
        Assert.That(linked.NewArtistCount, Is.EqualTo(1));
        Assert.That(_account.ArtistLinks, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task UnlinkAsync_DropsArtistsHeldOnlyByThatPlaylist()
    {
        var exclusive = new Artist { Id = "artist-exclusive", Name = "Exclusive" };
        var shared = new Artist { Id = "artist-shared", Name = "Shared" };
        _account.HoldArtist(exclusive, ArtistSource.FromLinkedPlaylist(PlaylistId));
        _account.HoldArtist(shared, ArtistSource.FromLinkedPlaylist(PlaylistId));
        _account.HoldArtist(shared, ArtistSource.Manual());
        _account.LinkedPlaylists.Add(new LinkedPlaylist
        {
            AccountId = _account.Id,
            SpotifyId = PlaylistId,
            Name = "Road trip"
        });

        var unlinked = await _service.UnlinkAsync(_accountId, PlaylistId);

        Assert.That(unlinked, Is.True);
        Assert.That(_account.LinkedPlaylists, Is.Empty);
        Assert.That(_account.ArtistLinks, Has.Count.EqualTo(1));
        Assert.That(_account.ArtistLinks[0].ArtistId, Is.EqualTo("artist-shared"));
        Assert.That(_account.ArtistLinks[0].Sources.Select(source => source.ToSource()),
            Is.EquivalentTo(new[] { ArtistSource.Manual() }));
        await _accountRepository.Received(1).SaveChangesAsync();
        await _spotifyClient.DidNotReceive()
            .GetArtistsByIdsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());
    }

    [Test]
    public async Task UnlinkAsync_ReturnsFalseWithoutWritingWhenPlaylistIsUnknown()
    {
        var unlinked = await _service.UnlinkAsync(_accountId, PlaylistId);

        Assert.That(unlinked, Is.False);
        await _accountRepository.DidNotReceive().SaveChangesAsync();
        await _spotifyClient.DidNotReceive()
            .GetArtistsByIdsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());
    }

    [Test]
    public async Task LinkAsync_ReturnsNotReadableWithoutWriting()
    {
        _playlistReadService.ReadAsync(PlaylistInput).Returns(new SpotifyPlaylistReadResult.NotReadable());

        var result = await _service.LinkAsync(_accountId, PlaylistInput);

        Assert.That(result, Is.TypeOf<SpotifyPlaylistLinkResult.NotReadable>());
        await _accountRepository.DidNotReceive().SaveChangesAsync();
        await _unitOfWork.DidNotReceive()
            .ExecuteSerializableTransactionAsync(Arg.Any<Func<Task>>());
    }

    [Test]
    public async Task LinkAsync_ReturnsInvalidInputWithoutWriting()
    {
        _playlistReadService.ReadAsync(PlaylistInput).Returns(new SpotifyPlaylistReadResult.InvalidInput());

        var result = await _service.LinkAsync(_accountId, PlaylistInput);

        Assert.That(result, Is.TypeOf<SpotifyPlaylistLinkResult.InvalidInput>());
        await _accountRepository.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task LinkAsync_UsesGenresFromArtistLookupWhenPresent()
    {
        ReadReturns(Playlist(
            artists: [new SpotifyPlaylistArtist { Id = "artist-1", Name = "With Genres" }],
            trackCount: 1));
        _spotifyClient.GetArtistsByIdsAsync("token", Arg.Is<IReadOnlyList<string>>(ids => ids.Single() == "artist-1"))
            .Returns([
                new SpotifyArtistDto
                {
                    Id = "artist-1",
                    Name = "With Genres",
                    Genres = ["rock"],
                    Popularity = 40
                }
            ]);

        await _service.LinkAsync(_accountId, PlaylistInput);

        var artist = _account.ArtistLinks.Single().Artist;
        Assert.That(artist.Genres.Select(genre => genre.Name), Is.EquivalentTo(new[] { "Rock" }));
        await _genreRepository.Received(1).CreateAsync(Arg.Is<Genre>(genre => genre.Name == "Rock"));
    }

    private void ReadReturns(SpotifyPlaylistRead playlist)
    {
        _playlistReadService.ReadAsync(PlaylistInput)
            .Returns(new SpotifyPlaylistReadResult.Success(playlist));
    }

    private static SpotifyPlaylistRead Playlist(
        IReadOnlyList<SpotifyPlaylistArtist> artists,
        int trackCount,
        bool truncatedAtTrackCap = false,
        string spotifyId = PlaylistId)
    {
        return new SpotifyPlaylistRead
        {
            SpotifyId = spotifyId,
            Name = "Road trip",
            Artists = artists,
            TrackCount = trackCount,
            TruncatedAtTrackCap = truncatedAtTrackCap
        };
    }

    private static void FillPlaylists(Account account, int count)
    {
        for (var i = 0; i < count; i++)
        {
            account.LinkedPlaylists.Add(new LinkedPlaylist
            {
                AccountId = account.Id,
                SpotifyId = $"playlist-{i}",
                Name = $"Playlist {i}"
            });
        }
    }

    private void StubArtistCatalog()
    {
        _artistRepository.GetAsync(
                Arg.Any<Expression<Func<Artist, bool>>>(),
                Arg.Any<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                Arg.Any<string[]>())
            .Returns(call =>
            {
                var predicate = call.Arg<Expression<Func<Artist, bool>>>().Compile();
                return _artistCatalog.Where(predicate).ToList();
            });
        _artistRepository.CreateAsync(Arg.Any<Artist>()).Returns(call =>
        {
            var artist = call.Arg<Artist>();
            _artistCatalog.Add(artist);
            return artist;
        });
    }

    private static Account NewAccount(Guid id) => new()
    {
        Id = id,
        Email = "test@mail",
        Name = "test",
        PasswordHash = "pass"
    };
}
