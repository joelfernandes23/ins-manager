namespace InsManager.Core.Services;

public interface ISimulatorConnection
{
    bool IsConnected { get; }
    bool IsMsfs2024 { get; }
    int InputEventCount { get; }
    string? DiagnosticReportPath { get; }
    string? LastError { get; }

    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
}
