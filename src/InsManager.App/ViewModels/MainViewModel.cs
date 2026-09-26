using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
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

    [ObservableProperty] private string _connectionStatus = "Simulator disconnected";
    [ObservableProperty] private string _connectionAction = "Connect";
    [ObservableProperty] private string _routeStatus = "No flight plan downloaded";
    [ObservableProperty] private string _downloadStatus = "Idle";
    [ObservableProperty] private string _fromSlot = "—";
    [ObservableProperty] private string _toSlot = "—";
    [ObservableProperty] private string _accuracy = "—";
    [ObservableProperty] private string _simBriefPilotId = "";
    [ObservableProperty] private string _selectedTheme = "Dark";
    [ObservableProperty] private string _selectedAircraft = "FSS Boeing 727";
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
        ResetSlots();
    }

    public ObservableCollection<InsSlot> Slots { get; } = [];
    public ObservableCollection<FlightPlanLeg> FlightPlan { get; } = [];
    public IReadOnlyList<string> Themes { get; } = ["Dark", "Light"];
    public IReadOnlyList<string> SupportedAircraft { get; } = ["FSS Boeing 727"];
    public IReadOnlyList<int> InsSlotNumbers { get; } = Enumerable.Range(1, 9).ToArray();

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadAsync();
        SimBriefPilotId = settings.SimBriefPilotId;
        SelectedTheme = settings.Theme;
        SelectedAircraft = SupportedAircraft.Contains(settings.Aircraft)
            ? settings.Aircraft
            : SupportedAircraft[0];
        _themeService.Apply(SelectedTheme);
    }

    partial void OnSelectedThemeChanged(string value) => _themeService.Apply(value);

    [RelayCommand]
    private void OpenSettings() => IsSettingsOpen = true;

    [RelayCommand]
    private void CloseSettings() => IsSettingsOpen = false;

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await _settingsService.SaveAsync(new AppSettings(SimBriefPilotId.Trim(), SelectedTheme, SelectedAircraft));
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private async Task ToggleConnectionAsync()
    {
        if (IsConnected)
        {
            ConnectionStatus = "Disconnecting…";
            await _simulatorConnection.DisconnectAsync();
            IsConnected = false;
            ConnectionAction = "Connect";
            ConnectionStatus = "Simulator disconnected";
            return;
        }

        ConnectionStatus = "Connecting…";
        IsConnected = await _simulatorConnection.ConnectAsync();
        ConnectionAction = IsConnected ? "Disconnect" : "Connect";
        ConnectionStatus = IsConnected
            ? $"MSFS 2024 connected · {_simulatorConnection.InputEventCount} controls"
            : _simulatorConnection.LastError ?? "Connection failed";
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
            await _settingsService.SaveAsync(new AppSettings(pilotId, SelectedTheme, SelectedAircraft));
            var route = await _routeProvider.GetLatestRouteAsync(pilotId);
            PopulateFlightPlan(route);
            ResyncSlots(0, 1);
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
    private void ConfirmSend()
    {
        if (SelectedFlightPlanLeg is null) return;

        var routeIndex = FlightPlan.IndexOf(SelectedFlightPlanLeg);
        if (AutoManageWaypoints) ResyncSlots(routeIndex, SelectedInsSlot);
        else SetSlot(SelectedInsSlot, SelectedFlightPlanLeg.Waypoint, "Loaded");

        RouteStatus = $"{SelectedFlightPlanLeg.Waypoint.Identifier} sent to INS slot {SelectedInsSlot}";
        IsSendDialogOpen = false;
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
    private void ConfirmDirect()
    {
        if (SelectedFlightPlanLeg is null) return;

        var loadedSlot = Slots.FirstOrDefault(slot => slot.Number > 0 && slot.Waypoint == SelectedFlightPlanLeg.Waypoint);
        var targetSlot = loadedSlot?.Number ?? (int.TryParse(ToSlot, out var currentTo) && currentTo is >= 1 and <= 9 ? currentTo : 1);
        var routeIndex = FlightPlan.IndexOf(SelectedFlightPlanLeg);

        if (AutoManageWaypoints) ResyncSlots(routeIndex, targetSlot);
        else SetSlot(targetSlot, SelectedFlightPlanLeg.Waypoint, "Direct");

        FromSlot = "0";
        ToSlot = targetSlot.ToString();
        Accuracy = "1";
        RouteStatus = $"Direct to {SelectedFlightPlanLeg.Waypoint.Identifier} via INS slot {targetSlot}";
        IsDirectDialogOpen = false;
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
            var assignment = slots.Length == 0 ? "—" : string.Join(", ", slots);
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
        FromSlot = "—";
        ToSlot = "—";
        Accuracy = "—";
    }
}
