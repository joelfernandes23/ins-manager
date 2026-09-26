using InsManager.Core.Services;

namespace InsManager.SimConnect;

using InsManager.Core.Models;

public sealed class MockSimulatorConnection : ISimulatorConnection
{
    public bool IsConnected { get; private set; }
    public int InputEventCount => 0;
    public string? DiagnosticReportPath => null;
    public string? LastError => null;

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(350, cancellationToken);
        IsConnected = true;
        return true;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task SendWaypointAsync(int slot, Waypoint waypoint, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SetDirectToAsync(int fromSlot, int toSlot, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ResetDriftAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SetDriftCorrectionEnabledAsync(
        bool enabled,
        TimeSpan interval,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
