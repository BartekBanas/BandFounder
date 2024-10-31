using System.Security.Claims;
using BandFounder.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace BandFounder.Application.Services;

public interface IAuthenticationService
{
    ClaimsIdentity GenerateClaimsIdentity(Account account);
    Guid GetUserId();
    ClaimsPrincipal GetUserClaims();
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsIdentity GenerateClaimsIdentity(Account account)
    {
        return new ClaimsIdentity(new Claim[]
        {
            new(ClaimTypes.Sid, account.Id.ToString()),
            new(ClaimTypes.PrimarySid, account.Id.ToString()),
            new(ClaimTypes.Name, account.Name),
            new(ClaimTypes.Email, account.Email),
        });
    }

    public Guid GetUserId()
    {
        if (_httpContextAccessor.HttpContext is not null)
        {
            var user = _httpContextAccessor.HttpContext.User;

            if (user is { Identity.IsAuthenticated: false })
            {
                throw new UnauthorizedAccessException();
            }

            var claim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.PrimarySid);

            if (claim == null || string.IsNullOrEmpty(claim.Value))
            {
                throw new Exception("Could not establish the user's ID");
            }

            return Guid.TryParse(claim.Value, out var userId)
                ? userId
                : throw new Exception("Could not establish the user's ID");
        }
        else
        {
            throw new UnauthorizedAccessException();
        }
    }

    public ClaimsPrincipal GetUserClaims()
    {
        if (_httpContextAccessor.HttpContext is null)
        {
            throw new UnauthorizedAccessException();
        }
        
        return _httpContextAccessor.HttpContext.User;
    }
}