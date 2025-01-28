// MessagesPane.js
const { useState, useEffect } = React;
const { useColorScheme } = React;

const MessagesPane = () => {
    const [messages, setMessages] = useState([]);
    const { colorScheme } = useColorScheme();

    const clearMessages = () => {
        setMessages([]);
        // Check Live Scroll when messages are cleared
        window.setLiveScroll(true);
    };

    React.useEffect(() => {
        const mainContent = document.getElementsByClassName('main-content')[0];

        const handleWheel = (event) => {
            // Only uncheck Live Scroll if it's currently checked
            if (window.getLiveScroll()) {
                window.setLiveScroll(false);
            }
        };

        if (mainContent) {
            mainContent.addEventListener('wheel', handleWheel);
        }

        return () => {
            if (mainContent) {
                mainContent.removeEventListener('wheel', handleWheel);
            }
        };
    }, []);

    const findMessageByGuid = (guid) => {
        return messages.find(message => message.guid === guid);
    };

    const addInitialMessages = (messageList) => {
        setMessages(messageList.map(msg => ({
            role: msg.Role,
            content: msg.Content,
            guid: msg.Guid,
            base64Image: msg.Base64Image,
            base64Type: msg.Base64Type
        })));
    };

    const setMessageText = (guid, newContent, role = 0, base64image = null, base64type = null) => {
        setMessages(prevMessages => {
            const existingMessageIndex = prevMessages.findIndex(msg => msg.guid === guid);
            if (existingMessageIndex !== -1) {
                return prevMessages.map(msg =>
                    msg.guid === guid ? { ...msg, content: newContent } : msg
                );
            } else {
                return [...prevMessages, {
                    role: role,
                    content: newContent,
                    guid: guid,
                    base64Image: base64image,
                    base64Type: base64type                    
                }];
            }
        });
    };

    const appendMessageText = (guid, additionalContent, role = 0) => {
        setMessages(prevMessages => {
            const existingMessageIndex = prevMessages.findIndex(msg => msg.guid === guid);
            if (existingMessageIndex !== -1) {
                return prevMessages.map(msg =>
                    msg.guid === guid ? { ...msg, content: msg.content + additionalContent } : msg
                );
            } else {
                return [...prevMessages, {
                    role: role,
                    content: additionalContent,
                    guid: guid
                }];
            }
        });
    };

    const addMessage = (msg) => {
        // msg.Base64Image = 'iVBORw0KGgoAAAANSUhEUgAAAF0AAAB2CAYAAACqGp82AAA(and so on)'
        // msg.Base64Type = 'image/png' (for instance)

        setMessages(prevMessages => [...prevMessages, {
            role: msg.Role,
            content: msg.Content,
            guid: msg.Guid,
            base64Image: msg.Base64Image,
            base64Type: msg.Base64Type
        }]);
        window.removeMessageByGuid("temp-user-msg");
    };

    // New method to remove a message by its GUID
    const removeMessageByGuid = (guid) => {
        setMessages(prevMessages => prevMessages.filter(msg => msg.guid !== guid));
    };

    // Export methods
    window.ClearMessages = clearMessages;
    window.findMessageByGuid = findMessageByGuid;
    window.AddInitialMessages = addInitialMessages;
    window.AddMessage = addMessage;
    window.setMessageText = setMessageText;
    window.appendMessageText = appendMessageText; // Export the new method
    window.removeMessageByGuid = removeMessageByGuid;

    useEffect(() => {
        if (messages.length > 0 ) { // Add check for getLiveScroll
            const mainContent = document.getElementsByClassName('main-content')[0];
            const messagesContainer = document.getElementById('messages-container');

            const liveScroll = window.getLiveScroll();

            if (mainContent && messagesContainer) {
                const lastMessage = messages[messages.length - 1];
                if (lastMessage) {
                    if (lastMessage.guid === "temp-ai-msg" || lastMessage.guid === "temp-user-msg") {
                        if (liveScroll) {
                            mainContent.scrollTop = mainContent.scrollHeight;
                            console.log("Scroll3");
                        }
                    } else {
                        if (liveScroll) {
                            const lastMessageElement = messagesContainer.lastElementChild;
                            if (lastMessageElement) {
                                mainContent.scrollTop = lastMessageElement.offsetTop - mainContent.offsetTop;
                                console.log("Scroll4");
                            }
                        }
                    }
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
                {`
                .messages-pane {
                    width: calc(100% - 20px);
                    ${colorScheme?.messageBorder ? `border: ${colorScheme.messageBorder}` : ``};
                    border-radius: 4px;
                    overflow-y: auto;
                    overflow-x: hidden;
                    white-space: pre-wrap;
                    word-wrap: break-word;
                    background-color: ${colorScheme.backgroundColor};
                    background: ${colorScheme.messagesPaneBackgroundCss};
                    background-image: ${colorScheme.messagesPaneBackgroundImage};
                    ${colorScheme?.messagesPaneBackgroundFilter ? `background-filter: ${colorScheme.messagesPaneBackgroundFilter}` : ``};
                    padding: 10px;
                    scroll-behavior: auto;  
                    color: ${colorScheme.textColor};
                }
                .message {
                    margin-bottom: 10px;
                    border-radius: 4px;
                    padding: 10px;
                    max-width: 80%;
                    ${colorScheme?.messageBorder ? `border: ${colorScheme.messageBorder}` : ``};
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
                .message-footer {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    margin-top: 10px;
                    justify-content: end;
                }
                .top-button {
                    background-color: ${colorScheme.buttonBackgroundColor};
                    color: ${colorScheme.buttonTextColor};
                    border: none;
                    border-radius: 4px;
                    padding: 5px 10px;
                    cursor: pointer;
                    font-size: 12px;
                    transition: background-color 0.3s;
                }
                .top-button:hover {
                    background-color: ${colorScheme.buttonHoverBackgroundColor};
                }
                `}
            </style>

            <div id="messages-container" className="messages-pane">
                {messages.map((message, index) => (
                    <Message
                        key={message.guid}
                        role={message.role}
                        content={message.content}
                        guid={message.guid}
                        previousAssistantUnbalanced={isPreviousAssistantMessageUnbalanced(index)}
                        base64Image = { message.base64Image }
                        base64Type = { message.base64Type }   
                    />
                ))}
            </div>
        </>
    );
};