const FormattedContent = ({ content }) => {
    const formatContent = (text) => {
        const codeBlockRegex = /\u0060\u0060\u0060(.*?)\n([\s\S]*?)\u0060\u0060\u0060/g;

        const parts = [];
        let lastIndex = 0;

        text.replace(codeBlockRegex, (match, fileType, code, offset) => {
            if (offset > lastIndex) {
                parts.push(text.slice(lastIndex, offset));
            }
            parts.push(
                <div key={offset}>
                    <div style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        fontWeight: 'bold',
                        backgroundColor: '#444',
                        color: 'white',
                        padding: '5px 10px',
                        borderTopLeftRadius: '5px',
                        borderTopRightRadius: '5px',
                    }}>
                        <span>{fileType.trim()}</span>
                        <button
                            onClick={() => {

                                window.chrome.webview.postMessage({
                                    type: 'Copy',
                                    content: code.trim()
                                });
                            }}
                            style={{
                                backgroundColor: '#666',
                                color: 'white',
                                border: 'none',
                                padding: '3px 8px',
                                borderRadius: '3px',
                                cursor: 'pointer',
                            }}
                        >
                            Copy
                        </button>
                    </div>
                    <div style={{
                        fontFamily: 'monospace',
                        whiteSpace: 'pre-wrap',
                        backgroundColor: '#333',
                        color: 'white',
                        padding: '10px',
                        borderBottomLeftRadius: '5px',
                        borderBottomRightRadius: '5px',
                        marginBottom: '10px'
                    }}>
                        {code.trim()}
                    </div>
                </div>
            );
            lastIndex = offset + match.length;
        });

        if (lastIndex < text.length) {
            parts.push(text.slice(lastIndex));
        }

        return parts;
    };

    return <>{formatContent(content)}</>;
};