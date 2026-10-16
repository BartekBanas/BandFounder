using System.Security.Claims;
using BandFounder.Application.Exceptions;
using BandFounder.Application.Services.Authorization;
using BandFounder.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class AuthorizationServiceExtensionsTests
{
    private readonly IAuthorizationService _authorizationService = Substitute.For<IAuthorizationService>();
    private readonly ClaimsPrincipal _principal = new(new ClaimsIdentity());

    [Test]
    public async Task AuthorizeRequiredAsync_Success_DoesNotThrow()
    {
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());

        Assert.DoesNotThrowAsync(() =>
            _authorizationService.AuthorizeRequiredAsync(_principal, new Chatroom { Id = Guid.NewGuid(), Name = "r" }, AuthorizationPolicies.IsMemberOf));
    }

    [Test]
    public void AuthorizeRequiredAsync_Failure_ThrowsForbiddenWithResourceName()
    {
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed());

        var exception = Assert.ThrowsAsync<ForbiddenException>(() =>
            _authorizationService.AuthorizeRequiredAsync(_principal, new Chatroom { Id = Guid.NewGuid(), Name = "r" }, AuthorizationPolicies.IsMemberOf));

        Assert.That(exception!.Message, Does.Contain("Chatroom"));
    }

    [Test]
    public void AuthorizeRequiredAsync_FailureWithoutResource_ThrowsForbiddenWithGenericName()
    {
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed());

        var exception = Assert.ThrowsAsync<ForbiddenException>(() =>
            _authorizationService.AuthorizeRequiredAsync(_principal, null, AuthorizationPolicies.IsMemberOf));

        Assert.That(exception!.Message, Does.Contain("resource"));
    }

    [Test]
    public void AuthorizeRequiredAsync_WhenAuthorizationServiceThrows_UnexpectedException_IsWrapped()
    {
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns<Task<AuthorizationResult>>(_ => throw new InvalidOperationException("boom"));

        var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authorizationService.AuthorizeRequiredAsync(_principal, new Chatroom { Id = Guid.NewGuid(), Name = "r" }, AuthorizationPolicies.IsMemberOf));

        Assert.That(exception!.Message, Is.EqualTo("boom"));
    }
}
