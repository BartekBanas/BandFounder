using System.Security.Claims;
using BandFounder.Application.Services.Authorization.Requirements;
using BandFounder.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace BandFounder.Application.Services.Authorization.Handlers;

public class AccountAuthorizationHandler : AuthorizationHandler<IsOwnerRequirement, Account>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IsOwnerRequirement requirement,
        Account resource)
    {
        var user = context.User;
        var userId = user.Claims.First(claim => claim.Type == ClaimTypes.PrimarySid).Value;

        if (new Guid(userId) == resource.Id)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}