const { useState } = React;

const InputBox = () => {
    const [content, setContent] = useState('');
    const [placeholder, setPlaceholder] = useState('Enter text here...');

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
                        padding: 10px;
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
                placeholder={placeholder}
            />
        </>
    );
};