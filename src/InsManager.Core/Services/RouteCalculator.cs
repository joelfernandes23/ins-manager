using InsManager.Core.Models;

namespace InsManager.Core.Services;

public static class RouteCalculator
{
    private const double EarthRadiusNauticalMiles = 3440.065;

    public static IReadOnlyList<FlightPlanLeg> BuildLegs(IReadOnlyList<Waypoint> waypoints)
    {
        var legs = new List<FlightPlanLeg>(waypoints.Count);
        for (var index = 0; index < waypoints.Count; index++)
        {
            var waypoint = waypoints[index];
            var hasPrevious = index > 0;
            var track = waypoint.TrackDegrees
                ?? (hasPrevious ? InitialBearing(waypoints[index - 1], waypoint) : null);
            var distance = waypoint.DistanceNauticalMiles
                ?? (hasPrevious ? DistanceNauticalMiles(waypoints[index - 1], waypoint) : null);

            legs.Add(new FlightPlanLeg(
                index + 1,
                waypoint,
                FormatCoordinates(waypoint),
                track is null ? "-" : $"{track.Value:000}°",
                distance is null ? "-" : $"{distance.Value:0.0} NM"));
        }

        return legs;
    }

    public static double DistanceNauticalMiles(Waypoint from, Waypoint to)
    {
        var latitude1 = DegreesToRadians(from.Latitude);
        var latitude2 = DegreesToRadians(to.Latitude);
        var deltaLatitude = latitude2 - latitude1;
        var deltaLongitude = DegreesToRadians(to.Longitude - from.Longitude);
        var a = Math.Pow(Math.Sin(deltaLatitude / 2), 2)
            + Math.Cos(latitude1) * Math.Cos(latitude2) * Math.Pow(Math.Sin(deltaLongitude / 2), 2);
        return EarthRadiusNauticalMiles * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double InitialBearing(Waypoint from, Waypoint to)
    {
        var latitude1 = DegreesToRadians(from.Latitude);
        var latitude2 = DegreesToRadians(to.Latitude);
        var deltaLongitude = DegreesToRadians(to.Longitude - from.Longitude);
        var y = Math.Sin(deltaLongitude) * Math.Cos(latitude2);
        var x = Math.Cos(latitude1) * Math.Sin(latitude2)
            - Math.Sin(latitude1) * Math.Cos(latitude2) * Math.Cos(deltaLongitude);
        return (RadiansToDegrees(Math.Atan2(y, x)) + 360) % 360;
    }

    private static string FormatCoordinates(Waypoint waypoint)
    {
        var latitude = FormatCoordinate(waypoint.Latitude, "N", "S", 2);
        var longitude = FormatCoordinate(waypoint.Longitude, "E", "W", 3);
        return $"{latitude} {longitude}";
    }

    private static string FormatCoordinate(
        double value,
        string positiveHemisphere,
        string negativeHemisphere,
        int degreeDigits)
    {
        var hemisphere = value >= 0 ? positiveHemisphere : negativeHemisphere;
        var absoluteValue = Math.Abs(value);
        var degrees = (int)Math.Floor(absoluteValue);
        var minutes = Math.Round((absoluteValue - degrees) * 60, 1);

        if (minutes >= 60)
        {
            degrees++;
            minutes = 0;
        }

        return $"{hemisphere}{degrees.ToString().PadLeft(degreeDigits, '0')}{minutes:00.0}";
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

    private static double RadiansToDegrees(double radians) => radians * 180 / Math.PI;
}
