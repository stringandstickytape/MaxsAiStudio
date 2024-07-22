const UserInputBar = () => {
    const handleSend = () => {
        const content = window.getInputContent();
        window.chrome.webview.postMessage({ type: 'send', content: content, selectedTools: "" });
        window.setInputContent(''); // Clear the input after sending
    };

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
                    <button className="input-button new-button" onClick={() => console.log("New clicked")}>
                        New
                    </button>
                </div>
            </div>
        </>
    );
};