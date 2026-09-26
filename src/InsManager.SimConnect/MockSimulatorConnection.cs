using InsManager.Core.Services;

namespace InsManager.SimConnect;

public sealed class MockSimulatorConnection : ISimulatorConnection
{
    public bool IsConnected { get; private set; }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(350, cancellationToken);
        IsConnected = true;
        return true;
    }
}
