using BandFounder.Api.Middlewares;
using BandFounder.Application.Exceptions;
using BandFounder.Domain.Exceptions;
using BandFounder.Infrastructure.Spotify.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Api.Tests;

[TestFixture]
public class ErrorHandlingMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_NoException_LeavesResponseUnchanged()
    {
        var (statusCode, _) = await Invoke(null);

        Assert.That(statusCode, Is.EqualTo(StatusCodes.Status200OK));
    }

    [TestCaseSource(nameof(MappedExceptions))]
    public async Task InvokeAsync_MapsExceptionToStatusAndBody(
        Exception thrown, int expectedStatus, string expectedBody)
    {
        var (statusCode, body) = await Invoke(thrown);

        Assert.That(statusCode, Is.EqualTo(expectedStatus));
        Assert.That(body, Is.EqualTo(expectedBody));
    }

    [Test]
    public async Task InvokeAsync_SetsPlainTextContentType()
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var middleware = new ErrorHandlingMiddleware(Substitute.For<ILogger<ErrorHandlingMiddleware>>());

        await middleware.InvokeAsync(
            context,
            _ => Task.FromException(new NotFoundException("missing")));

        Assert.That(context.Response.ContentType, Is.EqualTo("text/plain"));
    }

    private static IEnumerable<TestCaseData> MappedExceptions()
    {
        yield return Case(
            new ValidationException([new ValidationFailure("Name", "Name is required")]),
            StatusCodes.Status400BadRequest,
            "Name is required");
        yield return Case(new BadRequestException("bad"), StatusCodes.Status400BadRequest, "bad");
        yield return Case(new CustomValidationException("invalid"), StatusCodes.Status400BadRequest, "invalid");
        yield return Case(new UnauthorizedAccessException("nope"), StatusCodes.Status401Unauthorized, "nope");
        yield return Case(new ForbiddenException("denied"), StatusCodes.Status403Forbidden, "denied");
        yield return Case(new ItemNotFoundException("missing item"), StatusCodes.Status404NotFound, "missing item");
        yield return Case(new NotFoundException("missing"), StatusCodes.Status404NotFound, "missing");
        yield return Case(new ItemDuplicatedException("dup"), StatusCodes.Status409Conflict, "dup");
        yield return Case(new ConflictException("conflict"), StatusCodes.Status409Conflict, "conflict");
        var alreadyConnected = new SpotifyAccountAlreadyConnectedException();
        yield return Case(alreadyConnected, StatusCodes.Status409Conflict, alreadyConnected.Message);
        var reauthorizationRequired = new SpotifyReauthorizationRequiredException();
        yield return Case(reauthorizationRequired, StatusCodes.Status410Gone, reauthorizationRequired.Message);
        var notLinked = new SpotifyAccountNotLinkedException();
        yield return Case(notLinked, StatusCodes.Status422UnprocessableEntity, notLinked.Message);
        yield return Case(
            new RedundantRequestException("already done"),
            StatusCodes.Status204NoContent,
            "already done");
        yield return Case(
            new InvalidOperationException("kaboom"),
            StatusCodes.Status500InternalServerError,
            "Something went wrong");
    }

    private static TestCaseData Case(Exception thrown, int status, string body)
    {
        return new TestCaseData(thrown, status, body).SetArgDisplayNames(thrown.GetType().Name);
    }

    private static async Task<(int StatusCode, string Body)> Invoke(Exception? thrown)
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var middleware = new ErrorHandlingMiddleware(Substitute.For<ILogger<ErrorHandlingMiddleware>>());
        RequestDelegate next = _ => thrown is null ? Task.CompletedTask : Task.FromException(thrown);

        await middleware.InvokeAsync(context, next);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }
}
