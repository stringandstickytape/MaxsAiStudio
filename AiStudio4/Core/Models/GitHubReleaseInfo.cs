//C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Core\Models\GitHubReleaseInfo.cs




namespace AiStudio4.Core.Models
{
    public class GitHubReleaseInfo
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("published_at")]
        public DateTimeOffset PublishedAt { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("assets_url")]
        public string AssetsUrl { get; set; }

        [JsonProperty("upload_url")]
        public string UploadUrl { get; set; }

        [JsonProperty("tarball_url")]
        public string TarballUrl { get; set; }

        [JsonProperty("zipball_url")]
        public string ZipballUrl { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("node_id")]
        public string NodeId { get; set; }

        [JsonProperty("author")]
        public GitHubAuthorInfo Author { get; set; }

        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("draft")]
        public bool Draft { get; set; }

        [JsonProperty("assets")]
        public List<GitHubReleaseAsset> Assets { get; set; }
    }

    public class GitHubAuthorInfo
    {
        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }
    }

    public class GitHubReleaseAsset
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("download_count")]
        public int DownloadCount { get; set; }

        [JsonProperty("content_type")]
        public string ContentType { get; set; }
    }
}
