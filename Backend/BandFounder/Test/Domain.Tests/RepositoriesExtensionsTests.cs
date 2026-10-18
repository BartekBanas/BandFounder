using System.Linq.Expressions;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using NSubstitute;

namespace Domain.Tests;

public class RepositoriesExtensionsTests
{
    private IRepository<Genre> _genreRepository;
    private IRepository<MusicianRole> _musicianRoleRepository;
    private IRepository<Artist> _artistRepository;

    [SetUp]
    public void SetUp()
    {
        _genreRepository = Substitute.For<IRepository<Genre>>();
        _musicianRoleRepository = Substitute.For<IRepository<MusicianRole>>();
        _artistRepository = Substitute.For<IRepository<Artist>>();
        _genreRepository.GetAsync(
                Arg.Any<Expression<Func<Genre, bool>>>(),
                Arg.Any<Func<IQueryable<Genre>, IOrderedQueryable<Genre>>>(),
                Arg.Any<string[]>())
            .Returns(Array.Empty<Genre>());
        _genreRepository.CreateAsync(Arg.Any<Genre>()).Returns(call => call.Arg<Genre>());
    }
    
    [Test]
    public void NormalizeGenreName_ShouldCapitalizeEachWordCorrectly()
    {
        // Arrange
        var input = "progressive metalcore";
        var expectedOutput = "Progressive Metalcore";

        // Act
        var result = RepositoriesExtensions.NormalizeName(input);

        // Assert
        Assert.That(result, Is.EqualTo(expectedOutput));
    }

    [Test]
    public void NormalizeGenreName_ShouldTrimExtraSpaces()
    {
        // Arrange
        var input = "  progressive    metalcore ";
        var expectedOutput = "Progressive Metalcore";

        // Act
        var result = RepositoriesExtensions.NormalizeName(input);

        // Assert
        Assert.That(result, Is.EqualTo(expectedOutput));
    }

    [Test]
    public void NormalizeGenreName_ShouldHandleSingleWordCorrectly()
    {
        // Arrange
        var input = "rock";
        var expectedOutput = "Rock";

        // Act
        var result = RepositoriesExtensions.NormalizeName(input);

        // Assert
        Assert.That(result, Is.EqualTo(expectedOutput));
    }
    
    [TestCase("k-pop girl group", "K-Pop Girl Group")]
    [TestCase("nu-metalcore", "Nu-Metalcore")]
    [TestCase("pov: indie", "Pov: Indie")]
    [TestCase("australian post-hardcore", "Australian Post-Hardcore")]
    [TestCase("R&B", "R&B")]
    [TestCase("EDM", "Edm")]
    [TestCase("K-Pop", "K-Pop")]
    [TestCase("Hip-Hop", "Hip-Hop")]
    public void NormalizeGenreName_ShouldHandleComplexCasesCorrectly(string input, string expectedOutput)
    {
        // Act
        var result = input.NormalizeName();

        // Assert
        Assert.That(result, Is.EqualTo(expectedOutput));
    }

    [Test]
    public async Task TryAddGenreAsync_ShouldThrowArgumentException_WhenGenreNameIsEmpty()
    {
        // Arrange
        var emptyGenreName = "  ";

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(
            () => _genreRepository.GetOrCreateAsync(emptyGenreName));
        Assert.That(ex.Message, Is.EqualTo("Genre name cannot be empty or whitespace."));
    }

    [Test]
    public async Task TryAddGenreAsync_ShouldNotCallCreateAsync_WhenGenreAlreadyExists()
    {
        // Arrange
        const string genreName = "Progressive Metalcore";
        const string normalizedGenreName = "Progressive Metalcore";

        _genreRepository.GetOneAsync(normalizedGenreName).Returns(new Genre { Name = normalizedGenreName });

        // Act
        await _genreRepository.GetOrCreateAsync(genreName);

        // Assert
        await _genreRepository.DidNotReceive().CreateAsync(Arg.Any<Genre>());
    }

    [Test]
    public async Task TryAddGenreAsync_ShouldCallCreateAsync_WhenGenreDoesNotExist()
    {
        // Arrange
        const string genreName = "Progressive Metalcore";
        const string normalizedGenreName = "Progressive Metalcore";

        _genreRepository.GetOneAsync(normalizedGenreName).Returns((Genre)null);

        // Act
        await _genreRepository.GetOrCreateAsync(genreName);

        // Assert
        await _genreRepository.Received(1).CreateAsync(Arg.Is<Genre>(genre => genre.Name == normalizedGenreName));
    }

    [Test]
    public async Task TryAddGenreAsync_ShouldNormalizeGenreName()
    {
        // Arrange
        const string genreName = "  progressive    metalcore ";
        const string normalizedGenreName = "Progressive Metalcore";

        _genreRepository.GetOneAsync(normalizedGenreName).Returns((Genre)null);

        // Act
        await _genreRepository.GetOrCreateAsync(genreName);

        // Assert
        await _genreRepository.Received(1).CreateAsync(Arg.Is<Genre>(genre => genre.Name == normalizedGenreName));
    }
    
