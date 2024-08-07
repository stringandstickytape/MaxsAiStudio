using AiTool3;
using AiTool3.Snippets;
using AiTool3.Tools;
using AiTool3.UI;
using Microsoft.Extensions.DependencyInjection;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Configure the service collection
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        // Build the service provider
        var serviceProvider = serviceCollection.BuildServiceProvider();

        try
        {
            Application.Run(serviceProvider.GetRequiredService<MaxsAiStudio>());
        }
        catch (Exception ex)
        {
            // write very-full exception details to temp path
            var tempPath = Path.GetTempPath();
            var tempFile = Path.Combine(tempPath, "MaxsAiStudio-Error.txt");

            File.WriteAllText(tempFile, ex.ToString());
            File.AppendAllLines(tempFile, new[] { "----------------------", "Inner Exception", "----------------------", ex.InnerException?.ToString() });
            File.AppendAllLines(tempFile, new[] { "----------------------", "StackTrace", "----------------------", ex.StackTrace });
            File.AppendAllLines(tempFile, new[] { "----------------------", "Source", "----------------------", ex.Source });
            File.AppendAllLines(tempFile, new[] { "----------------------", "TargetSite", "----------------------", ex.TargetSite?.ToString() });
            File.AppendAllLines(tempFile, new[] { "----------------------", "Message", "----------------------", ex.Message });
            File.AppendAllLines(tempFile, new[] { "----------------------", "HelpLink", "----------------------", ex.HelpLink });
            File.AppendAllLines(tempFile, new[] { "----------------------", "HResult", "----------------------", ex.HResult.ToString() });

            MessageBox.Show($"Error has been written to {tempFile}", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ToolManager>();
        services.AddTransient<SnippetManager>();
        //services.AddTransient<FileAttachmentManager>();
        //services.AddTransient<ConversationManager>();
        //services.AddTransient<ModelUsageManager>();
        //services.AddScoped<SettingsSet>(); // If settings are context-specific
        //                                   // Add all necessary services here

        services.AddSingleton<MaxsAiStudio>();
        services.AddSingleton<ChatWebView>();
    }
}
