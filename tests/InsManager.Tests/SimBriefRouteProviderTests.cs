using System.Net;
using System.Text;
using InsManager.Infrastructure;

namespace InsManager.Tests;

public sealed class SimBriefRouteProviderTests
{
    [Fact]
    public async Task ParsesWaypointCoordinatesAndOmitsPseudoFixes()
    {
        const string response = """
            {
              "navlog": {
                "fix": [
                  { "ident": "LAM", "pos_lat": "51.6462", "pos_long": "0.1558" },
                  { "ident": "TOC", "pos_lat": "51.7000", "pos_long": "0.2000" },
                  { "ident": "KONAN", "pos_lat": 50.1469, "pos_long": 1.8667 }
                ]
              }
            }
            """;
        var provider = CreateProvider(HttpStatusCode.OK, response);

        var route = await provider.GetLatestRouteAsync("123456");

        Assert.Equal(2, route.Count);
        Assert.Equal("LAM", route[0].Identifier);
        Assert.Equal(50.1469, route[1].Latitude, 4);
    }

    [Theory]
    [InlineData("")]
    [InlineData("pilot-name")]
    public async Task RejectsInvalidPilotId(string pilotId)
    {
        var provider = CreateProvider(HttpStatusCode.OK, "{}");

        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetLatestRouteAsync(pilotId));
    }

    private static SimBriefRouteProvider CreateProvider(HttpStatusCode statusCode, string content)
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        });
        return new SimBriefRouteProvider(new HttpClient(handler));
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response);
    }
}
