namespace InsManager.Core.Services;

public interface ISimulatorConnection
{
    bool IsConnected { get; }

    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
}
