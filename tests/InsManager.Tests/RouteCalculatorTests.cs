using InsManager.Core.Models;
using InsManager.Core.Services;

namespace InsManager.Tests;

public sealed class RouteCalculatorTests
{
    [Fact]
    public void BuildsNumberedLegsWithTrackDistanceAndCoordinates()
    {
        Waypoint[] route =
        [
            new("LAM", 51.6462, 0.1558, 123, 42.5),
            new("SFD", 50.7606, 0.1215),
        ];

        var legs = RouteCalculator.BuildLegs(route);

        Assert.Equal(2, legs.Count);
        Assert.Equal(1, legs[0].Number);
        Assert.Equal("N51.6462 E000.1558", legs[0].Coordinates);
        Assert.Equal("123°", legs[0].Track);
        Assert.Equal("42.5 NM", legs[0].Distance);
        Assert.Equal("-", legs[0].InsSlot);
        Assert.EndsWith("°", legs[1].Track);
        Assert.EndsWith(" NM", legs[1].Distance);
    }
}
