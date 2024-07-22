const { useState, useEffect } = React;

const MessagesPane = () => {
    const [messages, setMessages] = useState([]);

    const addMessage = (message) => {
        setMessages(prevMessages => [...prevMessages, message]);
    };

    const clearMessages = () => {
        setMessages([]);
    };

    // Export methods
    window.addMessage = addMessage;
    window.clearMessages = clearMessages;

    useEffect(() => {
        // Scroll to bottom when messages change
        const messagesContainer = document.getElementById('messages-container');
        if (messagesContainer) {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }
    }, [messages]);

    return (
        <>
            <style>
                {`
                    .messages-pane {
                        width: 100%;
                        height: 300px;
                        border: 1px solid #ccc;
                        border-radius: 4px;
                        overflow-y: auto;
                        padding: 10px;
                        background-color: #f9f9f9;
                    }
                    .message {
                        margin-bottom: 10px;
                        padding: 8px;
                        border-radius: 4px;
                        max-width: 80%;
                    }
                    .user-message {
                        background-color: #e1f5fe;
                        align-self: flex-end;
                        margin-left: auto;
                    }
                    .ai-message {
                        background-color: #f0f4c3;
                        align-self: flex-start;
                    }
                `}
            </style>

            <div id="messages-container" className="messages-pane">
                {messages.map((msg, index) => (
                    <div key={index} className={`message ${msg.type === 'user' ? 'user-message' : 'ai-message'}`}>
                        {msg.content}
                    </div>
                ))}
            </div>
        </>
    );
};