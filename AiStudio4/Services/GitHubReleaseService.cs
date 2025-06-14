// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Services\GitHubReleaseService.cs





using System.Globalization;

using System.Net.Http;


namespace AiStudio4.Services
{
    public class GitHubReleaseService : IGitHubReleaseService
    {
        private readonly ILogger<GitHubReleaseService> _logger;
        private readonly HttpClient _httpClient;

        public GitHubReleaseService(ILogger<GitHubReleaseService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        decimal ParseReleaseNumberFromTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
                return 0m;

            // Extract the initial n.n pattern from the tagName
            int i = 0;
            while (i < tagName.Length && (char.IsDigit(tagName[i]) || tagName[i] == '.'))
            {
                i++;
            }

            string versionPart = tagName.Substring(0, i);

            // Try parse as decimal with invariant culture
            if (decimal.TryParse(versionPart, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
                return result;

            return 0m;
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync(string owner, string repo)
        {
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            _logger.LogInformation($"Fetching latest release from {apiUrl}");

            var result = new UpdateCheckResult
            {
                CheckSuccessful = false,
                IsUpdateAvailable = false
            };

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.UserAgent.ParseAdd("AiStudio4-ReleaseChecker/1.0");
                request.Headers.Accept.ParseAdd("application/vnd.github+json");

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    GitHubReleaseInfo releaseInfo = JsonConvert.DeserializeObject<GitHubReleaseInfo>(jsonResponse);

                    if (releaseInfo != null)
                    {
                        decimal latestReleaseNumber = ParseReleaseNumberFromTag(releaseInfo?.TagName);
                        bool isUpdateAvailable = latestReleaseNumber > App.VersionNumber;
                        
                        result.CheckSuccessful = true;
                        result.IsUpdateAvailable = isUpdateAvailable;
                        result.LatestVersion = releaseInfo.TagName;
                        result.ReleaseUrl = releaseInfo.HtmlUrl;
                        result.ReleaseName = releaseInfo.Name;
                        
                        _logger.LogInformation($"Parsed latest release number: {latestReleaseNumber} (Current: {App.VersionNumber}) => Update available: {isUpdateAvailable}");
                        _logger.LogDebug($"Successfully fetched latest release info:");
                        _logger.LogDebug($"  Tag: {releaseInfo.TagName}");
                        _logger.LogDebug($"  Name: {releaseInfo.Name}");
                        _logger.LogDebug($"  Published: {releaseInfo.PublishedAt}");
                        _logger.LogDebug($"  URL: {releaseInfo.HtmlUrl}");
                        string bodySnippet = releaseInfo.Body?.Substring(0, Math.Min(releaseInfo.Body.Length, 200)) + (releaseInfo.Body?.Length > 200 ? "..." : "");
                        _logger.LogDebug($"  Body (snippet): {bodySnippet}");

                        if (releaseInfo.Assets != null && releaseInfo.Assets.Any())
                        {
                            _logger.LogDebug("  Assets:");
                            foreach (var asset in releaseInfo.Assets)
                            {
                                _logger.LogDebug($"    - Name: {asset.Name}, URL: {asset.BrowserDownloadUrl}, Size: {asset.Size} bytes");
                            }
                        }
                        else
                        {
                            _logger.LogDebug("  No assets found for this release.");
                        }
                    }
                    else
                    {
                        result.ErrorMessage = "Failed to deserialize GitHub release information.";
                        _logger.LogWarning(result.ErrorMessage);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    result.ErrorMessage = $"Repository '{owner}/{repo}' or its latest release not found (404).";
                    _logger.LogWarning(result.ErrorMessage);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden || response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    result.ErrorMessage = $"GitHub API request failed due to restrictions (Status: {response.StatusCode}). This might be due to rate limiting.";
                    _logger.LogWarning(result.ErrorMessage);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    result.ErrorMessage = $"Failed to fetch GitHub release information. Status: {response.StatusCode}. Response: {errorContent}";
                    _logger.LogError(result.ErrorMessage);
                }
            }
            catch (HttpRequestException ex)
            {
                result.ErrorMessage = $"HTTP request failed while checking for GitHub release: {ex.Message}";
                _logger.LogError(ex, result.ErrorMessage);
            }
            catch (JsonException ex)
            {
                result.ErrorMessage = $"Failed to deserialize GitHub release JSON response: {ex.Message}";
                _logger.LogError(ex, result.ErrorMessage);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"An unexpected error occurred while checking for GitHub release: {ex.Message}";
                _logger.LogError(ex, result.ErrorMessage);
            }

            return result;
        }
    }
}