    [Test]
    public void GetOrCreateAsync_ShouldThrowArgumentException_WhenRoleNameIsEmpty()
    {
        // Arrange
        var emptyRoleName = "  ";

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(
            () => _musicianRoleRepository.GetOrCreateAsync(emptyRoleName));
        Assert.That(ex.Message, Is.EqualTo("Role name cannot be empty or whitespace"));
    }

    [Test]
    public async Task GetOrCreateAsync_ShouldReturnExistingRole_WhenRoleAlreadyExists()
    {
        // Arrange
        var roleName = "Guitarist";
        var normalizedRoleName = "Guitarist";
        var existingRole = new MusicianRole { Name = normalizedRoleName };

        // Mock the repository to return an existing role
        _musicianRoleRepository
            .GetOneAsync(Arg.Any<Expression<Func<MusicianRole, bool>>>())
            .Returns(existingRole);

        // Act
        var result = await _musicianRoleRepository.GetOrCreateAsync(roleName);

        // Assert
        Assert.That(result, Is.EqualTo(existingRole));
        await _musicianRoleRepository.DidNotReceive().CreateAsync(Arg.Any<MusicianRole>());
    }

    [Test]
    public async Task GetOrCreateAsync_ShouldCreateNewRole_WhenRoleDoesNotExist()
    {
        // Arrange
        var roleName = "Drummer";
        var normalizedRoleName = "Drummer";
        MusicianRole? nullRole = null;

        // Mock the repository to return null (no existing role found)
        _musicianRoleRepository
            .GetOneAsync(Arg.Any<Expression<Func<MusicianRole, bool>>>())
            .Returns(nullRole);

        // Act
        var result = await _musicianRoleRepository.GetOrCreateAsync(roleName);

        // Assert
        Assert.That(result.Name, Is.EqualTo(normalizedRoleName));
        await _musicianRoleRepository.Received(1).CreateAsync(Arg.Is<MusicianRole>(r => r.Name == normalizedRoleName));
    }

    [Test]
    public async Task GetOrCreateAsync_ShouldNormalizeRoleName()
    {
        // Arrange
        var roleName = "  lead   guitarist ";
        var normalizedRoleName = "Lead Guitarist";
        MusicianRole? nullRole = null;

        // Mock the repository to return null (no existing role found)
        _musicianRoleRepository
            .GetOneAsync(Arg.Any<Expression<Func<MusicianRole, bool>>>())
            .Returns(nullRole);

        // Act
        var result = await _musicianRoleRepository.GetOrCreateAsync(roleName);

        // Assert
        Assert.That(result.Name, Is.EqualTo(normalizedRoleName));
        await _musicianRoleRepository.Received(1).CreateAsync(Arg.Is<MusicianRole>(r => r.Name == normalizedRoleName));
    }

    [Test]
    public async Task GetOrCreateManyAsync_ShouldCreateOnlyMissingGenres_InOneQuery()
    {
        var existing = new Genre { Name = "Rock" };
        StubGenres(existing);

        var result = await _genreRepository.GetOrCreateManyAsync(["rock", "ROCK", "indie pop"]);

        Assert.That(result.Keys, Is.EquivalentTo(new[] { "Rock", "Indie Pop" }));
        Assert.That(result["Rock"], Is.SameAs(existing));
        await _genreRepository.Received(1).GetAsync(
            Arg.Any<Expression<Func<Genre, bool>>>(),
            Arg.Any<Func<IQueryable<Genre>, IOrderedQueryable<Genre>>>(),
            Arg.Any<string[]>());
        await _genreRepository.Received(1).CreateAsync(Arg.Is<Genre>(genre => genre.Name == "Indie Pop"));
    }

