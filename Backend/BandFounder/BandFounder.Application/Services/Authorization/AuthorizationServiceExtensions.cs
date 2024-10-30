using System.Security.Claims;
using BandFounder.Application.Error;
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
                throw new ForbiddenError();
        }
        catch (ForbiddenError exception)
        {
            throw new ForbiddenError(exception.Message);
        }
        catch (Exception exception)
        {
            throw new UnauthorizedAccessException(exception.Message);
        }
    }
}