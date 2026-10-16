using BandFounder.Application.Services;
using BandFounder.Domain.Entities;

namespace Services.Tests;

[TestFixture]
public class HashingServiceTests
{
    private readonly HashingService _hashingService = new();

    [Test]
    public void HashPassword_SameInput_ProducesDifferentHashes()
    {
        var first = _hashingService.HashPassword("Password123!");
        var second = _hashingService.HashPassword("Password123!");

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var account = CreateAccount("Password123!");

        Assert.That(_hashingService.VerifyPassword(account, "Password123!"), Is.True);
    }

    [Test]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        var account = CreateAccount("Password123!");

        Assert.That(_hashingService.VerifyPassword(account, "WrongPassword!"), Is.False);
    }

    private Account CreateAccount(string password)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            Name = "tester",
            Email = "tester@example.com",
            PasswordHash = _hashingService.HashPassword(password)
        };
    }
}
