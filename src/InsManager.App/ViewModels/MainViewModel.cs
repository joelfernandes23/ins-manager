using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InsManager.Core.Models;
using InsManager.Core.Services;

namespace InsManager.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRouteProvider _routeProvider;
    private readonly ISimulatorConnection _simulatorConnection;

    [ObservableProperty] private string _connectionStatus = "Simulator disconnected";
    [ObservableProperty] private string _routeStatus = "No route loaded";
    [ObservableProperty] private string _fromSlot = "—";
    [ObservableProperty] private string _toSlot = "—";
    [ObservableProperty] private string _accuracy = "—";
    [ObservableProperty] private bool _autoManageWaypoints = true;
    [ObservableProperty] private bool _correctDrift = true;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isRouteLoaded;

    public MainViewModel(IRouteProvider routeProvider, ISimulatorConnection simulatorConnection)
    {
        _routeProvider = routeProvider;
        _simulatorConnection = simulatorConnection;
        for (var slot = 0; slot < 10; slot++) Slots.Add(new InsSlot(slot, null, "Empty"));
    }

    public ObservableCollection<InsSlot> Slots { get; } = [];

    [RelayCommand]
    private async Task ConnectAsync()
    {
        ConnectionStatus = "Connecting…";
        IsConnected = await _simulatorConnection.ConnectAsync();
        ConnectionStatus = IsConnected ? "Mock simulator connected" : "Connection failed";
    }

    [RelayCommand]
    private async Task LoadRouteAsync()
    {
        var route = await _routeProvider.GetLatestRouteAsync();
        Slots.Clear();
        for (var slot = 0; slot < 10; slot++)
        {
            var waypoint = slot < 9 && slot < route.Count ? route[slot] : null;
            Slots.Add(new InsSlot(slot, waypoint, waypoint is null ? "Empty" : "Queued"));
        }

        FromSlot = "0";
        ToSlot = "1";
        Accuracy = "1";
        RouteStatus = $"Sample route loaded · {route.Count} waypoints";
        IsRouteLoaded = true;
    }
}
