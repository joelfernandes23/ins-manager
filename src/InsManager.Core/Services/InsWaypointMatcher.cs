using InsManager.Core.Models;

namespace InsManager.Core.Services;

public static class InsWaypointMatcher
{
    public static Waypoint? FindClosest(
        IEnumerable<Waypoint> route,
        double latitude,
        double longitude,
        double maximumDistanceNauticalMiles = 0.25)
    {
        var observed = new Waypoint("Observed", latitude, longitude);
        return route
            .Select(waypoint => new
            {
                Waypoint = waypoint,
                Distance = RouteCalculator.DistanceNauticalMiles(observed, waypoint),
            })
            .Where(candidate => candidate.Distance <= maximumDistanceNauticalMiles)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Waypoint)
            .FirstOrDefault();
    }
}
