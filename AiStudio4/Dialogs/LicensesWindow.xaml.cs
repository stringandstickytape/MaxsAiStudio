// AiStudio4/Dialogs/LicensesWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiStudio4.Dialogs
{
    public partial class LicensesWindow : Window
    {
        public LicensesWindow(string licensesJsonPath, string nugetLicense1Path, string nugetLicense2Path)
        {
            InitializeComponent();
            LoadLicenseData(licensesJsonPath, nugetLicense1Path, nugetLicense2Path);
        }

        private void LoadLicenseData(string licensesJsonPath, string nugetLicense1Path, string nugetLicense2Path)
        {
            StringBuilder sb = new StringBuilder();

            // Add header
            sb.AppendLine("THIRD-PARTY SOFTWARE LICENSES");
            sb.AppendLine("=============================");
            sb.AppendLine();

            // Process NPM packages from licenses.json
            if (File.Exists(licensesJsonPath))
            {
                sb.AppendLine("NPM PACKAGES");
                sb.AppendLine("-----------");
                sb.AppendLine();

                try
                {
                    string jsonContent = File.ReadAllText(licensesJsonPath);
                    var licensesData = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(jsonContent);

                    foreach (var package in licensesData)
                    {
                        string packageName = package.Key;
                        string license = package.Value["licenses"]?.ToString() ?? "Unknown";
                        string repository = package.Value["repository"]?.ToString() ?? "Not specified";

                        sb.AppendLine($"Package: {packageName}");
                        sb.AppendLine($"License: {license}");
                        sb.AppendLine($"Repository: {repository}");
                        sb.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Error parsing NPM licenses: {ex.Message}");
                    sb.AppendLine();
                }
            }

            // Process NuGet packages from nuget-license1.txt and nuget-license2.txt
            ProcessNuGetLicenses(sb, nugetLicense1Path, "NUGET PACKAGES (PART 1)");
            ProcessNuGetLicenses(sb, nugetLicense2Path, "NUGET PACKAGES (PART 2)");

            // Set the text to the TextBox
            LicensesTextBox.Text = sb.ToString();
        }

        private void ProcessNuGetLicenses(StringBuilder sb, string filePath, string header)
        {
            if (File.Exists(filePath))
            {
                sb.AppendLine(header);
                sb.AppendLine("-----------------");
                sb.AppendLine();

                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var licensesData = JsonConvert.DeserializeObject<List<JObject>>(jsonContent);

                    foreach (var package in licensesData)
                    {
                        string packageId = package["PackageId"]?.ToString() ?? "Unknown";
                        string license = package["License"]?.ToString() ?? "Unknown";
                        string projectUrl = package["PackageProjectUrl"]?.ToString() ?? "Not specified";

                        sb.AppendLine($"Package: {packageId}");
                        sb.AppendLine($"License: {license}");
                        sb.AppendLine($"Project URL: {projectUrl}");
                        sb.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Error parsing NuGet licenses: {ex.Message}");
                    sb.AppendLine();
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}