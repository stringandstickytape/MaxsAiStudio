const { useState, useEffect, useCallback, useRef } = React;
const { useColorScheme } = window;

const ScratchPad = () => {
    console.log("ScratchPad component rendering");

    const { colorScheme } = useColorScheme();
    const scratchPadWindowRef = useRef(null);
    const pillButtonRef = useRef(null);
    const [currentSelection, setCurrentSelection] = useState('');
    const selectionInProgressRef = useRef(false);
    const mousePositionRef = useRef({ x: 0, y: 0 });

    const updatePillButtonStyles = useCallback(() => {
        if (pillButtonRef.current) {
            pillButtonRef.current.style.backgroundColor = colorScheme.buttonBackgroundColor;
            pillButtonRef.current.style.background = colorScheme.buttonBackgroundCss;
            pillButtonRef.current.style.color = colorScheme.buttonTextColor;
            pillButtonRef.current.style.border = colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none';
            pillButtonRef.current.style.borderRadius = colorScheme.borderRadius ? colorScheme.borderRadius : '3px';
        }
    }, [colorScheme]);

    const createPillButton = useCallback(() => {
        console.log("Creating pill button");
        if (pillButtonRef.current && document.body.contains(pillButtonRef.current)) return;
        const button = document.createElement('button');
        button.id = 'scratchPadPillButton';
        button.textContent = 'Copy to Scratch Pad';
        button.style.position = 'fixed';
        button.style.zIndex = '1000';
        button.style.borderRadius = '20px';
        button.style.display = 'none';
        button.style.padding = '5px 10px';
        button.style.border = 'none';
        button.style.cursor = 'pointer';
        button.style.left = '50%';
        button.style.top = '50%';
        button.style.opacity = '0.8';
        button.style.transform = 'translate(-50%, -50%)';
        document.body.appendChild(button);
        pillButtonRef.current = button;
        updatePillButtonStyles();
        console.log("Pill button created and added to the DOM");
    }, [updatePillButtonStyles]);

    const showPillButton = useCallback((x, y) => {
        console.log("Showing pill button");
        const button = pillButtonRef.current;
        if (!button) {
            console.log("Pill button not found");
            return;
        }

        const buttonRect = button.getBoundingClientRect();
        const windowWidth = window.innerWidth;
        const windowHeight = window.innerHeight;

        let left = x + 10;
        let top = y + 10;

        if (left + buttonRect.width > windowWidth) {
            left = windowWidth - buttonRect.width - 10;
        }
        if (top + buttonRect.height > windowHeight) {
            top = windowHeight - buttonRect.height - 10;
        }

        button.style.left = `${left}px`;
        button.style.top = `${top}px`;
        button.style.display = 'block';
        button.style.transform = 'none';
        console.log("Pill button displayed at", left, top);
    }, []);

    const hidePillButton = useCallback(() => {
        console.log("Hiding pill button");
        if (pillButtonRef.current) {
            pillButtonRef.current.style.display = 'none';
        }
    }, []);

    const copyToScratchPad = useCallback(() => {
        console.log("Copying to scratch pad");
        if (!scratchPadWindowRef.current || scratchPadWindowRef.current.closed) {
            createScratchPadWindow();
        }
        scratchPadWindowRef.current.document.getElementById('scratchPadContent').value += currentSelection + '\n\n';
        window.chrome.webview.postMessage({
            type: 'saveScratchpad',
            content: scratchPadWindowRef.current.document.getElementById('scratchPadContent').value
        });
        hidePillButton();
        clearSelection();
    }, [currentSelection, hidePillButton]);

    const [scratchpadContent, setScratchpadContent] = useState('');
    const debouncedAddMessageButtonRef = useRef(null);

    const debouncedAddMessageButton = useCallback(() => {
        if (debouncedAddMessageButtonRef.current) {
            clearTimeout(debouncedAddMessageButtonRef.current);
        }

        debouncedAddMessageButtonRef.current = setTimeout(() => {
            console.log('Sending content...');
                window.chrome.webview.postMessage({
                    type: 'saveScratchpad',
                    content: scratchpadContent
            });
        }, 1000); // 1 second
    }, [scratchpadContent]);

    const updateScratchpadContent = useCallback((newContent) => {
        setScratchpadContent(newContent);
        debouncedAddMessageButton();
    }, [debouncedAddMessageButton]);

    const createScratchPadWindow = useCallback(() => {
        console.log("Creating scratch pad window");
        const newWindow = window.open('', 'ScratchPad', 'width=400,height=400');
        newWindow.document.write(`
            <html>
            <head>
                <title>Scratch Pad</title>
                <style>
                    body { 
                        font-family: Arial, sans-serif; 
                        margin: 0; 
                        padding: 10px; 
                        background-color: ${colorScheme.backgroundColor};
                        color: ${colorScheme.textColor};
                        display: flex;
                        flex-direction: column;
                        height: 100vh;
                    }
                    #scratchPadContent { 
                        width: 100%; 
                        height: calc(100% - 65px); 
                        resize: none; 
                        background-color: ${colorScheme.inputBackgroundColor};
                        color: ${colorScheme.inputTextColor};
                        border: 1px solid ${colorScheme.buttonBackgroundColor};
                        margin-bottom: 10px;
                    }
                    #copyToInputButton {
                        padding: 10px;
                        background-color: ${colorScheme.buttonBackgroundColor};
                        background: ${colorScheme.buttonBackgroundCss};
                        border: ${colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'};
                        borderRadius: ${colorScheme.borderRadius ? colorScheme.borderRadius : '3px'};
                        color: ${colorScheme.buttonTextColor};
                        border: none;
                        cursor: pointer;
                    }
                </style>
            </head>
            <body>
                <textarea id="scratchPadContent"></textarea>
                <button id="copyToInputButton">Copy Selected to Input</button>
                <script>
                    document.getElementById('scratchPadContent').addEventListener('input', function() {
                        window.opener.updateScratchpadContent(this.value);
                    });
                    document.getElementById('copyToInputButton').addEventListener('click', function() {
                        const selectedText = document.getElementById('scratchPadContent').value.substring(
                            document.getElementById('scratchPadContent').selectionStart,
                            document.getElementById('scratchPadContent').selectionEnd
                        );
                        window.opener.appendToUserInput(selectedText);
                    });
                </script>
            </body>
            </html>
        `);
        scratchPadWindowRef.current = newWindow;
    }, [colorScheme, updateScratchpadContent]);

    const clearSelection = useCallback(() => {
        console.log("Clearing selection");
        if (window.getSelection) {
            window.getSelection().removeAllRanges();
        } else if (document.selection) {
            document.selection.empty();
        }
    }, []);

    const setScratchpadContentAndOpen = useCallback((content) => {
        if (!scratchPadWindowRef.current || scratchPadWindowRef.current.closed) {
            createScratchPadWindow();
        }
        scratchPadWindowRef.current.document.getElementById('scratchPadContent').value = content;
        updateScratchpadContent(content);
        scratchPadWindowRef.current.focus();
    }, [createScratchPadWindow, updateScratchpadContent]);

    const openScratchpad = useCallback(() => {
        if (!scratchPadWindowRef.current || scratchPadWindowRef.current.closed) {
            createScratchPadWindow();
        }
        scratchPadWindowRef.current.focus();
    }, [createScratchPadWindow]);

    const checkSelection = useCallback((x, y) => {
        console.log("Checking selection");
        const selection = window.getSelection();
        const newSelection = selection.toString().trim();
        console.log("New selection:", newSelection);
        setCurrentSelection(newSelection);
        if (newSelection.length > 0) {
            console.log("Selection not empty, showing pill button at", x, y);
            showPillButton(x, y);
        } else {
            console.log("Selection empty, hiding pill button");
            hidePillButton();
        }
    }, [showPillButton, hidePillButton]);

    useEffect(() => {
        console.log("Setting up ScratchPad effect");
        createPillButton();

        const handleSelectionEnd = (e) => {
            console.log("Selection ended");
            if (selectionInProgressRef.current) {
                selectionInProgressRef.current = false;
                setTimeout(() => checkSelection(e.clientX, e.clientY), 10);
            }
        };

        const handleKeyDown = (e) => {
            if (e.shiftKey) {
                console.log("Shift key pressed");
                selectionInProgressRef.current = true;
            }
            // Hide pill button when any key is pressed and the button is visible
            if (pillButtonRef.current && pillButtonRef.current.style.display !== 'none') {
                console.log("Key pressed, hiding pill button");
                hidePillButton();
            }
        };

        const handleWheel = () => {
            // Hide pill button when mouse wheel is scrolled and the button is visible
            if (pillButtonRef.current && pillButtonRef.current.style.display !== 'none') {
                console.log("Mouse wheel scrolled, hiding pill button");
                hidePillButton();
            }
        };

        const handleKeyUp = (e) => {
            if (!e.shiftKey && selectionInProgressRef.current) {
                console.log("Shift key released");
                selectionInProgressRef.current = false;
                setTimeout(() => checkSelection(mousePositionRef.current.x, mousePositionRef.current.y), 10);
            }
        };

        const handleDocumentClick = (e) => {
            if (pillButtonRef.current && !pillButtonRef.current.contains(e.target)) {
                console.log("Document clicked, hiding pill button");
                hidePillButton();
            }
        };

        const updateMousePosition = (e) => {
            mousePositionRef.current = { x: e.clientX, y: e.clientY };
        };

        document.addEventListener('mousedown', () => {
            console.log("Mouse down, selection in progress");
            selectionInProgressRef.current = true;
        });
        document.addEventListener('mouseup', handleSelectionEnd);
        document.addEventListener('keydown', handleKeyDown);
        document.addEventListener('keyup', handleKeyUp);
        document.addEventListener('click', handleDocumentClick);
        document.addEventListener('mousemove', updateMousePosition);
        document.addEventListener('wheel', handleWheel);

        if (pillButtonRef.current) {
            pillButtonRef.current.addEventListener('click', copyToScratchPad);
        }

        return () => {
            console.log("Cleaning up ScratchPad effect");
            document.removeEventListener('mousedown', () => { selectionInProgressRef.current = true; });
            document.removeEventListener('mouseup', handleSelectionEnd);
            document.removeEventListener('keydown', handleKeyDown);
            document.removeEventListener('keyup', handleKeyUp);
            document.removeEventListener('click', handleDocumentClick);
            document.removeEventListener('mousemove', updateMousePosition);
            document.removeEventListener('wheel', handleWheel);
            if (pillButtonRef.current) {
                pillButtonRef.current.removeEventListener('click', copyToScratchPad);
            }
            // Note: We're not removing the pill button from the DOM on cleanup
        };
    }, [createPillButton, checkSelection, hidePillButton, copyToScratchPad]);

    useEffect(() => {
        console.log("Updating pill button styles due to color scheme change");
        updatePillButtonStyles();
    }, [colorScheme, updatePillButtonStyles]);

    useEffect(() => {
        window.updateScratchpadContent = updateScratchpadContent;
        window.appendToUserInput = (text) => {
            const currentContent = window.getUserPrompt();
            const newContent = currentContent ? currentContent + '\n' + text : text;
            window.setUserPrompt(newContent);
        };
        window.setScratchpadContentAndOpen = setScratchpadContentAndOpen;
        window.openScratchpad = openScratchpad;

        return () => {
            delete window.updateScratchpadContent;
            delete window.appendToUserInput;
            delete window.setScratchpadContentAndOpen;
            delete window.openScratchpad;
        };
    }, [updateScratchpadContent, setScratchpadContentAndOpen, openScratchpad]);

    return null;
};

window.ScratchPad = ScratchPad;