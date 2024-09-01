using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VSIXTest
{
    public class ResourceManager
    {
        private readonly Assembly _currentAssembly;

        public ResourceManager(Assembly assembly)
        {
            _currentAssembly = assembly;
        }

        public List<VsixResourceDetails> GetResourceDetails()
        {
            var resources = new List<VsixResourceDetails>();
            foreach (var resourceName in _currentAssembly.GetManifestResourceNames())
            {
                if (resourceName.StartsWith("VSIXTest.Html"))
                {
                    var penultimateDotIndex = resourceName.LastIndexOf(".", resourceName.LastIndexOf(".") - 1);
                    var filename = resourceName.Substring(penultimateDotIndex + 1);

                    resources.Add(new VsixResourceDetails
                    {
                        Uri = $"http://localhost/{filename}",
                        ResourceName = resourceName,
                        MimeType = "text/html"
                    });
                }
            }

            resources.AddRange(CreateResourceDetailsList());

            return resources;
        }

        public void ReturnResourceToWebView(CoreWebView2WebResourceRequestedEventArgs e, string resourceName, string mimeType, CoreWebView2 coreWebView2)
        {
            using (Stream stream = _currentAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        string content = reader.ReadToEnd();
                        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                        var response = coreWebView2.Environment.CreateWebResourceResponse(memoryStream, 200, "OK", $"Content-Type: {mimeType}");
                        e.Response = response;
                        e.Response.Headers.AppendHeader("Access-Control-Allow-Origin", "*");
                        return;
                    }
                }
                throw new Exception("Probably forgot to embed the resource :(");
            }
        }

        public List<VsixResourceDetails> CreateResourceDetailsList()
        {
            return new List<(string Uri, string ResourceName, string MimeType)>
        {
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
        }.Select(item => new VsixResourceDetails
        {
            Uri = item.Uri,
            ResourceName = $"AiTool3.ThirdPartyJavascript.{item.ResourceName}",
            MimeType = item.MimeType
        }).ToList();
        }

        public bool IsResourceRequested(string requestUri)
        {
            return GetResourceDetails().Any(x => requestUri.Equals(x.Uri, StringComparison.OrdinalIgnoreCase));
        }

        public VsixResourceDetails GetResourceDetailByUri(string uri)
        {
            return GetResourceDetails().FirstOrDefault(x => uri.Equals(x.Uri, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class VsixResourceDetails
    {
        public string Uri { get; set; }
        public string ResourceName { get; set; }
        public string MimeType { get; set; }
    }

}


