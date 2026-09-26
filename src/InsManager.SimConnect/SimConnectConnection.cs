using InsManager.Core.Services;
using InsManager.Core.Models;
using SimConnect.NET;
using SimConnect.NET.Events;

namespace InsManager.SimConnect;

public sealed class SimConnectConnection : ISimulatorConnection, IAsyncDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private SimConnectClient? _client;
    private CancellationTokenSource? _messageLoopCancellation;
    private Task? _messageLoop;
    private CancellationTokenSource? _driftCorrectionCancellation;
    private Task? _driftCorrectionLoop;

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

    public async Task SendWaypointAsync(
        int slot,
        Waypoint waypoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(slot, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slot, 9);
        ArgumentNullException.ThrowIfNull(waypoint);

        var client = GetConnectedClient();
        await client.SimVars.SetAsync($"L:FSS_B727_CIVA_WP_{slot}_LAT", "Number", waypoint.Latitude, cancellationToken: cancellationToken);
        await client.SimVars.SetAsync($"L:FSS_B727_CIVA_WP_{slot}_LON", "Number", waypoint.Longitude, cancellationToken: cancellationToken);
    }

    public async Task SetDirectToAsync(
        int fromSlot,
        int toSlot,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(fromSlot, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fromSlot, 9);
        ArgumentOutOfRangeException.ThrowIfLessThan(toSlot, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(toSlot, 9);

        var client = GetConnectedClient();
        await client.SimVars.SetAsync("L:FSS_B727_CIVA_FROM", "Number", (double)fromSlot, cancellationToken: cancellationToken);
        await client.SimVars.SetAsync("L:FSS_B727_CIVA_TO", "Number", (double)toSlot, cancellationToken: cancellationToken);
    }

    public async Task ResetDriftAsync(CancellationToken cancellationToken = default)
    {
        var client = GetConnectedClient();
        var latitude = await client.SimVars.GetAsync<double>(
            "L:FSS_B727_CIVA_SIM_LAT",
            "Number",
            cancellationToken: cancellationToken);
        var longitude = await client.SimVars.GetAsync<double>(
            "L:FSS_B727_CIVA_SIM_LON",
            "Number",
            cancellationToken: cancellationToken);
        await client.SimVars.SetAsync("L:FSS_B727_CIVA_POS_LAT", "Number", latitude, cancellationToken: cancellationToken);
        await client.SimVars.SetAsync("L:FSS_B727_CIVA_POS_LON", "Number", longitude, cancellationToken: cancellationToken);
    }

    public async Task SetDriftCorrectionEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        await StopDriftCorrectionAsync();
        if (!enabled) return;

        GetConnectedClient();
        _driftCorrectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _driftCorrectionLoop = CorrectDriftAsync(_driftCorrectionCancellation.Token);
    }

    private async Task CorrectDriftAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            do
            {
                await ResetDriftAsync(cancellationToken);
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is SimConnectException or InvalidOperationException)
        {
            LastError = $"Drift correction stopped: {exception.Message}";
        }
    }

    private async Task StopDriftCorrectionAsync()
    {
        _driftCorrectionCancellation?.Cancel();
        if (_driftCorrectionLoop is not null) await _driftCorrectionLoop;
        _driftCorrectionCancellation?.Dispose();
        _driftCorrectionCancellation = null;
        _driftCorrectionLoop = null;
    }

    private SimConnectClient GetConnectedClient() =>
        _client is { IsConnected: true } client
            ? client
            : throw new InvalidOperationException("MSFS 2024 is not connected.");

    private async Task DisconnectCoreAsync()
    {
        await StopDriftCorrectionAsync();
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
}
