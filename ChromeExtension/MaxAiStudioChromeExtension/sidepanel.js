let ws = null;

function log(message, isOutgoing = false) {
    const output = document.getElementById(isOutgoing ? 'outgoing' : 'reply');
    const guidOutput = document.getElementById('conversation-guid');

    // Check if message contains a GUID pattern
    const guidMatch = message.match(/([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})\s*$/);

    if (guidMatch) {
        // Remove GUID from message and display separately
        output.textContent += message.replace(guidMatch[0], '').trim() + '\n';
        guidOutput.textContent = guidMatch[1];
    } else {
        output.textContent += message + '\n';
    }
}

async function sendMessage(ws, message) {
    ws.send(JSON.stringify(message));
}


async function addWebHtml() {
    try {
        console.log('Add web html clicked');

        // Get the active tab
        const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
        console.log('Found tab:', tab);

        // Execute script to get page content directly from the DOM
        const [{ result }] = await chrome.scripting.executeScript({
            target: { tabId: tab.id },
            func: () => {
                const selection = window.getSelection();
                if (selection && selection.rangeCount) {
                    const container = document.createElement('div');
                    for (let i = 0; i < selection.rangeCount; i++) {
                        container.appendChild(selection.getRangeAt(i).cloneContents());
                    }
                    return container.innerHTML
                } else {
                    return document.body.innerHTML
                }
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
            '\u0060\u0060\u0060html\n' + webContent + '\n\u0060\u0060\u0060';

    } catch (error) {
        console.error('Error in addWebHtml! :', error);
        log(`Error getting web content! : ${error.message}`);
    }
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
                const selection = window.getSelection();
                if (selection && selection.toString()) {
                    return selection.toString();
                }

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
        //log('WebSocket connection opened.');
        const guidOutput = document.getElementById('conversation-guid');
        const guid = guidOutput.textContent;

        let message;
        if (guid) {
            message = {
                Content: userPrompt,
                MessageType: "vsContinueCompletion",
                JsonObject: JSON.stringify(guid)
            };
        } else {
            message = {
                Content: userPrompt,
                MessageType: "vsRunCompletion"
                // for continue, set Guid, json-stringiifed
            };
        }

        await sendMessage(ws, message);
        log(`Sent prompt: ${userPrompt}`, true);

        // Clear the input after sending
        promptInput.value = '';
    };

    ws.onmessage = (event) => {
        try {
            const response = JSON.parse(event.data);
            const content = JSON.parse(response.Content);
            log(`\n\n${content.Content}\n`);
            document.getElementById('conversation-guid').textContent = content.Guid;
        } catch (error) {
            log(`Error parsing message: ${error.message}`);
        }
    };

    ws.onerror = (error) => {
        log(`WebSocket error: ${error.message}`);
    };

    ws.onclose = () => {
        //log('WebSocket connection closed.');
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
                const selection = window.getSelection();
                if (selection && selection.toString()) {
                    return selection.toString();
                }

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
    document.getElementById('addWebHtmlButton').addEventListener('click', addWebHtml);
    document.getElementById('newButton').addEventListener('click', newChat);
    document.getElementById('summarizeButton').addEventListener('click', summarizePage);

    // Also allow sending with Ctrl+Enter
    document.getElementById('promptInput').addEventListener('keydown', (event) => {
        if (event.ctrlKey && event.key === 'Enter') {
            connectWebSocket();
        }
    });
});

// Clean up WebSocket when popup closes
function newChat() {
    document.getElementById('promptInput').value = '';
    document.getElementById('outgoing').textContent = '';
    document.getElementById('reply').textContent = '';
    document.getElementById('conversation-guid').textContent = '';
}

window.addEventListener('unload', () => {
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.close();
    }
});