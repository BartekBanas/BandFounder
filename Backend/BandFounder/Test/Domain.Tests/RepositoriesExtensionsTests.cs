using System.Linq.Expressions;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
using NSubstitute;

namespace Domain.Tests;

public class RepositoriesExtensionsTests
{
    private IRepository<Genre> _genreRepository;
    private IRepository<MusicianRole> _musicianRoleRepository;

    [SetUp]
    public void SetUp()
    {
        _genreRepository = Substitute.For<IRepository<Genre>>();
        _musicianRoleRepository = Substitute.For<IRepository<MusicianRole>>();
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
    
    [TestCase("k-pop girl group", "K-pop Girl Group")]
    [TestCase("nu-metalcore", "Nu-metalcore")]
    [TestCase("pov: indie", "Pov: Indie")]
    [TestCase("australian post-hardcore", "Australian Post-hardcore")]
    public void NormalizeGenreName_ShouldHandleComplexCasesCorrectly(string input, string expectedOutput)
    {
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
        Assert.That(ex.Message, Is.EqualTo("Role name cannot be empty or whitespace."));
    }

    [Test]
    public async Task GetOrCreateAsync_ShouldReturnExistingRole_WhenRoleAlreadyExists()
    {
        // Arrange
        var roleName = "Guitarist";
        var normalizedRoleName = "Guitarist";
        var existingRole = new MusicianRole { RoleName = normalizedRoleName };

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
        Assert.That(result.RoleName, Is.EqualTo(normalizedRoleName));
        await _musicianRoleRepository.Received(1).CreateAsync(Arg.Is<MusicianRole>(r => r.RoleName == normalizedRoleName));
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
        Assert.That(result.RoleName, Is.EqualTo(normalizedRoleName));
        await _musicianRoleRepository.Received(1).CreateAsync(Arg.Is<MusicianRole>(r => r.RoleName == normalizedRoleName));
    }
}