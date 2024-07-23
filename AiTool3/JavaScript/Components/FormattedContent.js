const FormattedContent = ({ content }) => {
    const formatContent = (text) => {
        const codeBlockRegex = /\u0060\u0060\u0060([\s\S]*?)\u0060\u0060\u0060/g;

        const parts = [];
        let lastIndex = 0;

        text.replace(codeBlockRegex, (match, code, offset) => {
            if (offset > lastIndex) {
                parts.push(text.slice(lastIndex, offset));
            }
            parts.push(
                <div key={offset} style={{
                    fontFamily: 'monospace',
                    whiteSpace: 'pre-wrap',
                    backgroundColor: '#333',
                    padding: '10px',
                    borderRadius: '5px',
                    margin: '10px 0'
                }}>
                    {code.trim()}
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
