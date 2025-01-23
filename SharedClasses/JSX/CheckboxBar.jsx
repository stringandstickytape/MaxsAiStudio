const CheckboxBar = () => {
    const { colorScheme } = React.useColorScheme();
    const [isLiveScroll, setIsLiveScroll] = React.useState(true);
    const [isFormatting, setIsFormatting] = React.useState(true);

    // Single useEffect for initialization and updates
    React.useEffect(() => {
        // Set up the getFormatting function once
        window.getFormatting = () => isFormatting;

        // Update other window properties
        window.isLiveScroll = isLiveScroll;
        window.isFormatting = isFormatting;

        // Set up control methods
        window.setLiveScroll = (value) => setIsLiveScroll(value);
        window.getLiveScroll = () => isLiveScroll;
        window.setFormatting = (value) => {
            setIsFormatting(value);
            window.dispatchEvent(new Event('formattingChanged'));
        };

        // Trigger initial formatting state
        window.dispatchEvent(new Event('formattingChanged'));

        return () => {
            delete window.isLiveScroll;
            delete window.setLiveScroll;
            delete window.getLiveScroll;
            delete window.isFormatting;
            delete window.setFormatting;
            delete window.getFormatting;
        };
    }, [isLiveScroll, isFormatting]);

    const handleFormattingChange = (e) => {
        setIsFormatting(e.target.checked);
        window.dispatchEvent(new Event('formattingChanged'));
    };

    return (
        <div style={{
            display: 'flex',
            alignItems: 'center',
            height: '20px',
            backgroundColor: colorScheme.backgroundColor,
            borderTop: `1px solid ${colorScheme.borderColor}`,
            borderBottom: `1px solid ${colorScheme.borderColor}`,
            justifyContent: 'right',
            padding: '0 10px',
            fontSize: '12px',
            color: colorScheme.textColor
        }}>

            <button onClick={() => {
                window.hideAllFormattedContent();
            }} style={{ fontSize: '12px', marginRight: '10px', backgroundColor: 'transparent', border: '1px solid white', color: colorScheme.textColor, cursor: 'pointer' }}>Collapse All</button>
            <button onClick={() => {
                window.showAllFormattedContent();
            }} style={{ fontSize: '12px', marginRight: '10px', backgroundColor: 'transparent', border: '1px solid white', color: colorScheme.textColor, cursor: 'pointer' }}>Expand All</button>
            <label style={{ display: 'flex', alignItems: 'center', cursor: 'pointer' }}>
                <input
                    type="checkbox"
                    checked={isFormatting}
                    onChange={handleFormattingChange}
                    style={{ marginRight: '5px' }}
                />
                Format
            </label>
            <label style={{
                display: 'flex',
                alignItems: 'center',
                cursor: 'pointer',
                marginRight: '20px'
            }}>
                <input
                    type="checkbox"
                    checked={isLiveScroll}
                    onChange={(e) => setIsLiveScroll(e.target.checked)}
                    style={{ marginRight: '5px' }}
                />
                Live Scroll
            </label>
        </div>
    );
};