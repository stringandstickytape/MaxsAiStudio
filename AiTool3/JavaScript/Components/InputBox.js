const { useState, useCallback } = React;

const InputBox = ({ onSend }) => {
    const [content, setContent] = useState('');
    const [placeholder, setPlaceholder] = useState('Enter text here...');

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

    // Export methods
    window.setInputContent = setInputContent;
    window.getInputContent = getInputContent;
    window.setInputPlaceholder = setInputPlaceholder;

    return (
        <>
            <style>
                {`
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
                `}
            </style>

            <textarea
                className="input-box"
                value={content}
                onChange={(e) => setContent(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder={placeholder}
            />
        </>
    );
};