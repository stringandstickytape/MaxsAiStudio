﻿namespace AiStudio4.DataModels
{
    public class LinearConvMessage
    {
        public string role { get; set; }
        public string content { get; set; }

        public string? base64type { get; set; }
        public string? base64image { get; set; }
    }
}