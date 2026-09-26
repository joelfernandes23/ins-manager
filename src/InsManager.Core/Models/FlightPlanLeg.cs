namespace InsManager.Core.Models;

public sealed record FlightPlanLeg(
    int Number,
    Waypoint Waypoint,
    string Coordinates,
    string Track,
    string Distance,
    string InsSlot = "—");
