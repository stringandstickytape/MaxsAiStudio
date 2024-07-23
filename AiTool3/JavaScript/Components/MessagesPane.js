const { useState, useEffect } = React;

const MessagesPane = () => {
    const [messages, setMessages] = useState([]);

    const clearMessages = () => {
        setMessages([]);
    };

    const findMessageByGuid = (guid) => {
        return messages.find(message => message.guid === guid);
    };

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
        if (messages.length > 0) {
            setTimeout(() => {
                const mainContent = document.getElementsByClassName('main-content')[0];
                const messagesContainer = document.getElementById('messages-container');
            
                if (mainContent && messagesContainer) {
                    const lastMessage = messagesContainer.lastElementChild;
                    if (lastMessage) {
                        mainContent.scrollTop = lastMessage.offsetTop - mainContent.offsetTop;
                    }
                }
            }, 10); // 100ms delay
        }
    }, [messages]);

    return (
        <>
            <style>
                {`.messages-pane {
                    width: 100%;
                border: 1px solid #444;
                border-radius: 4px;
                overflow-y: auto;
                overflow-x: hidden;
                white-space: pre-wrap;
                word-wrap: break-word;
                background-color: #1a1a1a;
                padding: 10px;
                scroll-behavior: smooth;  
                    }
                .message {
                    margin-bottom: 10px;
                border-radius: 4px;
                padding: 10px;
                max-width: 80%;
                    border:1px solid #555;
                    margin-bottom:10px;
                    }
                .message-role {
                    font-weight: bold;
                    margin-bottom: 5px;
                    }
                .user-message {
                    background-color: #1e3a5f;
                align-self: flex-end;
                margin-left: auto;
                    }
                .ai-message {
                    background-color: #2a2a0a;
                align-self: flex-start;
                    }
                .root-message {
                    background-color: #1a1a2e;
                align-self: center;
                    }
                    .message-header {
    display: flex;
    align-items: center;
    margin-bottom: 5px;
}

.message-role {
    font-weight: bold;
    margin-right: 10px;
}

.message-actions {
    flex-grow: 1;
    height: 17px;
    background-color: #333;
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