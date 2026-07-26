using BandFounder.Application.Services;

namespace Services.Tests;

[TestFixture]
public class PasswordResetTokenHelperTests
{
    [Test]
    public void GenerateRawToken_ReturnsUrlSafeOpaqueValue()
    {
        var token = PasswordResetTokenHelper.GenerateRawToken();

        Assert.That(token, Is.Not.Null.And.Not.Empty);
        Assert.That(token, Does.Not.Contain("+"));
        Assert.That(token, Does.Not.Contain("/"));
        Assert.That(token, Does.Not.Contain("="));
    }

    [Test]
    public void HashToken_IsDeterministicAndDifferentFromRaw()
    {
        var token = PasswordResetTokenHelper.GenerateRawToken();
        var hash1 = PasswordResetTokenHelper.HashToken(token);
        var hash2 = PasswordResetTokenHelper.HashToken(token);

        Assert.That(hash1, Is.EqualTo(hash2));
        Assert.That(hash1, Is.Not.EqualTo(token));
        Assert.That(hash1.Length, Is.EqualTo(64));
    }
}
