// LiveStream.js
const { useState, useEffect, useRef } = React;

const LiveStream = () => {
    const { colorScheme } = React.useColorScheme();
    const [liveStreamData, setLiveStreamData] = useState('');
    const [isVisible, setIsVisible] = useState(false);
    const [opacity, setOpacity] = useState(0);
    const timeoutRef = useRef(null);
    const containerRef = useRef(null);

    window.ScrollToTemp = () => {
        const mainContent = document.getElementsByClassName('main-content')[0];
        //mainContent.setAttribute("style", "scroll-behavior: auto");
        mainContent.scrollTop = mainContent.scrollHeight;
        //mainContent.setAttribute("style", "scroll-behavior: smooth");
    };

    useEffect(() => {
        window.updateTemp = (string) => {
            setLiveStreamData(prevData => prevData + string);
            setIsVisible(true);
            setOpacity(1);
            clearTimeout(timeoutRef.current);
            ScrollToTemp();
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
                width: '100%',
                maxHeight: '200px',
                backgroundColor: colorScheme.messageAIBackgroundColor,
                color: colorScheme.messageAITextColor,
                overflow: 'auto',
                padding: '10px',
                boxSizing: 'border-box',
                whiteSpace: 'pre-wrap',
                fontFamily: 'monospace',
                opacity: opacity,
                borderRadius: '5px',
                margin: '10px 0',
            }}
        >
            {liveStreamData}
        </div>
    );
};