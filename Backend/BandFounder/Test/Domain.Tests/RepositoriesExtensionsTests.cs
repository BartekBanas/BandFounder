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
    public async Task GetOrCreateArtistAsync_ShouldReturnExistingArtist_WhenSpotifyIdExistsWithDifferentName()
    {
        const string spotifyId = "spotify-artist-id";
        const string storedName = "Old Artist Name";
        const string spotifyName = "Updated Artist Name";

        var existingArtist = new Artist
        {
            Id = spotifyId,
            Name = storedName,
            Genres = []
        };

        StubArtistLookup(existingArtist);
        StubGenreGetOrCreate("Rock");

        var result = await _artistRepository.GetOrCreateAsync(
            _genreRepository, spotifyName, ["rock"], 80, spotifyId);

        Assert.That(result, Is.EqualTo(existingArtist));
        Assert.That(result.Name, Is.EqualTo(spotifyName));
        Assert.That(result.Popularity, Is.EqualTo(80));
        Assert.That(result.Genres.Select(g => g.Name), Is.EquivalentTo(new[] { "Rock" }));
        await _artistRepository.DidNotReceive().CreateAsync(Arg.Any<Artist>());
    }

    [Test]
    public async Task GetOrCreateArtistAsync_ShouldReplaceGenres_WhenExistingArtistAlreadyHasGenres()
    {
        const string spotifyId = "spotify-artist-id";
        var existingArtist = new Artist
        {
            Id = spotifyId,
            Name = "Artist",
            Popularity = 50,
            Genres = [new Genre { Name = "Rock" }, new Genre { Name = "Metal" }]
        };

        StubArtistLookup(existingArtist);
        StubGenreGetOrCreate("Pop");
        StubGenreGetOrCreate("Jazz");

        var result = await _artistRepository.GetOrCreateAsync(
            _genreRepository, "Artist", ["pop", "jazz"], 90, spotifyId);

        Assert.That(result.Genres.Select(g => g.Name), Is.EquivalentTo(new[] { "Pop", "Jazz" }));
        Assert.That(result.Popularity, Is.EqualTo(90));
    }

    [Test]
    public async Task GetOrCreateArtistAsync_ShouldClearGenres_WhenSpotifyReturnsEmptyList()
    {
        const string spotifyId = "spotify-artist-id";
        var existingArtist = new Artist
        {
            Id = spotifyId,
            Name = "Artist",
            Genres = [new Genre { Name = "Rock" }]
        };

        StubArtistLookup(existingArtist);

        var result = await _artistRepository.GetOrCreateAsync(
            _genreRepository, "Artist", [], 0, spotifyId);

        Assert.That(result.Genres, Is.Empty);
        Assert.That(result.Popularity, Is.EqualTo(0));
    }

    [Test]
    public async Task GetOrCreateArtistAsync_ShouldLeaveGenresAlone_WhenGenresArgumentIsNull()
    {
        const string spotifyId = "spotify-artist-id";
        var existingArtist = new Artist
        {
            Id = spotifyId,
            Name = "Artist",
            Genres = [new Genre { Name = "Rock" }]
        };

        StubArtistLookup(existingArtist);

        var result = await _artistRepository.GetOrCreateAsync(
            _genreRepository, "Artist", genres: null, popularity: 0, id: spotifyId);

        Assert.That(result.Genres.Select(g => g.Name), Is.EquivalentTo(new[] { "Rock" }));
    }

    [Test]
    public async Task GetOrCreateArtistAsync_ShouldNormalizeSpotifyUriId()
    {
        const string spotifyId = "spotify-artist-id";
        const string spotifyUri = "spotify:artist:spotify-artist-id";

        var existingArtist = new Artist
        {
            Id = spotifyId,
            Name = "Artist Name",
            Genres = []
        };

        StubArtistLookup(existingArtist);

        var result = await _artistRepository.GetOrCreateAsync(
            _genreRepository, "Artist Name", [], 0, spotifyUri);

        Assert.That(result, Is.EqualTo(existingArtist));
        await _artistRepository.DidNotReceive().CreateAsync(Arg.Any<Artist>());
    }

    private void StubArtistLookup(Artist existingArtist)
    {
        _artistRepository
            .GetOneAsync(Arg.Any<Expression<Func<Artist, bool>>>(), Arg.Any<string[]>())
            .Returns(callInfo =>
            {
                var filter = callInfo.Arg<Expression<Func<Artist, bool>>>().Compile();
                return filter(existingArtist) ? existingArtist : null;
            });
    }

    private void StubGenreGetOrCreate(string normalizedName)
    {
        _genreRepository.GetOneAsync(normalizedName).Returns(new Genre { Name = normalizedName });
    }
}