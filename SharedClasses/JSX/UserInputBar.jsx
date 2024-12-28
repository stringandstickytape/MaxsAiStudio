const UserInputBar = () => {
    const { colorScheme } = React.useColorScheme();

    const [sendDisabled, setSendDisabled] = React.useState(false);
    const [cancelDisabled, setCancelDisabled] = React.useState(false);
    const [newDisabled, setNewDisabled] = React.useState(false);
    const [inputContent, setInputContent] = React.useState('');
    const [sendAlternateLabel, setSendAlternateLabel] = React.useState('');
    const [sendAlternateColor, setSendAlternateColor] = React.useState('');
    const [cancelAlternateLabel, setCancelAlternateLabel] = React.useState('');
    const [cancelAlternateColor, setCancelAlternateColor] = React.useState('');
    const [newAlternateLabel, setNewAlternateLabel] = React.useState('');
    const [newAlternateColor, setNewAlternateColor] = React.useState('');
    const [sendButtonLabel, setSendButtonLabel] = React.useState('Send');
    const [addEmbeddings, setAddEmbeddings] = React.useState(false);
    const [showSendWithPrefill, setShowSendWithPrefill] = React.useState(false);
    const [isPrefillModalOpen, setIsPrefillModalOpen] = React.useState(false);

    window.setSendButtonLabel = setSendButtonLabel;

    const inputBoxRef = React.useRef(null);

    const insertTextAtCaret = (text) => {
        if (inputBoxRef.current) {
            const textarea = inputBoxRef.current;
            const start = textarea.selectionStart;
            const end = textarea.selectionEnd;
            const newContent = inputContent.substring(0, start) + text + inputContent.substring(end);
            setInputContent(newContent);

            setTimeout(() => {
                textarea.selectionStart = textarea.selectionEnd = start + text.length;
                textarea.focus();
            }, 0);
        }
    };

    const moveCaretToStart = (shiftHeld) => {
        if (inputBoxRef.current) {
            const textarea = inputBoxRef.current;

            if (shiftHeld) {
                textarea.setSelectionRange(0, textarea.selectionEnd);
            } else {
                textarea.setSelectionRange(0, 0);
            }

            textarea.focus();
        }
    }

    const moveCaretToEnd = async (shiftHeld, ctrlHeld, altHeld) => {
        if (inputBoxRef.current) {
            const textarea = inputBoxRef.current;

            // Dispatch the "End" key event (still useful for consistency and potential side effects)
            const endKeyEvent = new KeyboardEvent('keydown', {
                key: 'End',
                code: 'End',
                keyCode: 35,
                bubbles: true,
                cancelable: true,
                shiftKey: shiftHeld,
                ctrlKey: ctrlHeld,
                altKey: altHeld
            });
            textarea.dispatchEvent(endKeyEvent);

            // Use setTimeout to ensure our manual adjustments happen after the event is processed
            setTimeout(() => {
                if (ctrlHeld) {
                    // Ctrl+End: Move caret to the very end
                    textarea.selectionStart = textarea.selectionEnd = textarea.value.length;
                } else {
                    // End (without Ctrl): Manually move the caret to the end of the current line

                    const textAfterCaret = textarea.value.substring(textarea.selectionEnd);

                    const endOfCurrentLineIndex = textAfterCaret.indexOf('\n'); // Get the last line (current line)

                    const newCaretPosition = textarea.selectionEnd + (endOfCurrentLineIndex === -1 ? textAfterCaret.length : endOfCurrentLineIndex);

                    // Update selection, handling Shift if necessary
                    if (shiftHeld) {
                        textarea.selectionEnd = newCaretPosition;
                    } else {
                        textarea.setSelectionRange(newCaretPosition, newCaretPosition);
                    }
                }

                textarea.focus();
            }, 0);
        }
    };

    React.useEffect(() => {
        window.insertTextAtCaret = insertTextAtCaret;
        window.moveCaretToEnd = moveCaretToEnd;
        window.moveCaretToStart = moveCaretToStart;
        return () => {
            delete window.insertTextAtCaret;
            delete window.moveCaretToEnd;
            delete window.moveCaretToStart;
        };
    }, [inputContent]);

    function getTrueIndices(obj) {
        return Object.keys(obj)
            .filter(key => obj[key] === true)
            .join(',');
    }

    const handleSend = () => {
        // Check Live Scroll if it's not already checked
        if (!window.getLiveScroll()) {
            window.setLiveScroll(true);
        }

        var toolsEnabledIndices = getTrueIndices(window.splitButtonState_Tools.itemStates);

        window.chrome.webview.postMessage({
            type: window.event.shiftKey ? 'sendSecondary' : 'send',
            content: inputContent,
            selectedTools: toolsEnabledIndices,
            addEmbeddings: addEmbeddings.toString()
        });
    };

    const handleSendSecondary = () => {
        // Check Live Scroll if it's not already checked
        if (!window.getLiveScroll()) {
            window.setLiveScroll(true);
        }

        var toolsEnabledIndices = getTrueIndices(window.splitButtonState_Tools.itemStates);

        window.chrome.webview.postMessage({
            type: 'sendSecondary',
            content: inputContent,
            selectedTools: toolsEnabledIndices,
            addEmbeddings: addEmbeddings.toString()
        });
    };

    const handlePrefillSubmit = (prefillText) => {
        // Check Live Scroll if it's not already checked
        if (!window.getLiveScroll()) {
            window.setLiveScroll(true);
        }

        setIsPrefillModalOpen(false);
        window.chrome.webview.postMessage({
            type: 'sendWithPrefill',
            content: inputContent,
            prefillText: prefillText,
            selectedTools: getTrueIndices(window.splitButtonState_Tools.itemStates),
            addEmbeddings: addEmbeddings.toString()
        });
    };

    const handleSendWithPrefill = () => {
        setIsPrefillModalOpen(true);
    };


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

    const setSendButtonAlternate = (label, color) => {
        setSendAlternateLabel(label);
        setSendAlternateColor(color);
    };

    const setCancelButtonAlternate = (label, color) => {
        setCancelAlternateLabel(label);
        setCancelAlternateColor(color);
    };

    const setNewButtonAlternate = (label, color) => {
        setNewAlternateLabel(label);
        setNewAlternateColor(color);
    };

    const handleCancel = () => {
        window.chrome.webview.postMessage({ type: 'cancel' });
    };

    const handleInputChange = (string) => {
        setInputContent(string);
        if (string.trim().length > 0) window.setMessageText("temp-user-msg", string);
    };

    const setUserPrompt = (string, base64image, base64type) => {
        debugger;
        setInputContent(string);
        if (string.trim().length > 0) window.setMessageText("temp-user-msg", string, 0, base64image, base64type);
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

    React.useEffect(() => {
        window.disableSendButton = disableSendButton;
        window.enableSendButton = enableSendButton;
        window.disableCancelButton = disableCancelButton;
        window.enableCancelButton = enableCancelButton;
        window.disableNewButton = disableNewButton;
        window.enableNewButton = enableNewButton;
        window.setSendButtonAlternate = setSendButtonAlternate;
        window.setCancelButtonAlternate = setCancelButtonAlternate;
        window.setNewButtonAlternate = setNewButtonAlternate;
        window.toggleSendWithPrefill = () => {
            setShowSendWithPrefill(prev => !prev);
        };

        window.enablePrefill = () => {
            setShowSendWithPrefill(true);
        };

        window.disablePrefill = () => {
            setShowSendWithPrefill(false);
        };

        return () => {
            delete window.disableSendButton;
            delete window.enableSendButton;
            delete window.disableCancelButton;
            delete window.enableCancelButton;
            delete window.disableNewButton;
            delete window.enableNewButton;
            delete window.setSendButtonAlternate;
            delete window.setCancelButtonAlternate;
            delete window.setNewButtonAlternate;
            delete window.toggleSendWithPrefill;
        };
    }, []);

    return (
        <>
            <style>
                {`
                    .user-input-bar {
                        display: flex;
                        align-items: stretch;
                        background-color: ${colorScheme.backgroundColor};
                        color: ${colorScheme.textColor};
                        border-top: 1px solid ${colorScheme.headerBackgroundColor};
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
                        padding: 4px;
                        border: none;
                        border-radius: 4px;
                        color: ${colorScheme.buttonTextColor};
                        cursor: pointer;
                        font-size: 14px;
                        transition: opacity 0.3s ease;
                    }
                    .input-button:disabled {
                        opacity: 0.5;
                        cursor: not-allowed;
                    }
                    .send-button { background-color: ${colorScheme.buttonBackgroundColor}; background: ${colorScheme.buttonBackgroundCss}; 
                                border: ${colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'};
                                borderRadius: ${colorScheme.borderRadius ? colorScheme.borderRadius : '3px'};
                
                }
                    .cancel-button { background-color: ${colorScheme.buttonBackgroundColor}; background: ${colorScheme.buttonBackgroundCss}; 
                                                border: ${colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'};
                                borderRadius: ${colorScheme.borderRadius ? colorScheme.borderRadius : '3px'};
                            }
                    .new-button { background-color: ${colorScheme.buttonBackgroundColor}; background: ${colorScheme.buttonBackgroundCss}; 
                                                border: ${colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'};
                                borderRadius: ${colorScheme.borderRadius ? colorScheme.borderRadius : '3px'};
                            }

                    .checkbox-wrapper {
                        display: flex;
                        align-items: center;
                        font-size: 10px;
                        max-width: 95%;
                        overflow: hidden;
                    }
                    .checkbox-wrapper input[type="checkbox"] {
                        margin-right: 5px;
                    }

                    .split-button-container {
                        display: flex;
                        margin-bottom: 5px;
                    }
                    .split-button-main, .split-button-arrow {
                        background-color: ${colorScheme.buttonBackgroundColor};
                        background: ${colorScheme.buttonBackgroundCss};
                        color: ${colorScheme.buttonTextColor};
                        border: ${colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'};
                        borderRadius: ${colorScheme.borderRadius ? colorScheme.borderRadius : '3px'};
                        padding: 4px;
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
                        background-color: ${colorScheme.dropdownBackgroundColor};
                        min-width: 160px;
                        box-shadow: 0px 8px 16px 0px rgba(0,0,0,0.5);
                        z-index: 1;
                    }
                    .split-button-dropdown-item {
                        color: ${colorScheme.dropdownTextColor};
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
                        background-color: ${colorScheme.selectedItemBackgroundColor};
                    }
                `}
            </style>
            <div className="user-input-bar">
                <div className="input-box-wrapper">
                    <InputBox
                        ref={inputBoxRef}
                        id="user-input-textarea"
                        onSend={handleSend}
                        value={inputContent}
                        onChange={handleInputChange}
                        placeholderText="Enter prompt..."
                        disabled={sendDisabled}
                    />
                </div>
                <div className="buttons-wrapper">
                    <div className="checkbox-wrapper">
                        <input
                            type="checkbox"
                            id="addEmbeddings"
                            checked={addEmbeddings}
                            onChange={(e) => setAddEmbeddings(e.target.checked)}
                        />
                        <label htmlFor="addEmbeddings">Add Embeddings</label>
                    </div>
                    <SplitButton
                        label={sendButtonLabel}
                        onClick={handleSend}
                        disabled={sendDisabled}
                        color={colorScheme.buttonBackgroundColor}
                        background={colorScheme.buttonBackgroundCss}
                        border={colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'}
                        borderRadius={colorScheme.borderRadius ? colorScheme.borderRadius : '3px'}
                        alternateLabel={sendAlternateLabel}
                        alternateColor={sendAlternateColor}
                        dropdownItems={[
                            { label: "Send via Secondary AI", onClick: handleSendSecondary },
                            ...(showSendWithPrefill ? [{ label: "Send with Prefill", onClick: handleSendWithPrefill }] : [])
                        ]}
                        title="CTRL+Enter to send, CTRL+SHIFT+Enter to send via Secondary AI"
                    />

                    <SplitButton
                        label="Cancel"
                        onClick={handleCancel}
                        disabled={cancelDisabled}
                        color={colorScheme.buttonBackgroundColor}
                        background={colorScheme.buttonBackgroundCss}
                        alternateLabel={cancelAlternateLabel}
                        alternateColor={cancelAlternateColor}
                        border={colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'}
                        borderRadius={colorScheme.borderRadius ? colorScheme.borderRadius : '3px'}
                    />
                    <SplitButton
                        label="New"
                        onClick={handleNew}
                        dropdownItems={[
                            { label: "New with context", onClick: handleNewWithContext },
                            { label: "New with prompt", onClick: handleNewWithPrompt }
                        ]}
                        disabled={newDisabled}
                        color={colorScheme.buttonBackgroundColor}
                        background={colorScheme.buttonBackgroundCss}
                        alternateLabel={newAlternateLabel}
                        alternateColor={newAlternateColor}
                        border={colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'}
                        borderRadius={colorScheme.borderRadius ? colorScheme.borderRadius : '3px'}
                    />
                </div>
            </div>
            {isPrefillModalOpen && (
                <PrefillModal
                    isOpen={isPrefillModalOpen}
                    onClose={() => setIsPrefillModalOpen(false)}
                    onSubmit={handlePrefillSubmit}
                />
            )}
        </>
    );
};