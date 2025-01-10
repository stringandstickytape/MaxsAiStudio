let ws = null;

function log(message) {
    const output = document.getElementById('output');
    output.textContent += message + '\n';
}

async function sendMessage(ws, message) {
    ws.send(JSON.stringify(message));
}

function connectWebSocket() {
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.close();
    }

    ws = new WebSocket('ws://localhost:35001/');

    ws.onopen = async () => {
        log('WebSocket connection opened.');

        // Send test message
        const testMessage = {
            Content: "Hello from WebSocket client!",
            MessageType: "vsRunCompletion"
        };

        await sendMessage(ws, testMessage);
        log(`Sent message: ${JSON.stringify(testMessage)}`);
    };

    ws.onmessage = (event) => {
        try {
            const response = JSON.parse(event.data);
            const content = JSON.parse(response.Content);
            log(`Received message: ${content.Content}\nConversation GUID: ${content.Guid}`);
        } catch (error) {
            log(`Error parsing message: ${error.message}`);
        }
    };

    ws.onerror = (error) => {
        log(`WebSocket error: ${error.message}`);
    };

    ws.onclose = () => {
        log('WebSocket connection closed.');
    };
}

document.getElementById('connectButton').addEventListener('click', connectWebSocket);

// Clean up WebSocket when popup closes
window.addEventListener('unload', () => {
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.close();
    }
});