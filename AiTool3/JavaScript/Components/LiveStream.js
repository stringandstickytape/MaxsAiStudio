// LiveStream.js
const { useState, useEffect, useRef } = React;

const LiveStream = () => {
    const [liveStreamData, setLiveStreamData] = useState('');
    const [isVisible, setIsVisible] = useState(false);
    const [opacity, setOpacity] = useState(0);
    const timeoutRef = useRef(null);
    const containerRef = useRef(null);

    useEffect(() => {
        window.updateTemp = (string) => {
            setLiveStreamData(prevData => prevData + string);
            setIsVisible(true);
            setOpacity(1);
            clearTimeout(timeoutRef.current);
        };

        window.clearTemp = () => {
            setOpacity(0);
            timeoutRef.current = setTimeout(() => {
                setLiveStreamData('');
                setIsVisible(false);
            }, 750); // Wait for fade-out to complete before clearing data
        };

        return () => {
            delete window.updateTemp;
            delete window.clearTemp;
            clearTimeout(timeoutRef.current);
        };
    }, []);

    useEffect(() => {
        if (containerRef.current) {
            containerRef.current.scrollTop = containerRef.current.scrollHeight;
        }
    }, [liveStreamData]);

    if (!isVisible && opacity === 0) return null;

    return (
        <div
            ref={containerRef}
            style={{
                position: 'fixed',
                top: 0,
                left: 0,
                width: '100%',
                height: '100%',
                backgroundColor: 'rgba(0, 0, 0, 0.8)',
                color: 'white',
                zIndex: 9999,
                overflow: 'auto',
                padding: '20px',
                boxSizing: 'border-box',
                whiteSpace: 'pre-wrap',
                fontFamily: 'monospace',
                opacity: opacity,
                transition: 'opacity 0.75s ease-in-out',
            }}
        >
            {liveStreamData}
        </div>
    );
};