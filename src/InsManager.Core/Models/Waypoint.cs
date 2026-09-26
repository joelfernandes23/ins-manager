namespace InsManager.Core.Models;

public sealed record Waypoint(
    string Identifier,
    double Latitude,
    double Longitude,
    double? TrackDegrees = null,
    double? DistanceNauticalMiles = null);
