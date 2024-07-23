﻿// Message.js
const Message = ({ role, content, guid }) => {
    const getMessageClass = () => {
        switch (role) {
            case 0:
                return 'user-message';
            case 1:
                return 'ai-message';
            case 2:
                return 'root-message';
            default:
                return '';
        }
    };

    const getMessageLabel = () => {
        switch (role) {
            case 0:
                return 'User';
            case 1:
                return 'AI';
            case 2:
                return 'Root';
            default:
                return '';
        }
    };

    // Initialize a counter for code blocks
    let codeBlockCounter = 0;

    const handleRofls = () => {
        // Implement the rofls action here
        console.log('ROFL action triggered');
        // You can add more functionality as needed
    };

    const isUnterminatedCodeBlock = () => {
        const threeBackticks = String.fromCharCode(96, 96, 96);
        const occurrences = (content.match(new RegExp(threeBackticks, 'g')) || []).length;
        return occurrences % 2 !== 0;
    };

    return (
        <div className={`message ${getMessageClass()}`} key={guid}>
            <div className="message-header">
                <div className="message-role">{getMessageLabel()}</div>
                <div className="message-actions">
                    {role === 1 && (
                        <button onClick={handleRofls} className="rofls-button">
                            ROFLs
                        </button>
                    )}
                    {role === 1 && isUnterminatedCodeBlock() && (
                        <button onClick={handleRofls} className="rofls-button">
                            ROFL2s
                        </button>
                    )}
                    <div>!!{isUnterminatedCodeBlock() ? "true" : "false"}</div>
                </div>
            </div>
            <div className="message-content">
                <FormattedContent
                    content={content}
                    guid={guid}
                    codeBlockCounter={codeBlockCounter}
                    onCodeBlockRendered={() => codeBlockCounter++}
                />
            </div>
        </div>
    );
}