const { useState, useCallback } = React;

const InputBox = ({ onSend, value, onChange, className }) => {
    const [content, setContent] = useState('');
    const [placeholder, setPlaceholder] = useState('Enter text here...');
    const [isFullScreen, setIsFullScreen] = useState(false);
    const [rotation, setRotation] = useState(0);

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
                        background-color: #333;
                        color: white;
                    }
                .fullscreen-icon {
                    position: absolute;
                    top: 5px;
                    right: 5px;
                    width: 20px;
                    height: 20px;
                    background-color: rgba(0, 0, 0, 0.5);
                    color: white;
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
                    className={`input-box ${className}`}
                    value={value}
                    onChange={(e) => onChange(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder={placeholder}
                    style={isFullScreen ? { width: '100%', height: '100%' } : {}}
                />
                <div
                    className="fullscreen-icon"
                    onClick={toggleFullScreen}
                    style={{ transform: `rotate(${rotation}deg)` }}
                >
                    <svg viewBox="15 15 70 70" width="24" height="24" xmlns="http://www.w3.org/2000/svg">
                        <rect x="0" y="0" width="100" height="100" fill="#ffffff77" />
                        <path d="M25 75 L75 25 M75 25 L60 25 M75 25 L75 40" stroke="black" stroke-width="7" fill="none" stroke-linecap="round" />
                        <path d="M75 75 L25 25 M25 25 L40 25 M25 25 L25 40" stroke="black" stroke-width="7" fill="none" stroke-linecap="round" />
                        <path d="M25 75 L40 75 M25 75 L25 60" stroke="black" stroke-width="7" fill="none" stroke-linecap="round" />
                        <path d="M75 75 L60 75 M75 75 L75 60" stroke="black" stroke-width="7" fill="none" stroke-linecap="round" />
                    </svg>
                </div>
            </div>
        </>
    )
};