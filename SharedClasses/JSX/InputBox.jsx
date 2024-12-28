const { useState, useCallback } = React;
const { useColorScheme } = React;

const InputBox = React.forwardRef(({ onSend, value, onChange, className, placeholderText }, ref) => {
    const [content, setContent] = useState('');
    const [placeholder, setPlaceholder] = useState(placeholderText);
    const [isFullScreen, setIsFullScreen] = useState(false);
    const [rotation, setRotation] = useState(0);

    const { colorScheme } = useColorScheme();

    const handleKeyDown = useCallback((event) => {
        if (event.ctrlKey && event.key === 'Enter') {
            event.preventDefault();
            onSend();
        }
    }, [onSend]);

    const handlePaste = (event) => {
        event.preventDefault();
        const pastedText = event.clipboardData.getData('text');
        const cleanedText = pastedText.replace(/\r/g, '');

        const selectionStart = event.target.selectionStart;
        const selectionEnd = event.target.selectionEnd;

        const newContent = value.slice(0, selectionStart) + cleanedText + value.slice(selectionEnd);
        onChange(newContent);

        // Set the caret position after the pasted content and scroll it into view
        // We need to use setTimeout to ensure the state has been updated before we set the selection
        setTimeout(() => {
            event.target.selectionStart = selectionStart + cleanedText.length;
            event.target.selectionEnd = selectionStart + cleanedText.length;

            // Scroll the caret into view
            event.target.scrollIntoView({ block: 'nearest' });
        }, 0);
    };
    const setInputContent = (newContent) => {
        setContent(newContent);
    };

    const getInputContent = () => {
        return content;
    };

    const setInputPlaceholder = (newPlaceholder) => {
        setPlaceholder(newPlaceholder);
    };

    const toggleFullScreen = () => {
        setIsFullScreen(!isFullScreen);
        setRotation(rotation + 90);
    };

    // Export methods
    window.setInputContent = setInputContent;
    window.getInputContent = getInputContent;
    window.setInputPlaceholder = setInputPlaceholder;

    return (
        <>
            <style>
                {`
                    .input-box-container {
                        position: relative;
                        height: calc(100% - 10px);
                        width: 100%;
                    }
                    .input-box {
                        width: 100%;
                        min-height: 100px;
                        height:100%;
                        font-size: 16px;
                        border: 1px solid ${colorScheme.inputBackgroundColor};
                        border-radius: 4px;
                        resize: vertical;
                        overflow-wrap: break-word;
                        word-wrap: break-word;
                        background-color: ${colorScheme.inputBackgroundColor};
                        color: ${colorScheme.inputTextColor};
                        margin-top: 5px;
                        margin-left: 3px;
                    }
                    .fullscreen-icon {
                        position: absolute;
                        top: 5px;
                        right: 5px;
                        width: 20px;
                        height: 20px;
                        background-color: rgba(0, 0, 0, 0.5);
                        color: ${colorScheme.buttonTextColor};
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        cursor: pointer;
                        border-radius: 2px;
                        transition: transform 0.5s ease;
                    }
                    .fullscreen-icon svg {
                        width: 14px;
                        height: 14px;
                        fill: currentColor;
                    }
                    .fullscreen {
                        position: fixed;
                        top: 0;
                        left: 0;
                        width: 100vw;
                        height: 100vh;
                        z-index: 9999;
                    }
                `}
            </style>

            <div className={`input-box-container ${isFullScreen ? 'fullscreen' : ''}`}>
                <textarea
                    ref={ref}
                    className={`input-box ${className}`}
                    value={value}
                    onChange={(e) => onChange(e.target.value)}
                    onKeyDown={handleKeyDown}
                    onPaste={handlePaste}
                    placeholder={placeholder}
                    style={isFullScreen ? { width: '100%', height: '100%' } : {}}
                />
                <div
                    className="fullscreen-icon"
                    onClick={toggleFullScreen}
                    style={{ transform: `rotate(${rotation}deg)` }}
                >
                    <svg viewBox="15 15 70 70" width="24" height="24" xmlns="http://www.w3.org/2000/svg">
                        <rect x="0" y="0" width="100" height="100" fill={colorScheme.buttonBackgroundColor} />
                        <path d="M25 75 L75 25 M75 25 L60 25 M75 25 L75 40" stroke={colorScheme.buttonTextColor} strokeWidth="7" fill="none" strokeLinecap="round" />
                        <path d="M75 75 L25 25 M25 25 L40 25 M25 25 L25 40" stroke={colorScheme.buttonTextColor} strokeWidth="7" fill="none" strokeLinecap="round" />
                        <path d="M25 75 L40 75 M25 75 L25 60" stroke={colorScheme.buttonTextColor} strokeWidth="7" fill="none" strokeLinecap="round" />
                        <path d="M75 75 L60 75 M75 75 L75 60" stroke={colorScheme.buttonTextColor} strokeWidth="7" fill="none" strokeLinecap="round" />
                    </svg>
                </div>
            </div>
        </>
    )
});