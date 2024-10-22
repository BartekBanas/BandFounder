using BandFounder.Infrastructure.Errors;
using BandFounder.Infrastructure.Errors.Api;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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
        
        catch(Exception error)
        {
            throw new BadRequestError(error.Message, error);
        }
    }
}