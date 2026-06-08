using System.Net.Http;

namespace AzPin.Windows.Tests.Mocks;

internal sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => client;
}
