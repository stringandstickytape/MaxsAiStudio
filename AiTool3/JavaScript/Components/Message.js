// Message.js
const Message = ({ role, content, guid }) => {
    const getMessageClass = () => {
        switch (role) {
            case 'user':
                return 'user-message';
            case 'assistant':
                return 'ai-message';
            case 'root':
                return 'root-message';
            default:
                return '';
        }
    };

    return (
        <div className={`message ${getMessageClass()}`} key={guid}>
            <div className="message-role">{role}</div>
            <div className="message-content">{content}</div>
        </div>
    );
};
