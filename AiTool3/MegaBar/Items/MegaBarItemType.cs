namespace AiTool3.MegaBar.Items
{
    /// megabars
    /// 

    public enum MegaBarItemType
    {
        [MegaBarItemInfo(Title = "Copy", SupportedTypes = new[] { ".cs", ".html", "*" })]
        Copy,

        [MegaBarItemInfo(Title = "Browser", SupportedTypes = new[] { ".html", "*" })]
        Browser,

        [MegaBarItemInfo(Title = "WebView", SupportedTypes = new[] { ".html", "*" })]
        WebView,

        [MegaBarItemInfo(Title = "C# Script", SupportedTypes = new[] { ".cs", "*" })]
        CSharpScript,

        [MegaBarItemInfo(Title = "Notepad", SupportedTypes = new[] { ".cs", ".html", "*" })]
        Notepad,

        [MegaBarItemInfo(Title = "Save As", SupportedTypes = new[] { ".cs", ".html", "*" })]
        SaveAs,

        [MegaBarItemInfo(Title = "Copy w/o comments", SupportedTypes = new[] { ".cs", ".html", "*" })]
        CopyWithoutComments,

        [MegaBarItemInfo(Title = "Launch in VS", SupportedTypes = new[] { "*.cs", ".ts", "*" })]
        LaunchInVisualStudio

    }

}
