using InsManager.Core.Models;
using InsManager.Core.Services;

namespace InsManager.Tests;

public sealed class InsWaypointMatcherTests
{
    private static readonly Waypoint RouteWaypoint = new("TEST", 51.5000, -0.1000);

    [Fact]
    public void FindsRouteWaypointWithinCivaEntryPrecision()
    {
        var match = InsWaypointMatcher.FindClosest(
            [RouteWaypoint],
            51.5010,
            -0.1010);

        Assert.Same(RouteWaypoint, match);
    }

    [Fact]
    public void RejectsCoordinatesOutsideMatchingTolerance()
    {
        var match = InsWaypointMatcher.FindClosest(
            [RouteWaypoint],
            51.5200,
            -0.1200);

        Assert.Null(match);
    }
}
