using InsManager.Core.Models;
using InsManager.Core.Services;

namespace InsManager.Infrastructure;

public sealed class MockRouteProvider : IRouteProvider
{
    private static readonly Waypoint[] Route =
    [
        new("LAM", 51.6462, 0.1558), new("SFD", 50.7606, 0.1215),
        new("KONAN", 50.1469, 1.8667), new("KOK", 51.0947, 2.6517),
        new("NIK", 50.9183, 4.5036), new("LNO", 50.5856, 5.7103),
        new("SPI", 50.5147, 5.6231), new("DIK", 49.8611, 6.1297),
        new("RUDUS", 49.5236, 7.1589), new("UNOKO", 49.2564, 8.1086),
        new("KRH", 49.0239, 8.5842), new("LBU", 48.9122, 9.3406),
    ];

    public Task<IReadOnlyList<Waypoint>> GetLatestRouteAsync(string pilotId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Waypoint>>(Route);
}
