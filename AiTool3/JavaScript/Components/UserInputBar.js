const UserInputBar = () => {
    const [sendDisabled, setSendDisabled] = React.useState(false);
    const [cancelDisabled, setCancelDisabled] = React.useState(false);
    const [newDisabled, setNewDisabled] = React.useState(false);
    const [inputContent, setInputContent] = React.useState('');

    const handleNew = () => {
        window.chrome.webview.postMessage({ type: 'new' });
        setInputContent("");
    };

    const handleNewWithContext = () => {
        window.chrome.webview.postMessage({ type: 'newWithContext' });
    };

    const handleNewWithPrompt = () => {
        window.chrome.webview.postMessage({ type: 'newWithPrompt' });
    };

    const handleSend = () => {
        const selectedTools = window.getSelectedTools ? window.getSelectedTools() : "";
        window.chrome.webview.postMessage({
            type: 'send',
            content: inputContent,
            selectedTools: selectedTools
        });
    };

    const handleCancel = () => {
        window.chrome.webview.postMessage({ type: 'cancel'});
    };

    const handleInputChange = (newContent) => {
        setInputContent(newContent);
    };

    const setUserPrompt = (string) => {
        setInputContent(string);
    };
    window.setUserPrompt = setUserPrompt;

    const getUserPrompt = () => {
        return inputContent;
    };
    window.getUserPrompt = getUserPrompt;

    const disableSendButton = () => setSendDisabled(true);
    const enableSendButton = () => setSendDisabled(false);
    const disableCancelButton = () => setCancelDisabled(true);
    const enableCancelButton = () => setCancelDisabled(false);
    const disableNewButton = () => setNewDisabled(true);
    const enableNewButton = () => setNewDisabled(false);

    // Expose these methods to the window object for external access
    React.useEffect(() => {
        window.disableSendButton = disableSendButton;
        window.enableSendButton = enableSendButton;
        window.disableCancelButton = disableCancelButton;
        window.enableCancelButton = enableCancelButton;
        window.disableNewButton = disableNewButton;
        window.enableNewButton = enableNewButton;

        return () => {
            delete window.disableSendButton;
            delete window.enableSendButton;
            delete window.disableCancelButton;
            delete window.enableCancelButton;
            delete window.disableNewButton;
            delete window.enableNewButton;
        };
    }, []);

    // svgString="<svg class='paperclip-icon' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'> <path d='M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48'></path></svg > "
    return (
        <>
            <style>
                {`
                    .user-input-bar {
                        display: flex;
                        align-items: stretch;
                        background-color: #1a1a1a;
                        color: white;
                        border-top: 1px solid #444;
                    }
                    .input-box-wrapper {
                        flex-grow: 1;
                        margin-right: 10px;
                    }
                    .buttons-wrapper {
                        display: flex;
                        flex-direction: column;
                        width: 100px;
                    }
                    .input-button {
                        margin-bottom: 5px;
                        padding: 8px;
                        border: none;
                        border-radius: 4px;
                        color: white;
                        cursor: pointer;
                        font-size: 14px;
                        transition: opacity 0.3s ease;
                    }
                    .input-button:disabled {
                        opacity: 0.5;
                        cursor: not-allowed;
                    }
                    .send-button { background-color: #4a7c4c; }
                    .cancel-button { background-color: #7c4a4a; }
                    .new-button { background-color: #4a617c; }

                    .split-button-container {
                        display: flex;
                        margin-bottom: 5px;
                    }
                    .split-button-main, .split-button-arrow {
                        background-color: #4a617c;
                        color: white;
                        border: none;
                        padding: 8px;
                        cursor: pointer;
                        transition: opacity 0.3s ease;
                    }
                    .split-button-main:disabled, .split-button-arrow:disabled {
                        opacity: 0.5;
                        cursor: not-allowed;
                    }
                    .split-button-main {
                        flex-grow: 1;
                        border-radius: 4px 0 0 4px;
                    }
                    .split-button-arrow {
                        border-radius: 0 4px 4px 0;
                    }
                    .split-button-dropdown {
                        position: absolute;
                        bottom: 20px;
                        right: 16px;
                        background-color: #2a2a2a;
                        min-width: 160px;
                        box-shadow: 0px 8px 16px 0px rgba(0,0,0,0.5);
                        z-index: 1;
                    }
                    .split-button-dropdown-item {
                        color: white;
                        padding: 12px 16px;
                        text-decoration: none;
                        display: block;
                        border: none;
                        width: 100%;
                        text-align: left;
                        background-color: transparent;
                        cursor: pointer;
                    }
                    .split-button-dropdown-item:hover {
                        background-color: #3a3a3a;
                    }
                `}
            </style>
            <div className="user-input-bar">
                <div className="input-box-wrapper">
                    <InputBox
                        onSend={handleSend}
                        value={inputContent}
                        onChange={handleInputChange}
                        placeholder="Enter text here..."
                        disabled={sendDisabled}
                    />
                </div>
                <div className="buttons-wrapper">
                    <SplitButton
                        label="Send"
                        onClick={handleSend}
                        disabled={sendDisabled}
                        color="#4a7c4c"
                    />
                    <SplitButton
                        label="Cancel"
                        onClick={handleCancel}
                        disabled={cancelDisabled}
                        color="#7c4a4a"
                    />
                    <SplitButton
                        label="New"
                        onClick={handleNew}
                        dropdownItems={[
                            { label: "New with context", onClick: handleNewWithContext },
                            { label: "New with prompt", onClick: handleNewWithPrompt }
                        ]}
                        disabled={newDisabled}
                    />
                    <SplitButton
                        label="Test"
                        onClick={handleNew}
                        disabled={newDisabled}

                    />
                </div>
            </div>
        </>
    );
};