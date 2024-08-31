using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses
{
    // extension methods for WebView2
    public static class WebView2Extensions
    {


        //public async Task InitializeAsync(this WebView2 )
        //{
        //    if (IsDesignMode())
        //        return;
        //
        //    await EnsureCoreWebView2Async(null);
        //    CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
        //
        //    foreach (var resource in GetResourceDetails())
        //    {
        //        CoreWebView2.AddWebResourceRequestedFilter(resource.Uri, CoreWebView2WebResourceContext.All);
        //    }
        //    NavigateToString(AssemblyHelper.GetEmbeddedResource("SharedClasses", "SharedClasses.HTML.ChatWebView2.html"));
        //
        //    string[] scriptResources = new[]
        //            {
        //        "SharedClasses.JavaScriptViewers.JsonViewer.js",
        //        "SharedClasses.JavaScriptViewers.ThemeEditor.js",
        //        "SharedClasses.JavaScriptViewers.SvgViewer.js",
        //        "SharedClasses.JavaScriptViewers.MermaidViewer.js",
        //        "SharedClasses.JavaScriptViewers.DotViewer.js",
        //        "SharedClasses.JavaScriptViewers.FindAndReplacer.js"
        //    };
        //
        //    foreach (var resource in scriptResources)
        //    {
        //        await ExecuteScriptAsync(AssemblyHelper.GetEmbeddedResource("SharedClasses", resource));
        //    }
        //}

    }

}
