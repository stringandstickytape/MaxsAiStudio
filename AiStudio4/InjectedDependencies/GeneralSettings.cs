// InjectedDependencies/GeneralSettings.cs
using AiStudio4.Core.Models;
using SharedClasses.Providers;
using System;
using System.Collections.Generic;

namespace AiStudio4.InjectedDependencies
{
    public class GeneralSettings
    {
        public List<Model> ModelList { get; set; } = new();
        public List<ServiceProvider> ServiceProviders { get; set; } = new();
        public float Temperature { get; set; } = 0.2f;
        public bool UseEmbeddings { get; set; } = false;
        public bool UsePromptCaching { get; set; } = true;
        public bool StreamResponses { get; set; } = false;
        public string EmbeddingsFilename { get; set; }
        public string EmbeddingModel { get; set; } = "mxbai-embed-large";
        public string DefaultSystemPromptId { get; set; }
        public string ProjectPath { get; set; } = "C:\\Users\\maxhe\\source\\repos\\CloneTest\\MaxsAiTool\\AiStudio4";
        public string YouTubeApiKey { get; set; }
        public string GitHubApiKey { get; set; }
        public string CondaPath { get; set; }

        // New properties using GUIDs for model identification
        public string DefaultModelGuid { get; set; } = string.Empty;
        public string SecondaryModelGuid { get; set; } = string.Empty;
        
        // Keep old properties for backward compatibility
        [Obsolete("Use DefaultModelGuid instead")]
        public string DefaultModel { get; set; } = string.Empty;
        
        [Obsolete("Use SecondaryModelGuid instead")]
        public string SecondaryModel { get; set; } = string.Empty;

        public ApiSettings ToApiSettings() => new()
        {
            Temperature = this.Temperature,
            UsePromptCaching = this.UsePromptCaching,
            StreamResponses = this.StreamResponses,
            EmbeddingModel = this.EmbeddingModel,
            EmbeddingsFilename = this.EmbeddingsFilename,
            UseEmbeddings = this.UseEmbeddings,
            DefaultSystemPromptId = this.DefaultSystemPromptId
        };
    }
}