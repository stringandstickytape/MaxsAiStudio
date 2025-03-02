using System;
using System.Collections.Generic;

namespace SharedClasses.Providers
{
    /// <summary>
    /// Encapsulates API settings needed by AI services without creating a dependency on SettingsSet
    /// </summary>
    public class ApiSettings
    {
        public float Temperature { get; set; } = 0.9f;
        public bool UsePromptCaching { get; set; } = true;
        public bool StreamResponses { get; set; } = false;
        public string EmbeddingModel { get; set; } = "mxbai-embed-large";
        public string EmbeddingsFilename { get; set; } = string.Empty;
        public bool UseEmbeddings { get; set; } = false;
        public string DefaultSystemPromptId { get; set; }
    }
}