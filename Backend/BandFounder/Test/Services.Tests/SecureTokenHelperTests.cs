using BandFounder.Application.Services;

namespace Services.Tests;

[TestFixture]
public class SecureTokenHelperTests
{
    [Test]
    public void GenerateRawToken_ReturnsUrlSafeOpaqueValue()
    {
        var token = SecureTokenHelper.GenerateRawToken();

        Assert.That(token, Is.Not.Null.And.Not.Empty);
        Assert.That(token, Does.Not.Contain("+"));
        Assert.That(token, Does.Not.Contain("/"));
        Assert.That(token, Does.Not.Contain("="));
    }

    [Test]
    public void HashToken_IsDeterministicAndDifferentFromRaw()
    {
        var token = SecureTokenHelper.GenerateRawToken();
        var hash1 = SecureTokenHelper.HashToken(token);
        var hash2 = SecureTokenHelper.HashToken(token);

        Assert.That(hash1, Is.EqualTo(hash2));
        Assert.That(hash1, Is.Not.EqualTo(token));
        Assert.That(hash1.Length, Is.EqualTo(64));
    }

    [Test]
    public void DeriveRawToken_IsStableAndUrlSafe()
    {
        var tokenId = Guid.NewGuid();

        var first = SecureTokenHelper.DeriveRawToken(tokenId, "test-signing-key");
        var second = SecureTokenHelper.DeriveRawToken(tokenId, "test-signing-key");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Does.Not.Contain("+"));
            Assert.That(first, Does.Not.Contain("/"));
            Assert.That(first, Does.Not.EndWith("="));
        });
    }

    [Test]
    public void DeriveRawToken_IsBoundToTokenIdAndSigningKey()
    {
        var tokenId = Guid.NewGuid();
        var original = SecureTokenHelper.DeriveRawToken(tokenId, "test-signing-key");

        Assert.Multiple(() =>
        {
            Assert.That(
                SecureTokenHelper.DeriveRawToken(Guid.NewGuid(), "test-signing-key"),
                Is.Not.EqualTo(original));
            Assert.That(
                SecureTokenHelper.DeriveRawToken(tokenId, "different-signing-key"),
                Is.Not.EqualTo(original));
            Assert.That(SecureTokenHelper.HashToken(original), Is.Not.EqualTo(original));
        });
    }
}
