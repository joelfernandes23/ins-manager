using System.Text.Json;
using InsManager.Core.Services;
using SimConnect.NET;
using SimConnect.NET.Events;

namespace InsManager.SimConnect;

public sealed class SimConnectConnection : ISimulatorConnection, IAsyncDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private SimConnectClient? _client;
    private CancellationTokenSource? _messageLoopCancellation;
    private Task? _messageLoop;

    public bool IsConnected => _client?.IsConnected == true;
    public bool IsMsfs2024 => _client?.IsMSFS2024 == true;
    public int InputEventCount { get; private set; }
    public string? DiagnosticReportPath { get; private set; }
    public string? LastError { get; private set; }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected) return true;

            await DisconnectCoreAsync();
            LastError = null;
            var client = new SimConnectClient
            {
                AutoReconnectEnabled = true,
                ReconnectDelay = TimeSpan.FromSeconds(3),
                MaxReconnectAttempts = 10,
            };
            client.ConnectionStatusChanged += OnConnectionStatusChanged;
            client.ErrorOccurred += OnErrorOccurred;

            await client.ConnectAsync(IntPtr.Zero, 0, 0, cancellationToken);
            _client = client;
            _messageLoopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _messageLoop = ProcessMessagesAsync(client, _messageLoopCancellation.Token);

            if (!client.IsMSFS2024)
            {
                LastError = "INS Manager currently supports MSFS 2024 only.";
                await DisconnectCoreAsync();
                return false;
            }

            await WriteInputEventReportAsync(client, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is SimConnectException
            or DllNotFoundException
            or InvalidOperationException)
        {
            LastError = "Start MSFS 2024 and load the FSS 727, then connect again.";
            await DisconnectCoreAsync();
            return false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task WriteInputEventReportAsync(
        SimConnectClient client,
        CancellationToken cancellationToken)
    {
        var events = await client.InputEvents.EnumerateInputEventsAsync(cancellationToken);
        var report = events
            .OrderBy(inputEvent => inputEvent.Name, StringComparer.OrdinalIgnoreCase)
            .Select(inputEvent => new InputEventReportEntry(
                inputEvent.Name,
                inputEvent.Hash,
                inputEvent.Type.ToString(),
                inputEvent.NodeNames))
            .ToArray();

        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "INS Manager",
            "diagnostics");
        Directory.CreateDirectory(directory);
        DiagnosticReportPath = Path.Combine(directory, "msfs2024-input-events.json");
        await using var stream = File.Create(DiagnosticReportPath);
        await JsonSerializer.SerializeAsync(
            stream,
            report,
            new JsonSerializerOptions { WriteIndented = true },
            cancellationToken);
        InputEventCount = report.Length;
    }

    private static async Task ProcessMessagesAsync(
        SimConnectClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && client.IsConnected)
            {
                var processed = await client.ProcessNextMessageAsync(cancellationToken);
                if (!processed) await Task.Delay(10, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void OnConnectionStatusChanged(object? sender, ConnectionStatusChangedEventArgs eventArgs)
    {
        if (eventArgs.IsDisconnected) LastError = "MSFS 2024 disconnected.";
    }

    private void OnErrorOccurred(object? sender, SimConnectErrorEventArgs eventArgs)
    {
        LastError = eventArgs.Context ?? eventArgs.Exception?.Message ?? eventArgs.Error.ToString();
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            await DisconnectCoreAsync();
            LastError = null;
            InputEventCount = 0;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task DisconnectCoreAsync()
    {
        _messageLoopCancellation?.Cancel();
        if (_messageLoop is not null)
        {
            try
            {
                await _messageLoop;
            }
            catch (SimConnectException)
            {
            }
        }

        _messageLoopCancellation?.Dispose();
        _messageLoopCancellation = null;
        _messageLoop = null;

        if (_client is not null)
        {
            _client.ConnectionStatusChanged -= OnConnectionStatusChanged;
            _client.ErrorOccurred -= OnErrorOccurred;
            await _client.DisposeAsync();
            _client = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectCoreAsync();
        _connectionLock.Dispose();
    }

    private sealed record InputEventReportEntry(
        string Name,
        ulong Hash,
        string Type,
        string NodeNames);
}
