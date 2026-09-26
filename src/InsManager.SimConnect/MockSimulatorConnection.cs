using InsManager.Core.Services;

namespace InsManager.SimConnect;

public sealed class MockSimulatorConnection : ISimulatorConnection
{
    public bool IsConnected { get; private set; }
    public bool IsMsfs2024 => true;
    public int InputEventCount => 0;
    public string? DiagnosticReportPath => null;
    public string? LastError => null;

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(350, cancellationToken);
        IsConnected = true;
        return true;
    }
}
