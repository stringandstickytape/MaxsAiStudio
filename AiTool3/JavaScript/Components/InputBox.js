const { useState, useCallback } = React;

const InputBox = ({ onSend }) => {
    const [content, setContent] = useState('');
    const [placeholder, setPlaceholder] = useState('Enter text here...');
    const [isFullScreen, setIsFullScreen] = useState(false);

    const handleKeyDown = useCallback((event) => {
        if (event.ctrlKey && event.key === 'Enter') {
            event.preventDefault();
            onSend();
        }
    }, [onSend]);

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
                        width: 100%;
                    }
                    .input-box {
                        width: 100%;
                        min-height: 100px;
                        font-size: 16px;
                        border: 1px solid #ccc;
                        border-radius: 4px;
                        resize: vertical;
                        overflow-wrap: break-word;
                        word-wrap: break-word;
                    }
                    .fullscreen-icon {
                        position: absolute;
                        top: 5px;
                        right: 5px;
                        width: 15px;
                        height: 15px;
                        background-color: rgba(0, 0, 0, 0.5);
                        color: white;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        cursor: pointer;
                        font-size: 12px;
                        border-radius: 2px;
                        transform: rotate(45deg);
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
                    className="input-box"
                    value={content}
                    onChange={(e) => setContent(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder={placeholder}
                    style={isFullScreen ? { width: '100%', height: '100%' } : {}}
                />
                <div className="fullscreen-icon" onClick={toggleFullScreen}>
                    ↔
                </div>
            </div>
        </>
    );
};