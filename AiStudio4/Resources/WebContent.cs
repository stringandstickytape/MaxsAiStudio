namespace AiStudio4.Resources
{
    public static class WebContent
    {
        public const string TEST_HTML = @"
        <!DOCTYPE html>
        <html>
        <head>
            <title>AiStudio4 Test</title>
            <script>
                async function openNewWindow() {
                    try {
                        await window.chrome.webview.hostObjects.windowManager.CreateNewWindow('new-' + Date.now());
                    } catch(e) {
                        console.error('Error:', e);
                    }
                }
            </script>
            <style>
                body { 
                    background-color: #1e1e1e; 
                    color: white;
                    font-family: Arial, sans-serif;
                    margin: 0;
                    padding: 20px;
                }
            </style>
        </head>
        <body>
            <h1>AiStudio4 WebView2 Test</h1>
            <p>WebView2 initialization successful!</p>
            <button onclick='openNewWindow()'>Open New Window</button>
        </body>
        </html>";
    }
}