using System;
using System.Collections.Generic;

namespace AiTool3.DataModels
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

        // Create from SettingsSet
        public static ApiSettings FromSettingsSet(SettingsSet settingsSet)
        {
            return new ApiSettings
            {
                Temperature = settingsSet.Temperature,
                UsePromptCaching = settingsSet.UsePromptCaching,
                StreamResponses = settingsSet.StreamResponses,
                EmbeddingModel = settingsSet.EmbeddingModel,
                EmbeddingsFilename = settingsSet.EmbeddingsFilename,
                UseEmbeddings = settingsSet.UseEmbeddings
            };
        }
    }
}