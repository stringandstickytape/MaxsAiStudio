import React, { useEffect, useState } from 'react';
import { MarkdownPane } from './markdown-pane';

interface LiveStreamTokenProps {
  token: string;
}

export const LiveStreamToken: React.FC<LiveStreamTokenProps> = ({ token }) => {
    const [opacity, setOpacity] = useState(0);

    useEffect(() => {
        setTimeout(() => setOpacity(1), 50);
    }, []);

    return (
        <span
            className="inline whitespace-pre-wrap"
            style={{
                opacity: opacity,
                transition: 'opacity 250ms ease-in'
            }}
        >
            {token}
        </span>
    );
};