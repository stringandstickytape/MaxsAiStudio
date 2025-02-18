import React from 'react'

function App() {
    const openNewWindow = async () => {
        try {
            await window.chrome.webview.hostObjects.windowManager.CreateNewWindow('new-' + Date.now())
        } catch (e) {
            console.error('Error:', e)
        }
    }

    return (
        <div className="app">
            <h1>AiStudio4 React App</h1>
            <p>WebView2 initialization successful!</p>
            <button onClick={openNewWindow}>Open New Window</button>
        </div>
    )
}

export default App