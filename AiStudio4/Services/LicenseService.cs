




using System.Net.Http;



public class LicenseService : IDisposable
{
    private readonly HttpClient _httpClient;

    // Class to represent the structure of each NuGet package entry in the JSON
    private class NugetPackageInfo
    {
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }
        public string PackageProjectUrl { get; set; }
        public string Copyright { get; set; }
        public string Authors { get; set; }
        public string License { get; set; }
        public string LicenseUrl { get; set; }
        public string FetchedLicenseText { get; set; } // To store fetched license text
    }

    public LicenseService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AiStudio4-LicenseFetcher/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(20); // Timeout for fetching license text
    }

    /// <summary>
    /// Generates a formatted string containing all licenses: client-side licenses first,
    /// then processed NuGet licenses with full text fetched where possible.
    /// </summary>
    /// <param name="clientDistLicensesPath">Path to the AiStudioClient/dist/licenses.txt file.</param>
    /// <param name="appNugetLicensePath">Path to the app-nuget-license.txt file.</param>
    /// <param name="sharedClassesNugetLicensePath">Path to the sharedclasses-nuget-license.txt file.</param>
    /// <returns>A formatted string of all licenses.</returns>
    public async Task<string> GetFormattedAllLicensesAsync(
        string clientDistLicensesPath,
        string appNugetLicensePath,
        string sharedClassesNugetLicensePath)
    {
        var sb = new StringBuilder();

        // 1. Append client-side licenses (assumed to be pre-formatted)
        if (File.Exists(clientDistLicensesPath))
        {
            try
            {
                string clientLicensesContent = await File.ReadAllTextAsync(clientDistLicensesPath);
                sb.AppendLine(clientLicensesContent.TrimEnd()); // Trim trailing newlines to avoid too much space
                sb.AppendLine(); // Ensure separation before NuGet section
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error reading client licenses from {clientDistLicensesPath}: {ex.Message}");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine($"Warning: Client license file not found at {clientDistLicensesPath}");
            sb.AppendLine();
        }

        // 2. Process and append NuGet licenses
        sb.AppendLine("NUGET PACKAGES");
        sb.AppendLine("--------------");
        sb.AppendLine();

        var allNugetPackages = new List<NugetPackageInfo>();

        // Load from app-nuget-license.txt
        if (File.Exists(appNugetLicensePath))
        {
            try
            {
                string jsonContent = await File.ReadAllTextAsync(appNugetLicensePath);
                var appPackages = JsonConvert.DeserializeObject<List<NugetPackageInfo>>(jsonContent);
                if (appPackages != null) allNugetPackages.AddRange(appPackages);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error processing {Path.GetFileName(appNugetLicensePath)}: {ex.Message}");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine($"Warning: File not found at {appNugetLicensePath}");
            sb.AppendLine();
        }

        // Load from sharedclasses-nuget-license.txt
        if (File.Exists(sharedClassesNugetLicensePath))
        {
            try
            {
                string jsonContent = await File.ReadAllTextAsync(sharedClassesNugetLicensePath);
                var sharedPackages = JsonConvert.DeserializeObject<List<NugetPackageInfo>>(jsonContent);
                if (sharedPackages != null) allNugetPackages.AddRange(sharedPackages);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error processing {Path.GetFileName(sharedClassesNugetLicensePath)}: {ex.Message}");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine($"Warning: File not found at {sharedClassesNugetLicensePath}");
            sb.AppendLine();
        }

        // Deduplicate and sort
        var distinctPackages = allNugetPackages
            .GroupBy(p => new { p.PackageId, p.PackageVersion })
            .Select(g => g.First())
            .OrderBy(p => p.PackageId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.PackageVersion)
            .ToList();

        if (!distinctPackages.Any() && !File.Exists(appNugetLicensePath) && !File.Exists(sharedClassesNugetLicensePath))
        {
            // No NuGet files found and no packages loaded.
        }
        else if (!distinctPackages.Any())
        {
            sb.AppendLine("No NuGet packages found or processed.");
        }
        else
        {
            // Format each distinct package
            foreach (var package in distinctPackages)
            {
                // Fetch license text if URL is available
                if (!string.IsNullOrWhiteSpace(package.LicenseUrl))
                {
                    try
                    {
                        package.FetchedLicenseText = await FetchLicenseTextAsync(package.LicenseUrl);
                    }
                    catch (Exception ex)
                    {
                        package.FetchedLicenseText = $"(Error fetching full license text: {ex.Message})";
                        Console.WriteLine($"Error fetching license for {package.PackageId} from {package.LicenseUrl}: {ex.Message}");
                    }
                }
                FormatSingleNuGetPackage(sb, package);
            }
        }

        return sb.ToString().TrimEnd(); // Trim final newlines
    }

    private void FormatSingleNuGetPackage(StringBuilder sb, NugetPackageInfo package)
    {
        sb.AppendLine($"Name: {package.PackageId ?? "N/A"}");
        sb.AppendLine($"Version: {package.PackageVersion ?? "N/A"}");

        
            sb.AppendLine($"License: {(string.IsNullOrWhiteSpace(package.License) ? "null" : package.License)}");
        sb.AppendLine("Private: false"); // As per example
        // Description is not in NuGet JSON, so omitted.
        sb.AppendLine($"Repository: {package.PackageProjectUrl ?? "N/A"}");
        sb.AppendLine($"Author: {package.Authors ?? "N/A"}");

        bool hasPrintedContent = false;
        if (!string.IsNullOrWhiteSpace(package.Copyright))
        {
            sb.AppendLine("License Copyright:");
            sb.AppendLine("===");
            sb.AppendLine();
            sb.AppendLine(package.Copyright.Trim());
            sb.AppendLine();
            hasPrintedContent = true;
        }

        if (!string.IsNullOrWhiteSpace(package.FetchedLicenseText) && package.PackageId != "Microsoft.Web.WebView2")
        {
            if (hasPrintedContent) // If copyright was printed, add a small separator
            {
                // No separator if only full text is available, to match mermaid example style
            }
            sb.AppendLine("Full License Text:");
            sb.AppendLine("===");
            sb.AppendLine();
            sb.AppendLine(package.FetchedLicenseText.Trim());
            sb.AppendLine();
            hasPrintedContent = true;
        }
        else if (!string.IsNullOrWhiteSpace(package.LicenseUrl))
        {
            // LicenseUrl exists but FetchedLicenseText is empty or an error message
            // No explicit section for this if fetching failed, FetchLicenseTextAsync returns a message
        }

        if (!hasPrintedContent && string.IsNullOrWhiteSpace(package.LicenseUrl))
        {
            sb.AppendLine("(No specific license details or URL provided in metadata.)");
        }

        sb.AppendLine("---");
        sb.AppendLine(); // Two newlines for separation
    }

    private async Task<string> FetchLicenseTextAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
            || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            Console.WriteLine($"Invalid or non-HTTP(S) URL for license: {url}");
            return $"(Invalid URL: {url})";
        }

        try
        {
            var response = await _httpClient.GetAsync(uriResult);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return $"(Could not fetch license text. Status: {response.StatusCode})";
            }
        }
        catch (HttpRequestException ex)
        {
            return $"(Error fetching license: {ex.Message})";
        }
        catch (TaskCanceledException) // Catches timeouts
        {
            return "(Error fetching license: Request timed out.)";
        }
        catch (Exception ex) // Catch-all for other unexpected errors
        {
            return $"(Unexpected error fetching license: {ex.GetType().Name} - {ex.Message})";
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
