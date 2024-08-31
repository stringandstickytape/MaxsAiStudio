// Message.js
const Message = ({ role, content: initialContent, guid, previousAssistantUnbalanced }) => {
    const { colorScheme } = window.useColorScheme();
    const [showContinueButton, setShowContinueButton] = useState(false);
    const [content, setContent] = useState(initialContent);
    const messageRef = useRef(null);

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

    const isUnterminatedCodeBlock = () => {
        const threeBackticks = String.fromCharCode(96, 96, 96);
        const occurrences = (content.match(new RegExp(threeBackticks, 'g')) || []).length;
        return occurrences % 2 !== 0;
    };

    const handleContinue = () => {
        window.chrome.webview.postMessage({
            type: 'continue',
            guid: guid
        });
    };

    const scrollToTop = () => {
        if (messageRef.current) {
            messageRef.current.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    };

    useEffect(() => {
        setContent(initialContent);
    }, [initialContent]);

    useEffect(() => {
        if (role === 1 && isUnterminatedCodeBlock() && guid !== 'temp-ai-msg' && guid !== 'temp-user-msg') {
            setShowContinueButton(true);
        } else {
            setShowContinueButton(false);
        }
    }, [role, content, guid]);

    return (
        <div className={`message ${getMessageClass()}`} key={guid} ref={messageRef}>
            <div className="message-header">
                <div className="message-role">{getMessageLabel()}</div>
            </div>
            <div className="message-content">
                <FormattedContent
                    content={content}
                    guid={guid}
                    codeBlockCounter={codeBlockCounter}
                    onCodeBlockRendered={() => codeBlockCounter++}
                />
            </div>
            <div className="message-footer">
                {showContinueButton && (
                    <SplitButton
                        label="Continue..."
                        onClick={handleContinue}
                        color={colorScheme.buttonBackgroundColor}
                        background={colorScheme.buttonBackgroundCss}
                        border={colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none'}
                        borderRadius={colorScheme.borderRadius ? colorScheme.borderRadius : '3px'}
                    />
                )}
                <button className="top-button" onClick={scrollToTop}>↑ Top</button>
            </div>
        </div>
    );
}
