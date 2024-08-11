using System.Diagnostics;
using System.Text.Json;

namespace AiTool3.Helpers
{
    public static class VersionHelper
    {
        public static async Task<(string, decimal)> GetLatestRelease()
        {
            string apiUrl = "https://api.github.com/repos/stringandstickytape/MaxsAiStudio/releases";
            string userAgent = "MyGitHubApp/1.0"; // Replace with your app name and version

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

                try
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    using (JsonDocument doc = JsonDocument.Parse(responseBody))
                    {
                        JsonElement root = doc.RootElement;

                        if (root.GetArrayLength() > 0)
                        {
                            JsonElement latestRelease = root[0];
                            string releaseName = latestRelease.GetProperty("name").GetString();
                            string releaseUrl = latestRelease.GetProperty("html_url").GetString();

                            if (decimal.TryParse(releaseName, out decimal releaseVersion))
                            {
                                return (releaseUrl, releaseVersion);
                            }
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    Debug.WriteLine($"Error: {e.Message}");
                }

                return ("", 0);
            }
        }

        internal static async Task CheckForUpdate(MenuStrip menuBar)
        {
            try
            {
                var latestVersionDetails = await VersionHelper.GetLatestRelease();
                if (latestVersionDetails.Item1 != "")
                {
                    var latestVersion = latestVersionDetails.Item2;
                    var latestVersionUrl = latestVersionDetails.Item1.ToString();

                    var currentVersion = MaxsAiStudio.Version;

                    ToolStripMenuItem updateMenu = null;
                    if (latestVersion > currentVersion)
                    {
                        updateMenu = MenuHelper.CreateMenu("Update Available");
                        updateMenu.BackColor = System.Drawing.Color.DarkRed;
                    }
                    else if (latestVersion < currentVersion)
                    {
                        updateMenu = MenuHelper.CreateMenu($"Pre-Release Version {currentVersion}");
                        updateMenu.BackColor = System.Drawing.Color.DarkSalmon;
                    }
                    else if (latestVersion == currentVersion)
                    {
                        updateMenu = MenuHelper.CreateMenu($"Version {currentVersion}");
                        updateMenu.BackColor = System.Drawing.Color.DarkGreen;
                    }

                    updateMenu.Click += (s, e) =>
                    {
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {latestVersionUrl.Replace("&", "^&")}") { CreateNoWindow = true });
                    };
                    menuBar.Items.Add(updateMenu);
                }
            }
            catch { }
        }
    }



}