﻿using SharedClasses.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;


namespace SharedClasses.Helpers
{
    public static class AssemblyHelper
    {
        public static string GetEmbeddedResource(string resourceName)
        {
            return GetEmbeddedResource(Assembly.GetExecutingAssembly(), resourceName, false);
        }

        public static string GetEmbeddedResource(string assemblyName, string resourceName)
        {
            Assembly assembly = Assembly.Load(assemblyName);

            return GetEmbeddedResource(assembly, resourceName, false);
        }

        public static string GetEmbeddedResource(Assembly assembly, string resourceName)
        {
            try
            {
                // Get the resource stream
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        return null;

                    // Read the stream content
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions (e.g., assembly not found, resource not found)
                Debug.WriteLine($"Error loading resource: {ex.Message}");
                return null;
            }
        }

        private static string GetEmbeddedResource(Assembly assembly, string resourceName, bool ignoreErrors)
        {
            // Get the resource stream
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;

                // Read the stream content
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static List<ResourceDetails> GetResourceDetails(string rootUrl = "http://localhost/")
        {
            return GetResourceDetails(rootUrl, Assembly.GetExecutingAssembly(), "AiTool3.JavaScript.Components", "text/babel")
            .Concat(GetResourceDetails(rootUrl, Assembly.Load("SharedClasses"), "SharedClasses.JSX", "text/babel"))
            .Concat(CreateResourceDetailsList(rootUrl))
            .ToList();
        }

        private static List<ResourceDetails> CreateResourceDetailsList(string rootUrl)
        {
            //                await loadScript('https://cdn.jsdelivr.net/npm/marked/marked.min.js');
            // await loadStyle('https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/default.min.css');

            return new List<(string Uri, string ResourceName, string MimeType)>
            {
                ("https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/katex.min.js", "katex.min.js", "application/javascript"),
                ("https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/katex.min.css", "katex.min.css", "text/css"),
                ("https://cdn.jsdelivr.net/npm/marked/marked.min.js", "marked.min.js", "application/javascript"),
                ("https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/default.min.css", "default.min.css", "text/css"),
                ("https://cdn.jsdelivr.net/npm/mermaid@10.2.3/dist/mermaid.min.js", "mermaid.min.js", "application/javascript"),
                ("https://cdn.jsdelivr.net/npm/svg-pan-zoom@3.6.1/dist/svg-pan-zoom.min.js", "svg-pan-zoom.min.js", "application/javascript"),
                ("https://cdnjs.cloudflare.com/ajax/libs/jsoneditor/9.9.2/jsoneditor.min.js", "jsoneditor.min.js", "application/javascript"),
                ("https://cdnjs.cloudflare.com/ajax/libs/jsoneditor/9.9.2/jsoneditor.min.css", "jsoneditor.min.css", "text/css"),
                ("https://cdnjs.cloudflare.com/ajax/libs/jsoneditor/9.9.2/jsoneditor-icons.svg", "jsoneditor-icons.svg", "image/svg+xml"),
                ("https://cdnjs.cloudflare.com/ajax/libs/cytoscape/3.21.1/cytoscape.min.js", "cytoscape.min.js", "application/javascript"),
                ("https://cdnjs.cloudflare.com/ajax/libs/dagre/0.8.5/dagre.min.js", "dagre.min.js", "application/javascript"),
                ("https://unpkg.com/viz.js@2.1.2/viz.js", "viz.js", "application/javascript"),
                ("https://cdn.jsdelivr.net/npm/cytoscape-cxtmenu@3.4.0/cytoscape-cxtmenu.min.js", "cytoscape-cxtmenu.min.js", "application/javascript"),
                ("https://cdn.jsdelivr.net/npm/cytoscape-dagre@2.3.2/cytoscape-dagre.min.js", "cytoscape-dagre.min.js", "application/javascript")
            }.Select(item => new ResourceDetails
            {
                Uri = item.Uri,
                ResourceName = $"SharedClasses.ThirdPartyJavascript.{item.ResourceName}",
                MimeType = item.MimeType
            }).ToList();
        }

        private static List<ResourceDetails> GetResourceDetails(string rootUrl, Assembly assembly, string nameSpace, string mimeType)
        {
            var resources = new List<ResourceDetails>();
            foreach (var resourceName in assembly.GetManifestResourceNames().Where(x => x.StartsWith(nameSpace)))
            {
                {
                    // find the index of the penultimate dot in resource name
                    var penultimateDotIndex = resourceName.LastIndexOf(".", resourceName.LastIndexOf(".") - 1);
                    // get the filename using that
                    var filename = resourceName.Substring(penultimateDotIndex + 1);

                    resources.Add(new ResourceDetails
                    {
                        Uri = $"{rootUrl}{filename}",
                        ResourceName = resourceName,
                        MimeType = mimeType
                    });
                }
            }
            return resources;
        }
    }
}