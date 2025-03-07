using System.Collections.Generic;

namespace AiStudio4.Core.Models
{
    /// <summary>
    /// Represents a pinned command that users can quickly access
    /// </summary>
    public class PinnedCommand
    {
        /// <summary>
        /// Unique identifier for the command
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name of the command
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional icon name for the command
        /// </summary>
        public string IconName { get; set; }

        /// <summary>
        /// Section or category this command belongs to
        /// </summary>
        public string Section { get; set; }
    }

    /// <summary>
    /// Request for getting pinned commands
    /// </summary>
    public class GetPinnedCommandsRequest
    {
        public string ClientId { get; set; }
    }

    /// <summary>
    /// Response for getting pinned commands
    /// </summary>
    public class GetPinnedCommandsResponse
    {
        public bool Success { get; set; }
        public List<PinnedCommand> PinnedCommands { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Request for saving pinned commands
    /// </summary>
    public class SavePinnedCommandsRequest
    {
        public string ClientId { get; set; }
        public List<PinnedCommand> PinnedCommands { get; set; }
    }

    /// <summary>
    /// Response for saving pinned commands
    /// </summary>
    public class SavePinnedCommandsResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}