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
    [ObservableProperty] private string _routeStatus = "No flight plan downloaded";
    [ObservableProperty] private string _downloadStatus = "Idle";
    [ObservableProperty] private string _fromSlot = "—";
    [ObservableProperty] private string _toSlot = "—";
    [ObservableProperty] private string _accuracy = "—";
    [ObservableProperty] private string _simBriefPilotId = "";
    [ObservableProperty] private string _selectedTheme = "Dark";
    [ObservableProperty] private bool _autoManageWaypoints = true;
    [ObservableProperty] private bool _correctDrift = true;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isRouteLoaded;
    [ObservableProperty] private bool _isSettingsOpen;
    [ObservableProperty] private bool _isDownloading;

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
    public IReadOnlyList<string> Themes { get; } = ["Dark", "Light"];

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadAsync();
        SimBriefPilotId = settings.SimBriefPilotId;
        SelectedTheme = settings.Theme;
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
        await _settingsService.SaveAsync(new AppSettings(SimBriefPilotId.Trim(), SelectedTheme));
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        ConnectionStatus = "Connecting…";
        IsConnected = await _simulatorConnection.ConnectAsync();
        ConnectionStatus = IsConnected ? "Mock simulator connected" : "Connection failed";
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
            await _settingsService.SaveAsync(new AppSettings(pilotId, SelectedTheme));
            var route = await _routeProvider.GetLatestRouteAsync(pilotId);
            PopulateSlots(route);
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

    private void PopulateSlots(IReadOnlyList<Waypoint> route)
    {
        Slots.Clear();
        for (var slot = 0; slot < 10; slot++)
        {
            var waypoint = slot < 9 && slot < route.Count ? route[slot] : null;
            Slots.Add(new InsSlot(slot, waypoint, waypoint is null ? "Empty" : "Queued"));
        }

        FromSlot = "0";
        ToSlot = route.Count > 0 ? "1" : "—";
        Accuracy = "1";
    }

    private void ResetSlots()
    {
        Slots.Clear();
        for (var slot = 0; slot < 10; slot++) Slots.Add(new InsSlot(slot, null, "Empty"));
        FromSlot = "—";
        ToSlot = "—";
        Accuracy = "—";
    }
}
