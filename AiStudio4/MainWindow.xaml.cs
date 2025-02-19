using AiStudio4.InjectedDependencies;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AiStudio4;

public partial class WebViewWindow : Window
{
    private readonly WindowManager _windowManager;

    public WebViewWindow(WindowManager windowManager)
    {
        _windowManager = windowManager;
        InitializeComponent();
        webView.Initialize();
    }
}