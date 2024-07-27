const { useColorScheme, useEffect } = React;

function App() {
    const { colorScheme } = useColorScheme();

    useEffect(() => {
        window.chrome.webview.postMessage({
            type: 'ready'
        });
    }, []);

    return (
        <>
            <style>
                {`
                    html, body, #root {
                        height: 100%;
                        margin: 0;
                        padding: 0;
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    }
                    .app-container {
                        display: flex;
                        flex-direction: column;
                        height: 100vh;
                    }
                    .main-content {
                        flex-grow: 1;
                        overflow-y: scroll;
                        padding: 20px;
                        overflow-x: hidden;
                        scroll-behavior: smooth;
                        background-color: ${colorScheme.backgroundColor};
                        background: ${colorScheme.mainContentBackgroundCss};
                        color: ${colorScheme.textColor};
                    }
                `}
            </style>
            <div className="app-container">
                <HeaderBar />
                <div className="main-content">
                    <MessagesPane />
                    <LiveStream />
                </div>
                <UserInputBar />
                <ScratchPad />
            </div>
        </>
    );
}

ReactDOM.render(
    <React.StrictMode>
        <ColorSchemeProvider>
            <App />
        </ColorSchemeProvider>
    </React.StrictMode>,
    document.getElementById('root')
);