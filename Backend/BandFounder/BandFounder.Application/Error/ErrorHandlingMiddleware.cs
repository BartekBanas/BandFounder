using BandFounder.Infrastructure.Errors;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BandFounder.Application.Error;

public class ErrorHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (ex is BadRequestError or ValidationException)
        {
            await HandleErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SpotifyAccountNotLinkedError)
        {
            await HandleErrorAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (Exception ex) when (ex is ForbiddenError)
        {
            await HandleErrorAsync(context, StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (Exception ex) when (ex is ItemNotFoundErrorException or NotFoundError)
        {
            await HandleErrorAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (Exception ex) when (ex is ItemDuplicatedErrorException or ConflictError)
        {
            await HandleErrorAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await HandleErrorAsync(context, StatusCodes.Status500InternalServerError, "Something went wrong");
        }
    }

    private async Task HandleErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(message);
    }
}