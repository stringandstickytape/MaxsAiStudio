using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AiStudio4.McpStandalone.ViewModels;
using AiStudio4.McpStandalone.Services;
using Wpf.Ui;

namespace AiStudio4.McpStandalone;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;
    private IAutoStartOAuthServerService? _oauthServerService;
    private ISimpleMcpServerService? _mcpServerService;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
            })
            .ConfigureServices((context, services) =>
            {
                // OAuth Server
                services.AddSingleton<IAutoStartOAuthServerService, AutoStartOAuthServerService>();
                
                // MCP Server
                services.AddSingleton<ISimpleMcpServerService, SimpleMcpServerService>();
                
                // UI Services
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ITaskBarService, TaskBarService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();
                services.AddSingleton<INavigationService, NavigationService>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        // Start OAuth server
        _oauthServerService = _host.Services.GetRequiredService<IAutoStartOAuthServerService>();
        try
        {
            await _oauthServerService.StartAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start OAuth server: {ex.Message}", "OAuth Server Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Start MCP server (after OAuth server is running)
        _mcpServerService = _host.Services.GetRequiredService<ISimpleMcpServerService>();
        try
        {
            await _mcpServerService.StartServerAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start MCP server: {ex.Message}", "MCP Server Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // Stop MCP server
        if (_mcpServerService != null)
        {
            await _mcpServerService.StopServerAsync();
        }

        // Stop OAuth server
        if (_oauthServerService != null)
        {
            await _oauthServerService.StopAsync();
            _oauthServerService.Dispose();
        }

        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}

