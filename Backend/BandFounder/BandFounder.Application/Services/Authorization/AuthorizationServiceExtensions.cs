using System.Security.Claims;
using BandFounder.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace BandFounder.Application.Services.Authorization;

public static class AuthorizationServiceExtensions
{
    public static async Task AuthorizeRequiredAsync(this IAuthorizationService authorizationService,
        ClaimsPrincipal claimsPrincipal, object? resource, string policy)
    {
        try
        {
            var authorizationResult = await authorizationService.AuthorizeAsync(claimsPrincipal, resource, policy);
            
            if (!authorizationResult.Succeeded)
            {
                var resourceName = resource is not null 
                    ? resource.GetType().Name 
                    : "resource";

                throw new ForbiddenException($"You do not have permission to access this {resourceName}.");
            }
        }
        catch (ForbiddenException exception)
        {
            throw new ForbiddenException(exception.Message);
        }
        catch (Exception exception)
        {
            throw new UnauthorizedAccessException(exception.Message);
        }
    }
}