    [Test]
    public void GetOrCreateManyAsync_ShouldThrow_WhenAnyGenreNameIsBlank()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _genreRepository.GetOrCreateManyAsync(["rock", " "]));
    }

    [Test]
    public async Task ResolveManyAsync_ShouldReturnExistingArtistsInInputOrder_WithOneQuery()
    {
        var first = new Artist { Id = "id-1", Name = "First" };
        var second = new Artist { Id = "guid-2", Name = "Second" };
        StubArtists(first, second);

        var result = await _artistRepository.ResolveManyAsync(
        [
            new ArtistData("spotify:artist:id-1", "Whatever"),
            new ArtistData(null, "Second"),
            new ArtistData("id-3", "Third")
        ]);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.SameAs(first));
        Assert.That(result[1], Is.SameAs(second));
        Assert.That(result[2].Id, Is.EqualTo("id-3"));
        Assert.That(result[2].Name, Is.EqualTo("Third"));
        await _artistRepository.Received(1).GetAsync(
            Arg.Any<Expression<Func<Artist, bool>>>(),
            Arg.Any<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
            Arg.Any<string[]>());
        await _artistRepository.Received(1).CreateAsync(Arg.Any<Artist>());
    }

    [Test]
    public async Task ResolveManyAsync_ShouldCreateOneArtist_WhenSameArtistAppearsTwice()
    {
        StubArtists();

        var result = await _artistRepository.ResolveManyAsync(
            [new ArtistData("id-1", "Artist"), new ArtistData("id-1", "Artist")]);

        Assert.That(result[0], Is.SameAs(result[1]));
        await _artistRepository.Received(1).CreateAsync(Arg.Any<Artist>());
    }

    [Test]
    public void ResolveManyAsync_ShouldThrow_WhenAnyNameIsBlank()
    {
        Assert.ThrowsAsync<ArgumentException>(
            () => _artistRepository.ResolveManyAsync([new ArtistData("id-1", "Artist"), new ArtistData("id-2", " ")]));
    }

    [Test]
    public async Task GetOrCreateAsync_ShouldCreateEachNameOnce_AndSaveOnce()
    {
        StubArtists();

        var result = (await _artistRepository.GetOrCreateAsync(["A", "B", "A"])).ToList();

        Assert.That(result.Select(artist => artist.Name), Is.EqualTo(new[] { "A", "B", "A" }));
        await _artistRepository.Received(2).CreateAsync(Arg.Any<Artist>());
        await _artistRepository.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task GetOrCreateManyAsync_ShouldReturnExistingArtist_WhenSpotifyIdExistsWithDifferentName()
    {
        var existingArtist = new Artist { Id = "spotify-artist-id", Name = "Old Artist Name", Genres = [] };
        StubArtists(existingArtist);

        var result = await _artistRepository.GetOrCreateManyAsync(
            _genreRepository,
            [new ArtistData("spotify-artist-id", "Updated Artist Name") { Genres = ["rock"], Popularity = 80 }]);

        Assert.That(result.Single(), Is.SameAs(existingArtist));
        Assert.That(existingArtist.Name, Is.EqualTo("Updated Artist Name"));
        Assert.That(existingArtist.Popularity, Is.EqualTo(80));
        Assert.That(existingArtist.Genres.Select(genre => genre.Name), Is.EqualTo(new[] { "Rock" }));
        await _artistRepository.DidNotReceive().CreateAsync(Arg.Any<Artist>());
    }

    [Test]
    public async Task GetOrCreateManyAsync_ShouldNotChangeGenresOrPopularity_WhenArtistAlreadyHasThem()
    {
        var existingArtist = new Artist
        {
            Id = "id-1",
            Name = "Artist",
            Popularity = 50,
            Genres = [new Genre { Name = "Jazz" }]
        };
        StubArtists(existingArtist);

        await _artistRepository.GetOrCreateManyAsync(
            _genreRepository,
            [new ArtistData("id-1", "Artist") { Genres = ["rock"], Popularity = 80 }]);

        Assert.That(existingArtist.Popularity, Is.EqualTo(50));
        Assert.That(existingArtist.Genres.Select(genre => genre.Name), Is.EqualTo(new[] { "Jazz" }));
        await _genreRepository.DidNotReceive().CreateAsync(Arg.Any<Genre>());
    }

    [Test]
    public async Task GetOrCreateManyAsync_ShouldShareGenresBetweenArtists_AndTakeGenresFromFirstInputOnly()
    {
        StubArtists();

        var result = await _artistRepository.GetOrCreateManyAsync(
            _genreRepository,
            [
                new ArtistData("id-1", "A") { Genres = ["rock", "pop"] },
                new ArtistData("id-2", "B") { Genres = ["Rock"] },
                new ArtistData("id-1", "A") { Genres = ["metal"] }
            ]);

        Assert.That(result[0].Genres.Select(genre => genre.Name), Is.EquivalentTo(new[] { "Rock", "Pop" }));
        Assert.That(result[1].Genres.Single(), Is.SameAs(result[0].Genres.Single(genre => genre.Name == "Rock")));
        await _genreRepository.Received(2).CreateAsync(Arg.Any<Genre>());
    }

    [Test]
    public async Task GetOrCreateManyAsync_ShouldReturnEmpty_WhenNothingIsGiven()
    {
        var result = await _artistRepository.GetOrCreateManyAsync(_genreRepository, []);

        Assert.That(result, Is.Empty);
        await _artistRepository.DidNotReceiveWithAnyArgs().GetAsync();
    }

    private void StubGenres(params Genre[] catalog) =>
        _genreRepository.GetAsync(
                Arg.Any<Expression<Func<Genre, bool>>>(),
                Arg.Any<Func<IQueryable<Genre>, IOrderedQueryable<Genre>>>(),
                Arg.Any<string[]>())
            .Returns(call => catalog.Where(call.Arg<Expression<Func<Genre, bool>>>().Compile()).ToList());

    private void StubArtists(params Artist[] catalog) =>
        _artistRepository.GetAsync(
                Arg.Any<Expression<Func<Artist, bool>>>(),
                Arg.Any<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                Arg.Any<string[]>())
            .Returns(call => catalog.Where(call.Arg<Expression<Func<Artist, bool>>>().Compile()).ToList());
}
