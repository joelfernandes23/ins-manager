using InsManager.Core.Models;

namespace InsManager.Core.Services;

public interface IRouteProvider
{
    Task<IReadOnlyList<Waypoint>> GetLatestRouteAsync(string pilotId, CancellationToken cancellationToken = default);
}
