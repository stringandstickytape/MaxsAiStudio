// InjectedDependencies/GeneralSettings.cs
using AiStudio4.Core.Models;
using SharedClasses.Providers;
using System;
using System.Collections.Generic;
using System.IO;

namespace AiStudio4.InjectedDependencies
{
    public class GeneralSettings
    {
        public List<Model> ModelList { get; set; } = new();
        public List<ServiceProvider> ServiceProviders { get; set; } = new();
        public float Temperature { get; set; } = 0.2f;
        public bool UseEmbeddings { get; set; } = false;
        public bool UsePromptCaching { get; set; } = true;

        public string EmbeddingsFilename { get; set; }
        public string EmbeddingModel { get; set; } = "mxbai-embed-large";
        public string DefaultSystemPromptId { get; set; }
        public string ProjectPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos", "AiStudio4TestProject");
        public string YouTubeApiKey { get; set; }
        public string GitHubApiKey { get; set; }
        public string AzureDevOpsPAT { get; set; }
        public string CondaPath { get; set; }
        public bool AllowConnectionsOutsideLocalhost { get; set; } = false;
        public bool UseExperimentalCostTracking { get; set; } = false; // Default to false (unchecked)

        // New properties using GUIDs for model identification
        public string DefaultModelGuid { get; set; } = string.Empty;
        public string SecondaryModelGuid { get; set; } = string.Empty;
        
        public int ConversationZipRetentionDays { get; set; } = 30;
        public int ConversationDeleteZippedRetentionDays { get; set; } = 90;

        // Keep old properties for backward compatibility
        [Obsolete("Use DefaultModelGuid instead")]
        public string DefaultModel { get; set; } = string.Empty;
        
        [Obsolete("Use SecondaryModelGuid instead")]
        public string SecondaryModel { get; set; } = string.Empty;

        public List<string> PackerIncludeFileTypes { get; set; } = new List<string>
        {

        };

        public List<string> PackerExcludeFilenames { get; set; } = new List<string>();

        public ApiSettings ToApiSettings() => new()
        {
            Temperature = this.Temperature,
            UsePromptCaching = this.UsePromptCaching,

            EmbeddingModel = this.EmbeddingModel,
            EmbeddingsFilename = this.EmbeddingsFilename,
            UseEmbeddings = this.UseEmbeddings,
            DefaultSystemPromptId = this.DefaultSystemPromptId
        };
    }
}