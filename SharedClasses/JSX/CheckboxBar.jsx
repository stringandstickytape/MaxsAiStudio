const CheckboxBar = () => {
    const { colorScheme } = React.useColorScheme();
    const [isLiveScroll, setIsLiveScroll] = React.useState(true);

    React.useEffect(() => {
        // Expose the live scroll state to the window object
        window.isLiveScroll = isLiveScroll;

        // Expose methods to control the checkbox
        window.setLiveScroll = (value) => setIsLiveScroll(value);
        window.getLiveScroll = () => isLiveScroll;

        return () => {
            delete window.isLiveScroll;
            delete window.setLiveScroll;
            delete window.getLiveScroll;
        };
    }, [isLiveScroll]);

    return (
        <div style={{
            height: '20px',
            backgroundColor: colorScheme.backgroundColor,
            borderTop: `1px solid ${colorScheme.borderColor}`,
            borderBottom: `1px solid ${colorScheme.borderColor}`,
            display: 'flex',
            alignItems: 'center',
            padding: '0 10px',
            fontSize: '12px',
            color: colorScheme.textColor
        }}>
            <label style={{
                display: 'flex',
                alignItems: 'center',
                cursor: 'pointer'
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

window.CheckboxBar = CheckboxBar;