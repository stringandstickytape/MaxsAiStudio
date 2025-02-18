interface Window {
    chrome: {
        webview: {
            hostObjects: {
                windowManager: {
                    CreateNewWindow(name: string): Promise<void>;
                };
            };
        };
    };
}