﻿const { useColorScheme, useEffect } = React;

function App() {
    const { colorScheme } = useColorScheme();
    const [contextMenu, setContextMenu] = React.useState(null);

    React.useEffect(() => {
        const handleContextMenu = (event) => {
            //if (!event.ctrlKey) {
            //    event.preventDefault();
            //    setContextMenu({
            //        x: event.clientX,
            //        y: event.clientY,
            //        items: [
            //            { label: 'Custom Option 1', onClick: () => console.log('Custom Option 1 clicked') },
            //            { label: 'Custom Option 2', onClick: () => console.log('Custom Option 2 clicked') },
            //            // Add more custom options as needed
            //        ],
            //    });
            //}
            // If CTRL is held, do nothing, allowing the default context menu to appear
        };

        document.addEventListener('contextmenu', handleContextMenu);

        return () => {
            document.removeEventListener('contextmenu', handleContextMenu);
        };
    }, []);

    useEffect(() => {
        window.chrome.webview.postMessage({
            type: 'ready'
        });
    }, []);

    return (
        <>
            {colorScheme && colorScheme.fontLink && (
                <link href={colorScheme.fontLink} rel="stylesheet" />
            )}
            <style>
                {`
                    html, body, #root {
                        height: 100%;
                        margin: 0;
                        padding: 0;
                        overflow: hidden;
                        font-family: ${colorScheme && colorScheme.fontFamily ? colorScheme.fontFamily : "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"};
                    }

                    button {
                        font-family: ${colorScheme && colorScheme.fontFamily ? colorScheme.fontFamily : "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"};
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
                        background-color: ${colorScheme ? colorScheme.backgroundColor : "black"};
                        background: ${colorScheme ? colorScheme.mainContentBackgroundCss : ""};
                        background-image: ${colorScheme ? colorScheme.mainContentBackgroundImage : ""};
                        color: ${colorScheme ? colorScheme.textColor : "white"};
                    }
                `}
            </style>
            <div className="app-container">
                <HeaderBar />

                <div className="main-content">
                    <MessagesPane />
                    <LiveStream />
                </div>
                <CheckboxBar />
                <QuickActionsBar />
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

/*             {contextMenu && (
                <CustomContextMenu
                    x={contextMenu.x}
                    y={contextMenu.y}
                    items={contextMenu.items}
                    onClose={() => setContextMenu(null)}
                />
            )} */
