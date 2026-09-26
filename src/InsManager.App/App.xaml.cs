using System.Windows;
using InsManager.App.ViewModels;
using InsManager.Core.Services;
using InsManager.Infrastructure;
using InsManager.SimConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InsManager.App;

public partial class App : Application
{
    private readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<IRouteProvider, MockRouteProvider>();
            services.AddSingleton<ISimulatorConnection, MockSimulatorConnection>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        })
        .Build();

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        _host.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
