import React from 'react'

function App() {

    const version: string = 'Hello!';
    const swName: string = 'Typescript';

    const openNewWindow = async () => {
        try {
            console.log(version);
            await window.chrome.webview.hostObjects.windowManager.CreateNewWindow('new-' + Date.now())
        } catch (e) {
            console.error('Error:', e)
        }
    }

    return (
        <div className="app">
            <h1>Holy sh*t, it's {swName}!</h1>
            <p>WebView2 initialization successful!</p>
            <button onClick={openNewWindow}>Open New Window</button>
        </div>
    )
}

export default App