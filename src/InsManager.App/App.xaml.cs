using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using InsManager.App.Services;
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
            services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
            services.AddSingleton<IRouteProvider, SimBriefRouteProvider>();
            services.AddSingleton<ISettingsService, JsonSettingsService>();
            services.AddSingleton<ISimulatorConnection, SimConnectConnection>();
            services.AddSingleton<ThemeService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        })
        .Build();

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            await _host.StartAsync();
            await _host.Services.GetRequiredService<MainViewModel>().InitializeAsync();
            MainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow.Show();
            base.OnStartup(e);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "INS Manager could not start",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        try
        {
            _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            if (_host is IAsyncDisposable asyncHost)
                asyncHost.DisposeAsync().AsTask().GetAwaiter().GetResult();
            else
                _host.Dispose();
        }
        finally
        {
            base.OnExit(e);
        }
    }

    private void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs eventArgs)
    {
        eventArgs.Handled = true;
        MessageBox.Show(
            eventArgs.Exception.Message,
            "INS Manager encountered an error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Shutdown(1);
    }
}
