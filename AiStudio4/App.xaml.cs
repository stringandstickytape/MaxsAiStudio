using System.Configuration;
using System.Data;
using System.Windows;

namespace AiStudio4;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        WindowManager.Instance.CreateNewWindow("main-" + DateTime.Now.Ticks);
    }
}