const { useState, useEffect } = React;

const MessagesPane = () => {
    const [messages, setMessages] = useState([]);

    const addMessage = (role, content, guid) => {
        setMessages(prevMessages => [...prevMessages, { role, content, guid }]);
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
                        border: 1px solid #ccc;
                        border-radius: 4px;
                        overflow-y: auto;
                        overflow-x: hidden;
                        white-space: pre-wrap;
                        word-wrap: break-word;
                        background-color: #f9f9f9;
                        padding: 10px;
                    }
                    .message {
                        margin-bottom: 10px;
                        border-radius: 4px;
                        padding: 10px;
                        max-width: 80%;
                    }
                    .message-role {
                        font-weight: bold;
                        margin-bottom: 5px;
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
                    .root-message {
                        background-color: #e8eaf6;
                        align-self: center;
                        margin: 0 auto;
                    }
                `}
            </style>

            <div id="messages-container" className="messages-pane">
                {messages.map(message => (
                    <Message
                        key={message.guid}
                        role={message.role}
                        content={message.content}
                        guid={message.guid}
                    />
                ))}
            </div>
        </>
    );
};