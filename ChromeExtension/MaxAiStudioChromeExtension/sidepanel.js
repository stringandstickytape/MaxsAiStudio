let ws = null;

function log(message) {
    const output = document.getElementById('output');
    output.textContent += message + '\n';
}

async function sendMessage(ws, message) {
    ws.send(JSON.stringify(message));
}

function connectWebSocket() {
    const promptInput = document.getElementById('promptInput');
    const userPrompt = promptInput.value.trim();

    if (!userPrompt) {
        log('Please enter a prompt first.');
        return;
    }

    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.close();
    }

    ws = new WebSocket('ws://localhost:35001/');

    ws.onopen = async () => {
        log('WebSocket connection opened.');

        const message = {
            Content: userPrompt,
            MessageType: "vsRunCompletion"
        };

        await sendMessage(ws, message);
        log(`Sent prompt: ${userPrompt}`);

        // Clear the input after sending
        promptInput.value = '';
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

// Also allow sending with Ctrl+Enter
document.getElementById('promptInput').addEventListener('keydown', (event) => {
    if (event.ctrlKey && event.key === 'Enter') {
        connectWebSocket();
    }
});

// Clean up WebSocket when popup closes
window.addEventListener('unload', () => {
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.close();
    }
});