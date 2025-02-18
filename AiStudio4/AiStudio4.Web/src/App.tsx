import React from 'react'

function App() {

    const version: string = 'Hello!';
    const swName: string = 'Typescript';
    const [testData, setTestData] = React.useState<any>(null);

    const fetchTestData = async () => {
        try {
            const response = await fetch('/api/test');
            const data = await response.json();
            setTestData(data);
            console.log('Test data:', data);
        } catch (e) {
            console.error('Error fetching test data:', e);
        }
    };

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
            <button onClick={fetchTestData}>Fetch Test Data</button>
            {testData && (
                <div>
                    <h3>Test Data Response:</h3>
                    <pre>{JSON.stringify(testData, null, 2)}</pre>
                </div>
            )}
        </div>
    )
}

export default App