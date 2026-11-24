using System.Security.Claims;
using BandFounder.Application.Exceptions;
using BandFounder.Application.Services.Authorization;
using BandFounder.Application.Services.Authorization.Handlers;
using BandFounder.Application.Services.Authorization.Requirements;
using BandFounder.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;

namespace Services.Tests;

[TestFixture]
public class AuthorizationHandlerTests
{
    [Test]
    public async Task AccountHandler_SucceedsWhenUserOwnsAccount()
    {
        var accountId = Guid.NewGuid();
        var handler = new AccountAuthorizationHandler();
        var context = CreateContext(accountId, new IsOwnerRequirement(), new Account
        {
            Id = accountId,
            Name = "tester",
            Email = "t@e.com",
            PasswordHash = "h"
        });

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task AccountHandler_DoesNotSucceedForOtherAccount()
    {
        var handler = new AccountAuthorizationHandler();
        var context = CreateContext(Guid.NewGuid(), new IsOwnerRequirement(), new Account
        {
            Id = Guid.NewGuid(),
            Name = "tester",
            Email = "t@e.com",
            PasswordHash = "h"
        });

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task ChatroomHandler_IsMemberOf_SucceedsForMember()
    {
        var userId = Guid.NewGuid();
        var handler = new ChatRoomAuthorizationHandler();
        var context = CreateContext(userId, new IsMemberOfRequirement(), new Chatroom
        {
            Id = Guid.NewGuid(),
            Name = "Room",
            Members = [new Account { Id = userId, Name = "tester", Email = "t@e.com", PasswordHash = "h" }]
        });

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task ChatroomHandler_IsOwnerOf_SucceedsForOwner()
    {
        var ownerId = Guid.NewGuid();
        var handler = new ChatRoomAuthorizationHandler();
        var context = CreateContext(ownerId, new IsOwnerRequirement(), new Chatroom
        {
            Id = Guid.NewGuid(),
            Name = "Room",
            OwnerId = ownerId
        });

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public void ChatroomHandler_UnknownRequirement_Throws()
    {
        var handler = new ChatRoomAuthorizationHandler();
        var context = CreateContext(Guid.NewGuid(), Substitute.For<IAuthorizationRequirement>(), new Chatroom
        {
            Id = Guid.NewGuid(),
            Name = "Room"
        });

        Assert.ThrowsAsync<NotImplementedException>(() => handler.HandleAsync(context));
    }

    [Test]
    public async Task ListingHandler_IsOwnerOf_SucceedsForOwner()
    {
        var ownerId = Guid.NewGuid();
        var handler = new ListingAuthorizationHandler();
        var context = CreateContext(ownerId, new IsOwnerRequirement(), new Listing
        {
            Id = Guid.NewGuid(),
            Name = "Listing",
            OwnerId = ownerId,
            Type = ListingType.Band
        });

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task ListingHandler_IsOwnerOf_DoesNotSucceedForNonOwner()
    {
        var handler = new ListingAuthorizationHandler();
        var context = CreateContext(Guid.NewGuid(), new IsOwnerRequirement(), new Listing
        {
            Id = Guid.NewGuid(),
            Name = "Listing",
            OwnerId = Guid.NewGuid(),
            Type = ListingType.Band
        });

        await handler.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public void ListingHandler_UnknownRequirement_Throws()
    {
        var handler = new ListingAuthorizationHandler();
        var context = CreateContext(Guid.NewGuid(), Substitute.For<IAuthorizationRequirement>(), new Listing
        {
            Id = Guid.NewGuid(),
            Name = "Listing",
            OwnerId = Guid.NewGuid(),
            Type = ListingType.Band
        });

        Assert.ThrowsAsync<NotImplementedException>(() => handler.HandleAsync(context));
    }

    private static AuthorizationHandlerContext CreateContext(
        Guid userId,
        IAuthorizationRequirement requirement,
        object resource)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.PrimarySid, userId.ToString())],
            authenticationType: "TestAuth"));
        return new AuthorizationHandlerContext([requirement], principal, resource);
    }
}
