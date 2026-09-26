using InsManager.Infrastructure;

namespace InsManager.Tests;

public sealed class MockRouteProviderTests
{
    [Fact]
    public async Task ReturnsRouteLongerThanInitialInsBuffer()
    {
        var provider = new MockRouteProvider();
        var route = await provider.GetLatestRouteAsync("123456");
        Assert.True(route.Count > 9);
        Assert.All(route, waypoint => Assert.False(string.IsNullOrWhiteSpace(waypoint.Identifier)));
    }
}
