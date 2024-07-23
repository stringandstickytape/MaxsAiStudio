const UserInputBar = () => {
    const handleSend = () => {
        const content = window.getInputContent();
        window.chrome.webview.postMessage({ type: 'send', content: content, selectedTools: "" });
    };

    const handleNew = () => {
        window.chrome.webview.postMessage({ type: 'new' });
        window.setInputContent("");
    };

    const handleNewWithContext = () => {
        window.chrome.webview.postMessage({ type: 'newWithContext' });
    };

    const handleNewWithPrompt = () => {
        window.chrome.webview.postMessage({ type: 'newWithPrompt' });
        
    };

    const setUserPrompt = (string) => {
        window.setInputContent(string);
    };
    window.setUserPrompt = setUserPrompt;

    const getUserPrompt = () => {
        return window.getInputContent();
    };
    window.getUserPrompt = getUserPrompt;

    return (
        <>
            <style>
                {`
                    .user-input-bar {
                        display: flex;
                        align-items: stretch;
                        background-color: #f0f0f0;
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
                    }
                    .send-button { background-color: #4CAF50; }
                    .cancel-button { background-color: #f44336; }
                    .new-button { background-color: #008CBA; }

                    .split-button-container {
                        display: flex;
                        margin-bottom: 5px;
                    }
                    .split-button-main {
                        flex-grow: 1;
                        background-color: #008CBA;
                        color: white;
                        border: none;
                        padding: 8px;
                        cursor: pointer;
                        border-radius: 4px 0 0 4px;
                    }
                    .split-button-arrow {
                        background-color: #008CBA;
                        color: white;
                        border: none;
                        padding: 8px;
                        cursor: pointer;
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
                    <InputBox onSend={handleSend} />
                </div>
                <div className="buttons-wrapper">
                    <button className="input-button send-button" onClick={handleSend}>
                        Send
                    </button>
                    <button className="input-button cancel-button" onClick={() => console.log("Cancel clicked")}>
                        Cancel
                    </button>
                    <SplitButton
                        label="New"
                        onClick={handleNew}
                        dropdownItems={[
                            { label: "New with context", onClick: handleNewWithContext },
                            { label: "New with prompt", onClick: handleNewWithPrompt }
                        ]}
                    />
                </div>
            </div>
        </>
    );
};