using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Windows;
using System.IO;
using AiStudio4.Controls;

namespace AiStudio4
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;
        public ServiceProvider Services => _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Configure configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Register configuration
            services.AddSingleton<IConfiguration>(configuration);

            // Register services
            services.AddSingleton<SettingsManager>();
            services.AddSingleton<UiRequestBroker>();
            services.AddSingleton<WebServer>();
            services.AddSingleton<WindowManager>();
            services.AddTransient<WebViewWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var webViewWindow = _serviceProvider.GetRequiredService<WebViewWindow>();
            webViewWindow.Show();

            // Start web server
            var webServer = _serviceProvider.GetRequiredService<WebServer>();
            _ = webServer.StartAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            base.OnExit(e);
        }
    }
}