using BandFounder.Domain;
using BandFounder.Domain.Entities.Spotify;
using NSubstitute;

namespace Domain.Tests;

public class RepositoriesExtensionsTests
{
    private IRepository<Genre> _genreRepository;

    [SetUp]
    public void SetUp()
    {
        // Set up a mock repository for Genre
        _genreRepository = Substitute.For<IRepository<Genre>>();
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

    [Test]
    public async Task TryAddGenreAsync_ShouldThrowArgumentException_WhenGenreNameIsEmpty()
    {
        // Arrange
        var emptyGenreName = "  ";

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(
            () => _genreRepository.TryAdd(emptyGenreName));
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
        await _genreRepository.TryAdd(genreName);

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
        await _genreRepository.TryAdd(genreName);

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
        await _genreRepository.TryAdd(genreName);

        // Assert
        await _genreRepository.Received(1).CreateAsync(Arg.Is<Genre>(genre => genre.Name == normalizedGenreName));
    }
}