namespace InsManager.Core.Services;

using InsManager.Core.Models;

public interface ISimulatorConnection
{
    bool IsConnected { get; }
    bool IsMsfs2024 { get; }
    int InputEventCount { get; }
    string? DiagnosticReportPath { get; }
    string? LastError { get; }

    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task SendWaypointAsync(int slot, Waypoint waypoint, CancellationToken cancellationToken = default);
    Task SetDirectToAsync(int fromSlot, int toSlot, CancellationToken cancellationToken = default);
    Task ResetDriftAsync(CancellationToken cancellationToken = default);
    Task SetDriftCorrectionEnabledAsync(
        bool enabled,
        TimeSpan interval,
        CancellationToken cancellationToken = default);
}
