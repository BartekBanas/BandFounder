using System.Security.Claims;
using BandFounder.Application.Services.Authorization.Requirements;
using BandFounder.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace BandFounder.Application.Services.Authorization.Handlers;

public class ListingAuthorizationHandler : AuthorizationHandler<IAuthorizationRequirement, Listing>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IAuthorizationRequirement requirement,
        Listing resource)
    {
        return requirement switch
        {
            IsOwnerRequirement isOwnerRequirement => HandleIsOwnerOfRequirementAsync(
                context, isOwnerRequirement, resource),

            _ => throw new NotImplementedException()
        };
    }
    
    private Task HandleIsOwnerOfRequirementAsync(
        AuthorizationHandlerContext context,
        IsOwnerRequirement requirement,
        Listing resource)
    {
        var user = context.User;
        var userId = user.Claims.First(claim => claim.Type == ClaimTypes.PrimarySid).Value;
        
        if (Guid.TryParse(userId, out var parsedGuid))
        {
            if (resource.OwnerId == parsedGuid)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}