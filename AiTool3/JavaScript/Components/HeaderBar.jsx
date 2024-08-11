const { useState, useEffect } = React;

const HeaderBar = () => {
    const { colorScheme } = React.useColorScheme();
    const [logoText, setLogoText] = useState("Max's AI Studio");
    const [systemPrompt, setSystemPrompt] = useState("");
    const [mainAIOptions, setMainAIOptions] = useState([]);
    const [mainAIColumnData, setMainAIColumnData] = useState([]);
    const [summaryAIOptions, setSummaryAIOptions] = useState([]);
    const [summaryAIColumnData, setSummaryAIColumnData] = useState([]);
    const [mainAIValue, setMainAIValue] = useState(mainAIOptions[0]);
    const [summaryAIValue, setSummaryAIValue] = useState(summaryAIOptions[0]);
    const [tools, setTools] = useState([]);
    const [starredModels, setStarredModels] = useState({});

    const handleStarToggle = (modelName) => {
        setStarredModels(prevState => {
            const newState = { ...prevState, [modelName]: !prevState[modelName] };
            console.log(`Model: ${modelName}, Starred: ${newState[modelName]}`);
            return newState;
        });
    };

    useEffect(() => {
        window.setTools = (newTools) => {
            setTools(newTools.map(tool => ({ name: tool, checked: false })));
        };

        return () => {
            delete window.setTools;
        };
    }, []);

    // Export methods
    window.setDropdownOptions = (dropdownName, options, columnData) => {
        if (dropdownName === 'mainAI') {
            setMainAIOptions(options);
            setMainAIColumnData(columnData);
        } else if (dropdownName === 'summaryAI') {
            setSummaryAIOptions(options);
            setSummaryAIColumnData(columnData);
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

    window.getTools = () => tools;

    const changeLogo = (newText) => {
        setLogoText(newText);
    };

    const handleAttach = () => { window.chrome.webview.postMessage({ type: 'attach' }); };
    const handleVoice = () => { window.chrome.webview.postMessage({ type: 'voice' }); };
    const handleProject = () => { window.chrome.webview.postMessage({ type: 'project' }); };
    const handleTheme = () => { createThemeEditor(); };

    const handleSystemPromptChange = (newPrompt) => {
        setSystemPrompt(newPrompt);
    };

    const handleToolToggle = (index, state) => {
        const newTools = [...tools];
        newTools[index].checked = state;
        setTools(newTools);
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
                        background-color: ${colorScheme.headerBackgroundColor};
                        background: ${colorScheme.headerBarBackgroundCss};
                        background-image: ${colorScheme.headerBarBackgroundImage};
                        color: ${colorScheme.headerTextColor};
                    }
                    .system-prompt-input {
                        background-color: ${colorScheme.inputBackgroundColor};
                        color: ${colorScheme.inputTextColor};
                        border: none;
                        border-radius: 4px;
                        width: 100%;
                    }
                    .system-prompt-input::placeholder {
                        color: ${colorScheme.textColor}80;
                    }
                    .options-bar {
                        display: flex;
                        justify-content: space-evenly;
                        background-color: ${colorScheme.toolbarBackgroundColor};
                        padding: 5px 0;
                    }
                    .option:hover {
                        background-color: ${colorScheme.selectedItemBackgroundColor};
                    }
                    .dropdown-container select {
                        background-color: ${colorScheme.dropdownBackgroundColor};
                        color: ${colorScheme.dropdownTextColor};
                        border: none;
                        border-radius: 4px;
                        padding: 5px;
                        font-family: ${colorScheme.fontFamily ? colorScheme.fontFamily : "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"};
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
                        color: ${colorScheme.headerTextColor};
                    }
                    .system-prompt-container {
                        flex-grow: 1;
                        margin-left: 20px;
                    }

                    .option {
                        cursor: pointer;
                        padding: 5px 10px;
                        color: ${colorScheme.textColor};
                    }
                    .dropdown-container {
                        display: flex;
                        align-items: center;
                        margin-right: 20px;
                    }
                    .dropdown-container label {
                        margin-right: 10px;
                        color: ${colorScheme.textColor};
                    }
                 `}
            </style>

            <div className="header-bar">
                <div className="main-header">
                    <div className="left-section">
                        <div className="logo">{logoText}</div>
                        <div>
                            <ToggleSplitButton
                                color={colorScheme.buttonBackgroundColor}
                                background={colorScheme.buttonBackgroundCss}
                                border={colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'}
                                borderRadius={colorScheme.borderRadius ? colorScheme.borderRadius : '3px'}
                                label="Tools"
                                svgString='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" stroke="transparent" fill="currentColor">
  <path d="M12 15c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3z" fill="currentColour"/>
  <path d="M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22l-1.92 3.32c-.12.21-.07.47.12.61l2.03 1.58c-.05.3-.07.62-.07.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z" fill="currentColour"/>
</svg>'
                                onToggle={handleToolToggle}
                                dropdownItems={tools.map((tool, index) => ({
                                    label: tool.name,
                                    onClick: (newState) => {
                                        handleToolToggle(index, newState);
                                    }
                                }))}
                            />
                        </div>
                        <div height="24px" width="24px">
                            <SplitButton
                                color={colorScheme.buttonBackgroundColor}
                                background={colorScheme.buttonBackgroundCss}
                                border={colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'}
                                borderRadius={colorScheme.borderRadius ? colorScheme.borderRadius : '3px'}
                                label="Attach"
                                onClick={handleAttach}
                                svgString="<svg class='paperclip-icon' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'> <path d='M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48'></path></svg > "
                            />
                            <SplitButton
                                color={colorScheme.buttonBackgroundColor}
                                background={colorScheme.buttonBackgroundCss}
                                border={colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'}
                                borderRadius={colorScheme.borderRadius ? colorScheme.borderRadius : '3px'}
                                label="Voice"
                                onClick={handleVoice}
                                svgString='<svg viewBox="0 0 150 150" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
  <rect fill="currentColor" x="15" y="67" width="10" height="16" rx="5" />
  <rect fill="currentColor" x="35" y="50" width="10" height="50" rx="5" />
  <rect fill="currentColor" x="55" y="27" width="10" height="96" rx="5" />
  <rect fill="currentColor" x="75" y="43" width="10" height="64" rx="5" />
  <rect fill="currentColor" x="95" y="63" width="10" height="24" rx="5" />
  <rect fill="currentColor" x="115" y="57" width="10" height="36" rx="5" />
  <rect fill="currentColor" x="135" y="67" width="10" height="16" rx="5" />
</svg>'
                            />
                            <SplitButton
                                color={colorScheme.buttonBackgroundColor}
                                background={colorScheme.buttonBackgroundCss}
                                border={colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'}
                                borderRadius={colorScheme.borderRadius ? colorScheme.borderRadius : '3px'}
                                label="Project"
                                onClick={handleProject}
                                svgString='<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
  <rect x="10" y="10" width="80" height="80" fill="none" stroke="currentColor" stroke-width="2"></rect>
  <circle cx="25" cy="25" r="5" fill="currentColor" stroke="currentColor" stroke-width="2"></circle>
  <rect x="35" y="20" width="40" height="10" fill="currentColor" stroke="currentColor" stroke-width="2"></rect>
  <circle cx="25" cy="50" r="5" fill="currentColor" stroke="currentColor" stroke-width="2"></circle>
  <rect x="35" y="45" width="40" height="10" fill="currentColor" stroke="currentColor" stroke-width="2"></rect>
  <circle cx="25" cy="75" r="5" fill="currentColor" stroke="currentColor" stroke-width="2"></circle>
  <rect x="35" y="70" width="40" height="10" fill="currentColor" stroke="currentColor" stroke-width="2"></rect>
</svg>'
                            />
                            <SplitButton
                                color={colorScheme.buttonBackgroundColor}
                                background={colorScheme.buttonBackgroundCss}
                                border={colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'}
                                borderRadius={colorScheme.borderRadius ? colorScheme.borderRadius : '3px'}
                                label="Theme"
                                onClick={handleTheme}
                                svgString='<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
  <path d="M50 10C27.9 10 10 27.9 10 50C10 72.1 27.9 90 50 90C54.4 90 58 86.4 58 82C58 79.9 57.2 78 55.9 76.5C54.6 75 53.8 73.1 53.8 71C53.8 66.6 57.4 63 61.8 63H70C81 63 90 54 90 43C90 24.8 72.1 10 50 10ZM30 50C25.6 50 22 46.4 22 42C22 37.6 25.6 34 30 34C34.4 34 38 37.6 38 42C38 46.4 34.4 50 30 50ZM42 34C37.6 34 34 30.4 34 26C34 21.6 37.6 18 42 18C46.4 18 50 21.6 50 26C50 30.4 46.4 34 42 34ZM66 34C61.6 34 58 30.4 58 26C58 21.6 61.6 18 66 18C70.4 18 74 21.6 74 26C74 30.4 70.4 34 66 34Z" fill="currentColor"/>
</svg>'
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
                        helpText="Used only when you click Send."
                        columnData={mainAIColumnData}
                        starredModels={starredModels}
                        onStarToggle={handleStarToggle}
                    />
                    <DropDown
                        id="summaryAI"
                        label="Secondary AI"
                        options={summaryAIOptions}
                        value={summaryAIValue}
                        onChange={setSummaryAIValue}
                        helpText="Used for summaries, suggestions, and when you click Send via Secondary AI."
                        columnData={summaryAIColumnData}
                        starredModels={starredModels}
                        onStarToggle={handleStarToggle}
                    />
                </div>
            </div>
        </>
    );
};