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
        window.chrome.webview.postMessage({ type: 'send', content: inputContent, selectedTools: "" });
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

    return (
        <>
            <style>
                {`
                    .user-input-bar {
                        display: flex;
                        align-items: stretch;
                        background-color: #333;
                        color: white;
                        border-top: 1px solid #ccc;
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
                    .send-button { background-color: #4CAF50; }
                    .cancel-button { background-color: #f44336; }
                    .new-button { background-color: #008CBA; }

                    .split-button-container {
                        display: flex;
                        margin-bottom: 5px;
                    }
                    .split-button-main, .split-button-arrow {
                        background-color: #008CBA;
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
                        background-color: #f9f9f9;
                        min-width: 160px;
                        box-shadow: 0px 8px 16px 0px rgba(0,0,0,0.2);
                        z-index: 1;
                    }
                    .split-button-dropdown-item {
                        color: black;
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
                        background-color: #f1f1f1;
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
                    <button className="input-button send-button" onClick={handleSend} disabled={sendDisabled}>
                        Send
                    </button>
                    <button className="input-button cancel-button" onClick={() => console.log("Cancel clicked")} disabled={cancelDisabled}>
                        Cancel
                    </button>
                    <SplitButton
                        label="New"
                        onClick={handleNew}
                        dropdownItems={[
                            { label: "New with context", onClick: handleNewWithContext },
                            { label: "New with prompt", onClick: handleNewWithPrompt }
                        ]}
                        disabled={newDisabled}
                    />
                </div>
            </div>
        </>
    );
};