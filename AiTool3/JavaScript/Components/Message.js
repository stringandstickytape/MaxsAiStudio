// Message.js
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



    return (
        <div className={`message ${getMessageClass()}`} key={guid}>
            <div className="message-role">{getMessageLabel()}</div>
            <div className="message-content">{content}</div>
        </div>
    );
}
