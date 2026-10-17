using System.Net;
using System.Text;

namespace Infrastructure.Tests;

public sealed class CallbackHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
{
    public List<string> RequestUris { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri?.ToString() ?? string.Empty);
        return Task.FromResult(send(request));
    }
}

public static class JsonHttpResponse
{
    public static HttpResponseMessage From(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}
