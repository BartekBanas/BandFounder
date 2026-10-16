using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BandFounder.Application.Services.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Services.Tests;

[TestFixture]
public class JwtServiceTests
{
    private const string Issuer = "bandfounder-test-issuer";
    private const string Audience = "bandfounder-test-audience";
    private const string SecretKey = "an-extremely-long-secret-used-only-for-unit-tests-0123456789";

    [Test]
    public void GenerateSymmetricJwtToken_ProducesValidatedTokenWithClaimsIssuerAudienceAndExpiry()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.PrimarySid, userId.ToString()),
            new Claim(ClaimTypes.Name, "tester")
        ]);
        var service = CreateService(expiresMinutes: 30);

        var token = service.GenerateSymmetricJwtToken(identity);

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, CreateValidationParameters(), out var validatedToken);
        var jwt = (JwtSecurityToken)validatedToken;

        Assert.Multiple(() =>
        {
            Assert.That(principal.FindFirst(ClaimTypes.PrimarySid)!.Value, Is.EqualTo(userId.ToString()));
            Assert.That(principal.FindFirst(ClaimTypes.Name)!.Value, Is.EqualTo("tester"));
            Assert.That(jwt.Issuer, Is.EqualTo(Issuer));
            Assert.That(jwt.Audiences, Does.Contain(Audience));
            Assert.That(jwt.ValidTo, Is.EqualTo(DateTime.UtcNow.AddMinutes(30)).Within(TimeSpan.FromMinutes(2)));
        });
    }

    private static JwtService CreateService(int expiresMinutes = 60)
    {
        return new JwtService(Options.Create(new JwtConfiguration
        {
            Issuer = Issuer,
            Audience = Audience,
            PublicKey = "public-key",
            PublicKeyInfo = "public-key-info",
            SecretKey = SecretKey,
            Expires = expiresMinutes
        }));
    }

    private static TokenValidationParameters CreateValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
    }
}
