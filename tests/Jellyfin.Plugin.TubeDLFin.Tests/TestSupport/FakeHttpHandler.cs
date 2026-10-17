using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TubeDLFin.Tests.TestSupport;

/// <summary>
/// Answers HTTP requests from a lookup of URL to response factory and records every request.
/// </summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes = new(StringComparer.Ordinal);

    private readonly List<Uri> _requests = [];

    public IReadOnlyList<Uri> Requests => _requests;

    public FakeHttpHandler On(string url, Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _routes[url] = respond;
        return this;
    }

    public FakeHttpHandler OnContent(string url, byte[] content) =>
        On(url, _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) });

    public FakeHttpHandler OnText(string url, string content) =>
        On(url, _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });

    /// <summary>Simulates HttpClientHandler following a redirect: the response's request URI is the final URL.</summary>
    public FakeHttpHandler OnRedirect(string url, string finalUrl) =>
        On(url, _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, finalUrl),
            Content = new StringContent("<html></html>"),
        });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request.RequestUri!);
        var response = _routes.TryGetValue(request.RequestUri!.ToString(), out var respond)
            ? respond(request)
            : new HttpResponseMessage(HttpStatusCode.NotFound);
        response.RequestMessage ??= request;
        return Task.FromResult(response);
    }
}
