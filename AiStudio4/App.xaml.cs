using System.Configuration;
using System.Data;
using System.Windows;

namespace AiStudio4;

public partial class App : Application
{
    private WebServer webServer;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            // Start the web server


            // Create the main window
            //WindowManager.Instance.CreateNewWindow("main-" + DateTime.Now.Ticks);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start application: {ex.Message}", "Startup Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (webServer != null)
            {
                await webServer.StopAsync();
            }
        }
        catch (Exception ex)
        {
            // Log the error if you have logging set up
            System.Diagnostics.Debug.WriteLine($"Error shutting down web server: {ex}");
        }
        finally
        {
            base.OnExit(e);
        }
    }
}