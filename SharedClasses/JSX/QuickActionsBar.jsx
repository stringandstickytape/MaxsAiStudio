// QuickActionsBar.jsx

const QuickActionsBar = () => {
    const { colorScheme } = React.useColorScheme();
    const [buttons, setButtons] = React.useState([]);

    React.useEffect(() => {
        window.addQuickActionButton = (label, onClick, dropdownItems = [], svgString = null) => {
            setButtons(prevButtons => [
                ...prevButtons,
                { label, onClick, dropdownItems, svgString }
            ]);
        };

        window.removeQuickActionButton = (label) => {
            setButtons(prevButtons => prevButtons.filter(button => button.label !== label));
        };

        return () => {
            delete window.addQuickActionButton;
            delete window.removeQuickActionButton;
        };
    }, []);

    if (buttons.length === 0) {
        return null;
    }

    return (
        <div style={{
            display: 'flex',
            flexWrap: 'wrap',
            gap: '5px',
            padding: '5px',
            backgroundColor: colorScheme.backgroundColor,
            borderTop: `1px solid ${colorScheme.borderColor}`,
        }}>
            {buttons.map((button, index) => (
                <SplitButton
                    key={index}
                    label={button.label}
                    onClick={button.onClick}
                    dropdownItems={button.dropdownItems}
                    color={colorScheme.buttonBackgroundColor}
                    background={colorScheme.buttonBackgroundCss}
                    border={colorScheme.buttonBorder || 'none'}
                    borderRadius={colorScheme.borderRadius || '3px'}
                    svgString={button.svgString}
                />
            ))}
        </div>
    );
};
// Export the component
window.QuickActionsBar = QuickActionsBar;