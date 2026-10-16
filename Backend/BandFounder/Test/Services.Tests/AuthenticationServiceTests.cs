using System.Security.Claims;
using BandFounder.Application.Services;
using BandFounder.Domain.Entities;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class AuthenticationServiceTests
{
    [Test]
    public void GenerateClaimsIdentity_IncludesAccountDetails()
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = "tester",
            Email = "tester@example.com",
            PasswordHash = "hash",
            PasswordVersion = 4
        };
        var service = CreateService(null);

        var identity = service.GenerateClaimsIdentity(account);

        Assert.Multiple(() =>
        {
            Assert.That(identity.FindFirst(ClaimTypes.Sid)!.Value, Is.EqualTo(account.Id.ToString()));
            Assert.That(identity.FindFirst(ClaimTypes.PrimarySid)!.Value, Is.EqualTo(account.Id.ToString()));
            Assert.That(identity.FindFirst(ClaimTypes.Name)!.Value, Is.EqualTo("tester"));
            Assert.That(identity.FindFirst(ClaimTypes.Email)!.Value, Is.EqualTo("tester@example.com"));
            Assert.That(identity.FindFirst(AuthClaimTypes.PasswordVersion)!.Value, Is.EqualTo("4"));
        });
    }

    [Test]
    public void GetUserId_WithoutHttpContext_ThrowsUnauthorized()
    {
        var service = CreateService(null);

        Assert.Throws<UnauthorizedAccessException>(() => service.GetUserId());
    }

    [Test]
    public void GetUserId_WithUnauthenticatedUser_ThrowsUnauthorized()
    {
        var context = CreateContext(new ClaimsPrincipal(new ClaimsIdentity()));
        var service = CreateService(context);

        Assert.Throws<UnauthorizedAccessException>(() => service.GetUserId());
    }

    [Test]
    public void GetUserId_WithAuthenticatedUser_ReturnsIdFromPrimarySidClaim()
    {
        var userId = Guid.NewGuid();
        var context = CreateAuthenticatedContext(new Claim(ClaimTypes.PrimarySid, userId.ToString()));
        var service = CreateService(context);

        Assert.That(service.GetUserId(), Is.EqualTo(userId));
    }

    [Test]
    public void GetUserId_WithAuthenticatedUserMissingClaim_Throws()
    {
        var context = CreateAuthenticatedContext(new Claim(ClaimTypes.Name, "tester"));
        var service = CreateService(context);

        var exception = Assert.Throws<Exception>(() => service.GetUserId());
        Assert.That(exception!.Message, Is.EqualTo("Could not establish the user's ID"));
    }

    [Test]
    public void GetUserId_WithAuthenticatedUserAndNonGuidClaim_Throws()
    {
        var context = CreateAuthenticatedContext(new Claim(ClaimTypes.PrimarySid, "not-a-guid"));
        var service = CreateService(context);

        var exception = Assert.Throws<Exception>(() => service.GetUserId());
        Assert.That(exception!.Message, Is.EqualTo("Could not establish the user's ID"));
    }

    [Test]
    public void GetUserClaims_WithoutHttpContext_ThrowsUnauthorized()
    {
        var service = CreateService(null);

        Assert.Throws<UnauthorizedAccessException>(() => service.GetUserClaims());
    }

    [Test]
    public void GetUserClaims_WithHttpContext_ReturnsThePrincipal()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateContext(principal);
        var service = CreateService(context);

        Assert.That(service.GetUserClaims(), Is.SameAs(principal));
    }

    private static AuthenticationService CreateService(HttpContext? context)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        return new AuthenticationService(accessor);
    }

    private static HttpContext CreateContext(ClaimsPrincipal principal)
    {
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        return context;
    }

    private static HttpContext CreateAuthenticatedContext(params Claim[] claims)
    {
        return CreateContext(new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth")));
    }
}
