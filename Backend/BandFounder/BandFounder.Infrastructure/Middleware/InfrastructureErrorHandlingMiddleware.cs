using BandFounder.Infrastructure.Errors;
using BandFounder.Infrastructure.Errors.Api;
using Microsoft.AspNetCore.Http;

namespace BandFounder.Infrastructure.Middleware;

public class InfrastructureErrorHandlingMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next.Invoke(context);
        }
        
        // Infrastructure errors
        catch (ItemNotFoundErrorException error)
        {
            throw new NotFoundError(error.Message, error);
        }
        catch (ItemDuplicatedErrorException error)
        {
            throw new ConflictError(error.Message, error);
        }
    }
}