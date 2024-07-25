const { useColorScheme } = React;

function App() {
    const { colorScheme } = React.useColorScheme();
    useEffect(() => {
        // This effect runs after the component has mounted
        window.chrome.webview.postMessage({
            type: 'ready'
        });
    }, []); // Empty dependency array means this effect runs once after initial render
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