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
            {colorScheme.fontLink && (
                <link href={colorScheme.fontLink} rel="stylesheet" />
            )}
            <style>
                {`
                    html, body, #root {
                        height: 100%;
                        margin: 0;
                        padding: 0;
                        overflow: hidden;
                        font-family: ${colorScheme.fontFamily ? colorScheme.fontFamily : "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"};
                    }

                    button {
                        font-family: ${colorScheme.fontFamily ? colorScheme.fontFamily : "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"};
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
                        scroll-behavior: auto;
                        background-color: ${colorScheme.backgroundColor};
                        background: ${colorScheme.mainContentBackgroundCss};
                        background-image: ${colorScheme.mainContentBackgroundImage};
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