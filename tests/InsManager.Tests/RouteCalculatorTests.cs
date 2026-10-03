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
        Assert.Equal("N5138.8 E00009.3", legs[0].Coordinates);
        Assert.Equal("123°", legs[0].Track);
        Assert.Equal("42.5 NM", legs[0].Distance);
        Assert.Equal("-", legs[0].InsSlot);
        Assert.EndsWith("°", legs[1].Track);
        Assert.EndsWith(" NM", legs[1].Distance);
    }

    [Fact]
    public void FormatsCoordinatesLikeTheCivaDisplay()
    {
        var leg = RouteCalculator.BuildLegs(
            [new Waypoint("TEST", 42.2794, -87.0633)])[0];

        Assert.Equal("N4216.8 W08703.8", leg.Coordinates);
    }
}
