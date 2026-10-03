using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InsManager.App.Services;
using InsManager.Core.Models;
using InsManager.Core.Services;

namespace InsManager.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRouteProvider _routeProvider;
    private readonly ISettingsService _settingsService;
    private readonly ISimulatorConnection _simulatorConnection;
    private readonly ThemeService _themeService;
    private readonly DispatcherTimer _insStateTimer;
    private bool _isRefreshingInsState;
    private int? _lastObservedFromSlot;
    private int? _lastObservedToSlot;
    private DateTimeOffset _lastSlotRefresh = DateTimeOffset.MinValue;

    [ObservableProperty] private string _connectionStatus = "Simulator disconnected";
    [ObservableProperty] private string _connectionAction = "Connect";
    [ObservableProperty] private string _routeStatus = "No flight plan downloaded";
    [ObservableProperty] private string _downloadStatus = "Idle";
    [ObservableProperty] private string _fromSlot = "-";
    [ObservableProperty] private string _toSlot = "-";
    [ObservableProperty] private string _accuracy = "-";
    [ObservableProperty] private string _simBriefPilotId = "";
    [ObservableProperty] private string _selectedTheme = "Dark";
    [ObservableProperty] private string _selectedAircraft = "FSS Boeing 727";
    [ObservableProperty] private DriftIntervalOption _selectedDriftInterval;
    [ObservableProperty] private bool _autoManageWaypoints = true;
    [ObservableProperty] private bool _correctDrift = true;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isRouteLoaded;
    [ObservableProperty] private bool _isSettingsOpen;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isSendDialogOpen;
    [ObservableProperty] private bool _isDirectDialogOpen;
    [ObservableProperty] private int _selectedInsSlot = 1;
    [ObservableProperty] private FlightPlanLeg? _selectedFlightPlanLeg;

    public MainViewModel(
        IRouteProvider routeProvider,
        ISettingsService settingsService,
        ISimulatorConnection simulatorConnection,
        ThemeService themeService)
    {
        _routeProvider = routeProvider;
        _settingsService = settingsService;
        _simulatorConnection = simulatorConnection;
        _themeService = themeService;
        _selectedDriftInterval = DriftIntervals[1];
        _insStateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _insStateTimer.Tick += RefreshInsState;
        ResetSlots();
    }

    public ObservableCollection<InsSlot> Slots { get; } = [];
    public ObservableCollection<FlightPlanLeg> FlightPlan { get; } = [];
    public IReadOnlyList<string> Themes { get; } = ["Dark", "Light"];
    public IReadOnlyList<string> SupportedAircraft { get; } = ["FSS Boeing 727"];
    public IReadOnlyList<DriftIntervalOption> DriftIntervals { get; } =
    [
        new("10 minutes", 10),
        new("30 minutes", 30),
        new("1 hour", 60),
    ];
    public IReadOnlyList<int> InsSlotNumbers { get; } = Enumerable.Range(1, 9).ToArray();

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadAsync();
        SimBriefPilotId = settings.SimBriefPilotId;
        SelectedTheme = settings.Theme;
        SelectedAircraft = SupportedAircraft.Contains(settings.Aircraft)
            ? settings.Aircraft
            : SupportedAircraft[0];
        SelectedDriftInterval = DriftIntervals.FirstOrDefault(option =>
            option.Minutes == settings.DriftCorrectionIntervalMinutes) ?? DriftIntervals[1];
        _themeService.Apply(SelectedTheme);
    }

    partial void OnSelectedThemeChanged(string value) => _themeService.Apply(value);

    partial void OnCorrectDriftChanged(bool value)
    {
        if (IsConnected) _ = UpdateDriftCorrectionAsync(value);
    }

    partial void OnAutoManageWaypointsChanged(bool value)
    {
        if (value)
        {
            _lastObservedFromSlot = null;
            _lastObservedToSlot = null;
        }
    }

    partial void OnSelectedDriftIntervalChanged(DriftIntervalOption value)
    {
        if (IsConnected && CorrectDrift) _ = UpdateDriftCorrectionAsync(true);
    }

    private async Task UpdateDriftCorrectionAsync(bool enabled)
    {
        try
        {
            await _simulatorConnection.SetDriftCorrectionEnabledAsync(
                enabled,
                TimeSpan.FromMinutes(SelectedDriftInterval.Minutes));
            RouteStatus = enabled
                ? $"Drift correction every {SelectedDriftInterval.Label.ToLowerInvariant()}"
                : "Drift correction disabled";
        }
        catch (Exception exception)
        {
            RouteStatus = $"Drift correction failed: {exception.Message}";
        }
    }

    [RelayCommand]
    private void OpenSettings() => IsSettingsOpen = true;

    [RelayCommand]
    private void CloseSettings() => IsSettingsOpen = false;

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await _settingsService.SaveAsync(new AppSettings(
            SimBriefPilotId.Trim(),
            SelectedTheme,
            SelectedAircraft,
            SelectedDriftInterval.Minutes));
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private async Task ToggleConnectionAsync()
    {
        if (IsConnected)
        {
            ConnectionStatus = "Disconnecting…";
            _insStateTimer.Stop();
            await _simulatorConnection.SetDriftCorrectionEnabledAsync(
                false,
                TimeSpan.FromMinutes(SelectedDriftInterval.Minutes));
            await _simulatorConnection.DisconnectAsync();
            IsConnected = false;
            ConnectionAction = "Connect";
            ConnectionStatus = "Simulator disconnected";
            return;
        }

        ConnectionStatus = "Connecting…";
        _lastObservedFromSlot = null;
        _lastObservedToSlot = null;
        IsConnected = await _simulatorConnection.ConnectAsync();
        if (IsConnected && CorrectDrift)
        {
            await _simulatorConnection.SetDriftCorrectionEnabledAsync(
                true,
                TimeSpan.FromMinutes(SelectedDriftInterval.Minutes));
        }
        if (IsConnected)
        {
            await RefreshInsStateAsync();
            _insStateTimer.Start();
        }
        ConnectionAction = IsConnected ? "Disconnect" : "Connect";
        ConnectionStatus = IsConnected
            ? "MSFS connected"
            : _simulatorConnection.LastError ?? "Connection failed";
    }

    private async void RefreshInsState(object? sender, EventArgs eventArgs) =>
        await RefreshInsStateAsync();

    private async Task RefreshInsStateAsync()
    {
        if (!IsConnected || _isRefreshingInsState) return;

        _isRefreshingInsState = true;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var state = await _simulatorConnection.GetInsStateAsync(timeout.Token);
            FromSlot = state.FromSlot.ToString();
            ToSlot = state.ToSlot.ToString();
            Accuracy = state.AccuracyIndex.ToString("0.0");

            if (DateTimeOffset.UtcNow - _lastSlotRefresh >= TimeSpan.FromSeconds(5))
                await RefreshSlotsFromAircraftAsync();

            var stateChanged = state.FromSlot != _lastObservedFromSlot
                || state.ToSlot != _lastObservedToSlot;
            if (stateChanged && AutoManageWaypoints && IsRouteLoaded)
                await RefillUpcomingSlotsAsync(state.FromSlot, state.ToSlot);

            _lastObservedFromSlot = state.FromSlot;
            _lastObservedToSlot = state.ToSlot;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            _insStateTimer.Stop();
            IsConnected = false;
            ConnectionAction = "Connect";
            ConnectionStatus = _simulatorConnection.LastError ?? "MSFS disconnected";
        }
        finally
        {
            _isRefreshingInsState = false;
        }
    }

    private async Task RefreshSlotsFromAircraftAsync()
    {
        _lastSlotRefresh = DateTimeOffset.UtcNow;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        var aircraftSlots = await _simulatorConnection.GetInsWaypointsAsync(timeout.Token);
        var route = FlightPlan.Select(leg => leg.Waypoint).ToArray();

        foreach (var aircraftSlot in aircraftSlots)
        {
            Waypoint? waypoint = null;
            var isEmpty = Math.Abs(aircraftSlot.Latitude) < 0.000001
                && Math.Abs(aircraftSlot.Longitude) < 0.000001;

            if (!isEmpty)
            {
                waypoint = InsWaypointMatcher.FindClosest(
                    route,
                    aircraftSlot.Latitude,
                    aircraftSlot.Longitude)
                    ?? new Waypoint(
                        $"MAN-{aircraftSlot.SlotNumber}",
                        aircraftSlot.Latitude,
                        aircraftSlot.Longitude);
            }

            SetSlot(
                aircraftSlot.SlotNumber,
                waypoint,
                isEmpty ? "Empty" : "Aircraft");
        }
    }

    private async Task RefillUpcomingSlotsAsync(int fromSlot, int toSlot)
    {
        if (fromSlot is < 1 or > 9 || toSlot is < 1 or > 9) return;

        var toWaypoint = Slots[toSlot].Waypoint;
        if (toWaypoint is null) return;

        var toRouteIndex = -1;
        for (var index = 0; index < FlightPlan.Count; index++)
        {
            if (ReferenceEquals(FlightPlan[index].Waypoint, toWaypoint))
            {
                toRouteIndex = index;
                break;
            }
        }

        if (toRouteIndex < 0) return;

        var updatedSlots = 0;
        var refillPlan = InsSlotPlanner.BuildRefillPlan(
            fromSlot,
            toSlot,
            toRouteIndex,
            FlightPlan.Count);

        foreach (var assignment in refillPlan)
        {
            var waypoint = FlightPlan[assignment.RouteIndex].Waypoint;
            if (ReferenceEquals(Slots[assignment.SlotNumber].Waypoint, waypoint)) continue;

            await _simulatorConnection.SendWaypointAsync(assignment.SlotNumber, waypoint);
            SetSlot(assignment.SlotNumber, waypoint, "Synced");
            updatedSlots++;
        }

        if (updatedSlots > 0)
            RouteStatus = $"Auto-synced {updatedSlots} upcoming INS slots";
    }

    [RelayCommand]
    private async Task DownloadFlightPlanAsync()
    {
        if (IsDownloading) return;

        IsDownloading = true;
        DownloadStatus = "Loading";
        RouteStatus = "Downloading latest SimBrief flight plan…";

        try
        {
            var pilotId = SimBriefPilotId.Trim();
            await _settingsService.SaveAsync(new AppSettings(
                pilotId,
                SelectedTheme,
                SelectedAircraft,
                SelectedDriftInterval.Minutes));
            var route = await _routeProvider.GetLatestRouteAsync(pilotId);
            PopulateFlightPlan(route);
            ResyncSlots(0, 1);
            _lastObservedFromSlot = null;
            _lastObservedToSlot = null;
            _lastSlotRefresh = DateTimeOffset.MinValue;
            RouteStatus = $"Flight plan downloaded · {route.Count} waypoints";
            DownloadStatus = "Success";
            IsRouteLoaded = true;
        }
        catch (Exception exception) when (exception is ArgumentException
            or HttpRequestException
            or InvalidOperationException
            or JsonException
            or TaskCanceledException)
        {
            ResetSlots();
            RouteStatus = exception is TaskCanceledException
                ? "SimBrief request timed out."
                : exception.Message;
            DownloadStatus = "Failure";
            IsRouteLoaded = false;
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private void OpenSendDialog(FlightPlanLeg? leg)
    {
        if (leg is null) return;
        SelectedFlightPlanLeg = leg;
        SelectedInsSlot = 1;
        IsSendDialogOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmSendAsync()
    {
        if (SelectedFlightPlanLeg is null) return;

        if (!IsConnected)
        {
            RouteStatus = "Connect to MSFS before sending a waypoint.";
            return;
        }

        var routeIndex = FlightPlan.IndexOf(SelectedFlightPlanLeg);
        try
        {
            if (AutoManageWaypoints) await ResyncSlotsAsync(routeIndex, SelectedInsSlot);
            else
            {
                await _simulatorConnection.SendWaypointAsync(SelectedInsSlot, SelectedFlightPlanLeg.Waypoint);
                SetSlot(SelectedInsSlot, SelectedFlightPlanLeg.Waypoint, "Loaded");
            }

            RouteStatus = $"{SelectedFlightPlanLeg.Waypoint.Identifier} sent to INS slot {SelectedInsSlot}";
            IsSendDialogOpen = false;
        }
        catch (Exception exception)
        {
            RouteStatus = $"Waypoint send failed: {exception.Message}";
        }
    }

    [RelayCommand]
    private void CancelSend() => IsSendDialogOpen = false;

    [RelayCommand]
    private void OpenDirectDialog(FlightPlanLeg? leg)
    {
        if (leg is null) return;
        SelectedFlightPlanLeg = leg;
        IsDirectDialogOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmDirectAsync()
    {
        if (SelectedFlightPlanLeg is null) return;

        var loadedSlot = Slots.FirstOrDefault(slot => slot.Number > 0 && slot.Waypoint == SelectedFlightPlanLeg.Waypoint);
        var targetSlot = loadedSlot?.Number ?? (int.TryParse(ToSlot, out var currentTo) && currentTo is >= 1 and <= 9 ? currentTo : 1);
        var routeIndex = FlightPlan.IndexOf(SelectedFlightPlanLeg);

        if (!IsConnected)
        {
            RouteStatus = "Connect to MSFS before selecting direct-to.";
            return;
        }

        try
        {
            if (AutoManageWaypoints) await ResyncSlotsAsync(routeIndex, targetSlot);
            else
            {
                await _simulatorConnection.SendWaypointAsync(targetSlot, SelectedFlightPlanLeg.Waypoint);
                SetSlot(targetSlot, SelectedFlightPlanLeg.Waypoint, "Direct");
            }

            await _simulatorConnection.SetDirectToAsync(0, targetSlot);
            FromSlot = "0";
            ToSlot = targetSlot.ToString();
            Accuracy = "1";
            RouteStatus = $"Direct to {SelectedFlightPlanLeg.Waypoint.Identifier} via INS slot {targetSlot}";
            IsDirectDialogOpen = false;
        }
        catch (Exception exception)
        {
            RouteStatus = $"Direct-to failed: {exception.Message}";
        }
    }

    [RelayCommand]
    private void CancelDirect() => IsDirectDialogOpen = false;

    private void PopulateFlightPlan(IReadOnlyList<Waypoint> route)
    {
        FlightPlan.Clear();
        foreach (var leg in RouteCalculator.BuildLegs(route)) FlightPlan.Add(leg);
    }

    private void ResyncSlots(int routeIndex, int startingSlot)
    {
        for (var offset = 0; offset < 9; offset++)
        {
            var slotNumber = ((startingSlot - 1 + offset) % 9) + 1;
            var waypointIndex = routeIndex + offset;
            var waypoint = waypointIndex < FlightPlan.Count ? FlightPlan[waypointIndex].Waypoint : null;
            SetSlot(slotNumber, waypoint, waypoint is null ? "Empty" : "Synced");
        }
    }

    private async Task ResyncSlotsAsync(int routeIndex, int startingSlot)
    {
        for (var offset = 0; offset < 9; offset++)
        {
            var slotNumber = ((startingSlot - 1 + offset) % 9) + 1;
            var waypointIndex = routeIndex + offset;
            var waypoint = waypointIndex < FlightPlan.Count ? FlightPlan[waypointIndex].Waypoint : null;
            if (waypoint is not null) await _simulatorConnection.SendWaypointAsync(slotNumber, waypoint);
            SetSlot(slotNumber, waypoint, waypoint is null ? "Empty" : "Synced");
        }
    }

    private void SetSlot(int slotNumber, Waypoint? waypoint, string state)
    {
        Slots[slotNumber] = new InsSlot(slotNumber, waypoint, state);
        RefreshInsAssignments();
    }

    private void RefreshInsAssignments()
    {
        for (var index = 0; index < FlightPlan.Count; index++)
        {
            var leg = FlightPlan[index];
            var slots = Slots
                .Where(slot => slot.Number > 0 && ReferenceEquals(slot.Waypoint, leg.Waypoint))
                .Select(slot => slot.Number.ToString())
                .ToArray();
            var assignment = slots.Length == 0 ? "-" : string.Join(", ", slots);
            if (leg.InsSlot != assignment) FlightPlan[index] = leg with { InsSlot = assignment };
        }
    }

    private void ResetSlots()
    {
        FlightPlan.Clear();
        Slots.Clear();
        for (var slot = 0; slot < 10; slot++)
        {
            Slots.Add(new InsSlot(slot, null, slot == 0 ? "Position" : "Empty"));
        }
        FromSlot = "-";
        ToSlot = "-";
        Accuracy = "-";
    }
}

public sealed record DriftIntervalOption(string Label, int Minutes);
