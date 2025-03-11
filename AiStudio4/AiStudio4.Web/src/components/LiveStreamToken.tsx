import React, { useEffect, useState } from 'react';

interface LiveStreamTokenProps {
  token: string;
}

export const LiveStreamToken: React.FC<LiveStreamTokenProps> = ({ token }) => {
  const [opacity, setOpacity] = useState(0);

  useEffect(() => {
    setTimeout(() => setOpacity(1), 50);
  }, []);

  return (
    <span className={`inline whitespace-pre-wrap animate-hover ${opacity === 1 ? 'opacity-100' : 'opacity-0'}`}>
      {token}
    </span>
  );
};
