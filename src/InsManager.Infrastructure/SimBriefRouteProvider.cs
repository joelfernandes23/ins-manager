using System.Globalization;
using System.Net;
using System.Text.Json;
using InsManager.Core.Models;
using InsManager.Core.Services;

namespace InsManager.Infrastructure;

public sealed class SimBriefRouteProvider(HttpClient httpClient) : IRouteProvider
{
    private static readonly HashSet<string> IgnoredIdentifiers =
        new(StringComparer.OrdinalIgnoreCase) { "TOC", "TOD" };

    public async Task<IReadOnlyList<Waypoint>> GetLatestRouteAsync(
        string pilotId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pilotId) || !pilotId.All(char.IsDigit))
        {
            throw new ArgumentException("Enter a numeric SimBrief Pilot ID.", nameof(pilotId));
        }

        var endpoint = $"https://www.simbrief.com/api/xml.fetcher.php?userid={Uri.EscapeDataString(pilotId)}&json=1";
        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException("SimBrief could not find a flight plan for this Pilot ID.");
        }

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("navlog", out var navlog)
            || !navlog.TryGetProperty("fix", out var fixes)
            || fixes.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("The SimBrief response did not contain a navigational route.");
        }

        var waypoints = new List<Waypoint>();
        foreach (var fix in fixes.EnumerateArray())
        {
            var identifier = GetString(fix, "ident");
            if (string.IsNullOrWhiteSpace(identifier) || IgnoredIdentifiers.Contains(identifier)) continue;
            if (!TryGetDouble(fix, "pos_lat", out var latitude)
                || !TryGetDouble(fix, "pos_long", out var longitude)) continue;

            var track = GetFirstDouble(fix, "track_true", "track_mag", "track");
            var distance = GetFirstDouble(fix, "distance");
            waypoints.Add(new Waypoint(identifier, latitude, longitude, track, distance));
        }

        if (waypoints.Count == 0)
        {
            throw new InvalidOperationException("SimBrief returned a flight plan without usable waypoint coordinates.");
        }

        return waypoints;
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) ? property.ToString() : null;

    private static bool TryGetDouble(JsonElement element, string name, out double value)
    {
        value = default;
        if (!element.TryGetProperty(name, out var property)) return false;
        return property.ValueKind == JsonValueKind.Number
            ? property.TryGetDouble(out value)
            : double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static double? GetFirstDouble(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetDouble(element, name, out var value)) return value;
        }

        return null;
    }
}
