const { useState, useEffect } = React;

const MessagesPane = () => {
    const [messages, setMessages] = useState([]);

    const clearMessages = () => {
        setMessages([]);
    };

    const findMessageByGuid = (guid) => {
        return messages.find(message => message.guid === guid);
    };

    // Corrected function to add initial messages with capitalized property names
    const addInitialMessages = (messageList) => {
        setMessages(messageList.map(msg => ({
            role: msg.Role,
            content: msg.Content,
            guid: msg.Guid
        })));
    };

    const addMessage = (msg) => {
        setMessages(prevMessages => [...prevMessages, {
            role: msg.Role,
            content: msg.Content,
            guid: msg.Guid
        }]);
    };

    // Export methods
    window.ClearMessages = clearMessages;
    window.findMessageByGuid = findMessageByGuid;
    window.AddInitialMessages = addInitialMessages;
    window.AddMessage = addMessage;

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
                {`.messages-pane {
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
                    border:1px solid black;
                    margin-bottom:10px;
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
                    }`}
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