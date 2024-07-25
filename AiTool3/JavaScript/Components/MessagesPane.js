// MessagesPane.js
const { useState, useEffect } = React;
const { useColorScheme } = React;

const MessagesPane = () => {
    const [messages, setMessages] = useState([]);
    const { colorScheme } = useColorScheme();

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
            const mainContent = document.getElementsByClassName('main-content')[0];
            const messagesContainer = document.getElementById('messages-container');

            if (mainContent && messagesContainer) {
                const lastMessage = messagesContainer.lastElementChild;
                if (lastMessage) {
                    mainContent.scrollTop = lastMessage.offsetTop - mainContent.offsetTop;
                }
            }
        }
    }, [messages]);

    const isPreviousAssistantMessageUnbalanced = (index) => {
        if (index === 0) return false;
        for (let i = index - 1; i >= 0; i--) {
            if (messages[i].role === 1) { // 1 represents AI/assistant role
                return isUnterminatedCodeBlock(messages[i].content);
            }
        }
        return false;
    };

    const isUnterminatedCodeBlock = (content) => {
        const threeBackticks = String.fromCharCode(96, 96, 96);
        const occurrences = (content.match(new RegExp(threeBackticks, 'g')) || []).length;
        return occurrences % 2 !== 0;
    };

    return (
        <>
            <style>
                {`.messages-pane {
                    width: calc(100% - 20px);
                    border: 1px solid ${colorScheme.textColor};
                    border-radius: 4px;
                    overflow-y: auto;
                    overflow-x: hidden;
                    white-space: pre-wrap;
                    word-wrap: break-word;
                    background-color: ${colorScheme.backgroundColor};
                    padding: 10px;
                    scroll-behavior: smooth;  
                    color: ${colorScheme.textColor};
                }
                .message {
                    margin-bottom: 10px;
                    border-radius: 4px;
                    padding: 10px;
                    max-width: 80%;
                    border: 1px solid ${colorScheme.textColor};
                    margin-bottom: 10px;
                }
                .message-role {
                    font-weight: bold;
                    margin-bottom: 5px;
                }
                .user-message {
                    background-color: ${colorScheme.messageUserBackgroundColor};
                    align-self: flex-end;
                    margin-left: auto;
                    color: ${colorScheme.messageUserTextColor};
                }
                .ai-message {
                    background-color: ${colorScheme.messageAIBackgroundColor};
                    align-self: flex-start;
                    color: ${colorScheme.messageAITextColor};
                }
                .root-message {
                    background-color: ${colorScheme.messageRootBackgroundColor};
                    align-self: center;
                    color: ${colorScheme.messageRootTextColor};
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
                    background-color: ${colorScheme.toolbarBackgroundColor};
                }`}
            </style>

            <div id="messages-container" className="messages-pane">
                {messages.map((message, index) => (
                    <Message
                        key={message.guid}
                        role={message.role}
                        content={message.content}
                        guid={message.guid}
                        previousAssistantUnbalanced={isPreviousAssistantMessageUnbalanced(index)}
                    />
                ))}
            </div>
        </>
    );
};