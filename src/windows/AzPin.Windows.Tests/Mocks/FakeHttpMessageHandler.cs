using System.Net;
using System.Net.Http;

namespace AzPin.Windows.Tests.Mocks;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, HttpResponseMessage> Handler { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.OK);
    public HttpRequestMessage? LastRequest { get; private set; }
    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        LastRequest = request;
        return Task.FromResult(Handler(request));
    }
}
