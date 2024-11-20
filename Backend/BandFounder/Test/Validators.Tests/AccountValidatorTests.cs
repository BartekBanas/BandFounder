using System.Linq.Expressions;
using BandFounder.Application.Error;
using BandFounder.Application.Validation;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using BandFounder.Infrastructure.Errors;
using NSubstitute;

namespace Validators.Tests;

[TestFixture]
public class AccountValidatorTests
{
    private IRepository<Account> _repositoryMock;
    private AccountValidator _validator;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = Substitute.For<IRepository<Account>>();
        _validator = new AccountValidator(_repositoryMock);
    }

    [Test]
    public async Task Validate_ValidAccount_ShouldPass()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = "ValidUsername",
            Email = "valid.email@example.com",
            PasswordHash = "hashedpassword123"
        };

        _repositoryMock.GetAsync(Arg.Any<Expression<Func<Account, bool>>>(),
            Arg.Any<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
            Arg.Any<string[]>()).Returns([]);

        // Act
        var result = await _validator.ValidateAsync(account);

        // Assert
        Assert.IsTrue(result.IsValid, "Validation should pass for valid account.");
    }

    [Test]
    public async Task Validate_InvalidEmail_ShouldFail()
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = "ValidUsername",
            Email = "invalid-email",
            PasswordHash = "hashedpassword123"
        };

        // Act
        var result = await _validator.ValidateAsync(account);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("'Email' is not a valid email address.", result.Errors[0].ErrorMessage);
    }

    [TestCase("Valid-Name", true)]
    [TestCase("Valid_Name", true)]
    [TestCase("invalid name", false)]
    [TestCase("Invalid@Name", false)]
    [TestCase("-InvalidName", false)]
    [TestCase("InvalidName-", false)]
    [TestCase("_InvalidName", false)]
    [TestCase("InvalidName_", false)]
    public async Task Validate_UsernameFormat_ShouldMatchExpectedResult(string username, bool isValid)
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = username,
            Email = "valid.email@example.com",
            PasswordHash = "hashedpassword123"
        };

        // Act & Assert
        if (isValid)
        {
            var result = await _validator.ValidateAsync(account);
            Assert.IsTrue(result.IsValid);
        }
        else
        {
            var ex = Assert.ThrowsAsync<CustomValidationException>(async () => await _validator.ValidateAsync(account));
            Assert.IsTrue(ex.Message.Contains("Username"));
        }
    }

    [TestCase("")]
    [TestCase("  ")]
    public async Task Validate_EmptyOrWhitespaceName_ShouldFail(string? name)
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = "valid.email@example.com",
            PasswordHash = "hashedpassword123"
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<CustomValidationException>(async () => await _validator.ValidateAsync(account));
        Assert.IsTrue(ex.Message.Contains("Username can only contain letters, numbers, hyphens, and underscores"));
    }

    [Test]
    public async Task Validate_DuplicateUsername_ShouldFail()
    {
        // Arrange
        var duplicateAccount = new Account
        {
            Name = "DuplicateUsername",
            Id = default,
            PasswordHash = null,
            Email = null
        };
        
        _repositoryMock.GetAsync(Arg.Any<Expression<Func<Account, bool>>>(),
            Arg.Any<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
            Arg.Any<string[]>()).Returns([duplicateAccount]);

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = "DuplicateUsername",
            Email = "unique.email@example.com",
            PasswordHash = "hashedpassword123"
        };

        // Act & Assert
        Assert.ThrowsAsync<ItemDuplicatedErrorException>(async () => await _validator.ValidateAsync(account));
    }

    [Test]
    public async Task Validate_DuplicateEmail_ShouldFail()
    {
        // Arrange
        var duplicateAccount = new Account
        {
            Email = "duplicate.email@example.com",
            Id = default,
            Name = "Test",
            PasswordHash = null
        };
        
        _repositoryMock.GetAsync(Arg.Any<Expression<Func<Account, bool>>>(),
            Arg.Any<Func<IQueryable<Account>, IOrderedQueryable<Account>>>(),
            Arg.Any<string[]>()).Returns([duplicateAccount]);

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = "UniqueUsername",
            Email = "duplicate.email@example.com",
            PasswordHash = "hashedpassword123"
        };

        // Act & Assert
        Assert.ThrowsAsync<ItemDuplicatedErrorException>(async () => await _validator.ValidateAsync(account));
    }

    [TestCase("ab")]
    [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task Validate_UsernameLength_ShouldFail(string username)
    {
        // Arrange
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = username,
            Email = "valid.email@example.com",
            PasswordHash = "hashedpassword123"
        };

        // Act
        var result = await _validator.ValidateAsync(account);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Exists(e => e.ErrorMessage.Contains("'Name' must be between 3 and 32 characters")));
    }
}