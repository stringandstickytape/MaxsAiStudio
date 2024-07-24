const { useState } = React;

const HeaderBar = () => {
    const [logoText, setLogoText] = useState("AI Studio");
    const [systemPrompt, setSystemPrompt] = useState("");
    const [mainAIOptions, setMainAIOptions] = useState([]);
    const [summaryAIOptions, setSummaryAIOptions] = useState([]);
    const [mainAIValue, setMainAIValue] = useState(mainAIOptions[0]);
    const [summaryAIValue, setSummaryAIValue] = useState(summaryAIOptions[0]);

    // Export methods
    window.setDropdownOptions = (dropdownName, options) => {
        if (dropdownName === 'mainAI') {
            setMainAIOptions(options);
        } else if (dropdownName === 'summaryAI') {
            setSummaryAIOptions(options);
        }
    };

    window.getDropdownValue = (dropdownName) => {
        if (dropdownName === 'mainAI') {
            return mainAIValue;
        } else if (dropdownName === 'summaryAI') {
            return summaryAIValue;
        }
    };

    window.setDropdownValue = (dropdownName, value) => {
        if (dropdownName === 'mainAI') {
            setMainAIValue(value);
        } else if (dropdownName === 'summaryAI') {
            setSummaryAIValue(value);
        }
    };

    const changeLogo = (newText) => {
        setLogoText(newText);
    };

    const handleAttach = () => { window.chrome.webview.postMessage({ type: 'attach' }); };
    const handleVoice = () => { window.chrome.webview.postMessage({ type: 'voice' }); };
    const handleProject = () => { window.chrome.webview.postMessage({ type: 'project' }); };

    const handleSystemPromptChange = (newPrompt) => {
        setSystemPrompt(newPrompt);
    };

    // export changeLogo and getSystemPrompt
    window.changeLogo = changeLogo;
    window.getSystemPrompt = () => systemPrompt;
    window.updateSystemPrompt = setSystemPrompt;

    return (
        <>
            <style>
                {`
                    .header-bar {
                        display: flex;
                        flex-direction: column;
                        background-color: #333;
                        color: white;
                    }
                    .main-header {
                        display: flex;
                        justify-content: space-between;
                        padding: 10px 20px;
                    }
                    .left-section {
                        display: flex;
                        flex-direction: column;
                    }
                    .logo {
                        font-size: 24px;
                        font-weight: bold;
                        margin-bottom: 10px;
                    }
                    .system-prompt-container {
                        flex-grow: 1;
                        margin-left: 20px;
                    }
                    .system-prompt-input {
                        background-color: #555;
                        color: white;
                        border: none;
                        border-radius: 4px;
                        width: 100%;
                    }
                    .system-prompt-input::placeholder {
                        color: #aaa;
                    }
                    .options-bar {
                        display: flex;
                        justify-content: space-around;
                        background-color: #444;
                        padding: 5px 0;
                    }
                    .option {
                        cursor: pointer;
                        padding: 5px 10px;
                    }
                    .option:hover {
                        background-color: #555;
                    }
                                        .dropdown-container {
                        display: flex;
                        align-items: center;
                        margin-right: 20px;
                    }
                    .dropdown-container label {
                        margin-right: 10px;
                        color: white;
                    }
                    .dropdown-container select {
                        background-color: #555;
                        color: white;
                        border: none;
                        border-radius: 4px;
                        padding: 5px;
                    }
                `}
            </style>

            <div className="header-bar">
                <div className="main-header">
                    <div className="left-section">
                        <div className="logo">{logoText}</div>
                        <ToolsDropdown />
                        <div height="24px" width="24px">
                        <SplitButton
                            color="#777"
                            label="Attach"
                            onClick={handleAttach}
                            //disabled={newDisabled}
                            svgString="<svg class='paperclip-icon' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'> <path d='M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48'></path></svg > "
                            />
                            <SplitButton
                                color="#777"
                                label="Voice"
                                onClick={handleVoice}
                                //disabled={newDisabled}
                                svgString='<svg viewBox="0 0 150 150" xmlns="http://www.w3.org/2000/svg"><rect x="15" y="67" width="10" height="16" rx="5" fill="white" /><rect x="35" y="50" width="10" height="50" rx="5" fill="white" /><rect x="55" y="27" width="10" height="96" rx="5" fill="white" /><rect x="75" y="43" width="10" height="64" rx="5" fill="white" /><rect x="95" y="63" width="10" height="24" rx="5" fill="white" /><rect x="115" y="57" width="10" height="36" rx="5" fill="white" /><rect x="135" y="67" width="10" height="16" rx="5" fill="white" /></svg> '
                            />
                            <SplitButton
                                color="#777"
                                label="Project"
                                onClick={handleProject}
                                //disabled={newDisabled}
                                svgString='<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg"><rect x="10" y="10" width="80" height="80" fill="transparent" stroke="white" stroke-width="2"></rect><circle cx="25" cy="25" r="5" fill="white"></circle><rect x="35" y="20" width="40" height="10" fill="white"></rect><circle cx="25" cy="50" r="5" fill="white"></circle><rect x="35" y="45" width="40" height="10" fill="white"></rect><circle cx="25" cy="75" r="5" fill="white"></circle><rect x="35" y="70" width="40" height="10" fill="white"></rect></svg>'
                            />
                        </div>
                    </div>
                    <div className="system-prompt-container">
                        <InputBox
                            onSend={() => { }}
                            value={systemPrompt}
                            onChange={handleSystemPromptChange}
                            placeholderText="Enter system prompt..."
                            className="system-prompt-input"
                        />
                    </div>
                </div>
                <div className="options-bar">
                    <DropDown
                        id="mainAI"
                        label="Main AI"
                        options={mainAIOptions}
                        value={mainAIValue}
                        onChange={setMainAIValue}
                    />
                    <DropDown
                        id="summaryAI"
                        label="Summaries and Suggestions AI"
                        options={summaryAIOptions}
                        value={summaryAIValue}
                        onChange={setSummaryAIValue}
                    />
                </div>
            </div>
        </>
    );
};