let ws = null;

function log(message) {
    const output = document.getElementById('output');
    output.textContent += message + '\n';
}

async function sendMessage(ws, message) {
    ws.send(JSON.stringify(message));
}

async function addWebContent() {
    try {
        console.log('Add web content clicked');

        // Get the active tab
        const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
        console.log('Found tab:', tab);

        // Execute script to get page content directly from the DOM
        const [{ result }] = await chrome.scripting.executeScript({
            target: { tabId: tab.id },
            func: () => {
                // Get visible text content, excluding scripts and styles
                const getVisibleText = (node) => {
                    if (node.nodeType === Node.TEXT_NODE) {
                        return node.textContent.trim();
                    }

                    if (node.nodeType !== Node.ELEMENT_NODE) {
                        return '';
                    }

                    const style = window.getComputedStyle(node);
                    if (style.display === 'none' || style.visibility === 'hidden') {
                        return '';
                    }

                    if (node.tagName === 'SCRIPT' || node.tagName === 'STYLE' ||
                        node.tagName === 'NOSCRIPT' || node.tagName === 'TEMPLATE') {
                        return '';
                    }

                    let text = '';
                    for (let child of node.childNodes) {
                        text += getVisibleText(child) + ' ';
                    }
                    return text.trim();
                };

                return getVisibleText(document.body);
            }
        });
        console.log('Got result:', result);

        // Get the current prompt text
        const promptInput = document.getElementById('promptInput');
        const currentText = promptInput.value;

        // Add the web content as a markdown code block
        const webContent = result.split('\n')
            .filter(line => line.trim()) // Remove empty lines
            .join('\n');

        promptInput.value = currentText +
            (currentText ? '\n\n' : '') +
            `URL: ${tab.url}\n\n` +
            '\u0060\u0060\u0060\n' + webContent + '\n\u0060\u0060\u0060';

    } catch (error) {
        console.error('Error in addWebContent! :', error);
        log(`Error getting web content! : ${error.message}`);
    }
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

async function summarizePage() {
    try {
        console.log('Summarize page clicked');

        // Get the active tab
        const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
        console.log('Found tab:', tab);

        // Execute script to get page content directly from the DOM
        const [{ result }] = await chrome.scripting.executeScript({
            target: { tabId: tab.id },
            func: () => {
                // Get visible text content, excluding scripts and styles
                const getVisibleText = (node) => {
                    if (node.nodeType === Node.TEXT_NODE) {
                        return node.textContent.trim();
                    }

                    if (node.nodeType !== Node.ELEMENT_NODE) {
                        return '';
                    }

                    const style = window.getComputedStyle(node);
                    if (style.display === 'none' || style.visibility === 'hidden') {
                        return '';
                    }

                    if (node.tagName === 'SCRIPT' || node.tagName === 'STYLE' ||
                        node.tagName === 'NOSCRIPT' || node.tagName === 'TEMPLATE') {
                        return '';
                    }

                    let text = '';
                    for (let child of node.childNodes) {
                        text += getVisibleText(child) + ' ';
                    }
                    return text.trim();
                };

                return getVisibleText(document.body);
            }
        });

        const webContent = result.split('\n')
            .filter(line => line.trim()) // Remove empty lines
            .join('\n');

        // Create the summarization prompt
        const summarizationPrompt = 
            'Please provide a clear, concise bullet-point summary of the following webpage content. ' +
            'Focus on the main points, key information, and important details. ' +
            'Organize the summary in a logical structure and avoid any unnecessary text or meta-commentary.\n\n' +
            `URL: ${tab.url}\n\n` +
            '\u0060\u0060\u0060\n' + webContent + '\n\u0060\u0060\u0060';

        // Set the prompt in the input field
        const promptInput = document.getElementById('promptInput');
        promptInput.value = summarizationPrompt;

        // Automatically send the prompt
        connectWebSocket();

    } catch (error) {
        console.error('Error in summarizePage:', error);
        log(`Error summarizing page: ${error.message}`);
    }
}

// Wait for DOM to be ready
document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('connectButton').addEventListener('click', connectWebSocket);
    document.getElementById('addWebContentButton').addEventListener('click', addWebContent);
    document.getElementById('summarizeButton').addEventListener('click', summarizePage);

    // Also allow sending with Ctrl+Enter
    document.getElementById('promptInput').addEventListener('keydown', (event) => {
        if (event.ctrlKey && event.key === 'Enter') {
            connectWebSocket();
        }
    });
});

// Clean up WebSocket when popup closes
window.addEventListener('unload', () => {
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.close();
    }
});