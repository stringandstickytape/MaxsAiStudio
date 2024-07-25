const { useState, useEffect, useRef } = React;

const SplitButton = ({ label, onClick, dropdownItems = [], disabled, color = '#007bff', svgString, alternateLabel, alternateColor }) => {
    const { colorScheme } = window.useColorScheme();
    //const { colorScheme } = useColorScheme();
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef(null);
    const hasSplit = dropdownItems.length > 0;
    const uniqueId = useRef(`split-button-${Math.random().toString(36).substr(2, 9)}`).current;

    const currentLabel = alternateLabel || label;
    const currentColor = alternateColor || colorScheme.buttonBackgroundColor;

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                setIsOpen(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, []);

    const buttonStyle = {
        backgroundColor: currentColor,
        color: colorScheme.buttonTextColor,
        border: 'none',
        padding: '8px 8px',
        cursor: 'pointer',
        transition: 'background-color 0.3s',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
    };

    const mainButtonStyle = {
        ...buttonStyle,
        flexGrow: 1,
        textAlign: 'center',
        borderRadius: hasSplit ? '4px 0 0 4px' : '4px',
    };

    const arrowButtonStyle = {
        ...buttonStyle,
        borderRadius: '0 4px 4px 0',
        padding: '8px 8px',
        borderLeft: `3px solid ${colorScheme.backgroundColor}`,
    };

    const dropdownStyle = {
        position: 'absolute',
        left: '-44px',
        bottom: '15px',
        backgroundColor: colorScheme.dropdownBackgroundColor,
        border: `1px solid ${colorScheme.textColor}`,
        borderRadius: '4px',
        boxShadow: '0 2px 5px rgba(0,0,0,0.1)',
        zIndex: 1000,
    };

    const dropdownItemStyle = {
        display: 'block',
        width: '100%',
        padding: '10px 15px',
        textAlign: 'left',
        background: 'none',
        border: 'none',
        cursor: 'pointer',
        color: colorScheme.dropdownTextColor,
    };


    const svgStyle = {
        width: '20px',
        height: '20px',
        backgroundColor: 'transparent',
    };

    const labelStyle = {
        fontSize: svgString ? '0.8em' : '1em',
    };

    return (
        <div style={{ display: 'inline-flex', position: 'relative', padding: '4px' }} id={uniqueId}>
            <button
                style={mainButtonStyle}
                onClick={onClick}
                disabled={disabled}
            >
                {svgString && (
                    <div
                        style={svgStyle}
                        dangerouslySetInnerHTML={{ __html: svgString }}
                    />
                )}
                <span style={labelStyle}>{currentLabel}</span>
            </button>
            {hasSplit && (
                <>
                    <button
                        style={arrowButtonStyle}
                        onClick={() => setIsOpen(!isOpen)}
                        disabled={disabled}
                        //onMouseEnter={(e) => e.target.style.backgroundColor = adjustColor(color, -20)}
                        //onMouseLeave={(e) => e.target.style.backgroundColor = color}
                    >
                        ▼
                    </button>
                    {isOpen && (
                        <div style={dropdownStyle} ref={dropdownRef}>
                            {dropdownItems.map((item, index) => (
                                <button
                                    key={index}
                                    style={dropdownItemStyle}
                                    onClick={() => {
                                        item.onClick();
                                        setIsOpen(false);
                                    }}
                                    //onMouseEnter={(e) => e.target.style.backgroundColor = '#f0f0f0'}
                                    //onMouseLeave={(e) => e.target.style.backgroundColor = 'white'}
                                >
                                    {item.label}
                                </button>
                            ))}
                        </div>
                    )}
                </>
            )}
        </div>
    );
};

// Helper function to adjust color brightness
function adjustColor(color, amount) {
    return '#' + color.replace(/^#/, '').replace(/../g, color => ('0' + Math.min(255, Math.max(0, parseInt(color, 16) + amount)).toString(16)).substr(-2));
}

const ToggleSplitButton = ({ label, onToggle, dropdownItems = [], disabled, color = '#007bff', svgString }) => {
    const hasSplit = dropdownItems.length > 0;
    const [mainState, setMainState] = useState(false);
    const [itemStates, setItemStates] = useState(dropdownItems.map(() => false));

    const handleMainToggle = () => {
        setMainState(prevState => !prevState);
        const newState = !mainState;
        console.log('Main button clicked. New state:', newState);
        onToggle(-1, newState);
    };

    const handleItemToggle = (index, item) => {
        setItemStates(prevStates => {
            const newStates = [...prevStates];
            newStates[index] = !newStates[index];
            const newState = newStates[index];
            console.log(`${item.label} clicked. New state: ${newState ? 'checked' : 'unchecked'}`);
            onToggle(index, newState);
            item.onClick && item.onClick(newState);
            return newStates;
        });
    };

    const modifiedDropdownItems = hasSplit
        ? dropdownItems.map((item, index) => ({
            ...item,
            label: `${itemStates[index] ? '☑' : '☐'} ${item.label}`,
            onClick: () => handleItemToggle(index, item)
        }))
        : [];

    const mainLabel = hasSplit
        ? label
        : `${mainState ? '☑' : '☐'} ${label}`;

    return (
        <SplitButton
            label={mainLabel}
            onClick={hasSplit ? undefined : handleMainToggle}
            dropdownItems={modifiedDropdownItems}
            disabled={disabled}
            color={color}
            svgString={svgString}
        />
    );
};
                                /* voice svg
                                     <svg viewBox="0 0 300 100" xmlns="http://www.w3.org/2000/svg">
            <rect x="15" y="42" width="10" height="16" rx="5" fill="black" />
            <rect x="35" y="25" width="10" height="50" rx="5" fill="black" />
            <rect x="55" y="2" width="10" height="96" rx="5" fill="black" />
            <rect x="75" y="18" width="10" height="64" rx="5" fill="black" />
            <rect x="95" y="38" width="10" height="24" rx="5" fill="black" />
            <rect x="115" y="32" width="10" height="36" rx="5" fill="black" />
            <rect x="135" y="12" width="10" height="76" rx="5" fill="black" />
            <rect x="155" y="2" width="10" height="96" rx="5" fill="black" />
            <rect x="175" y="22" width="10" height="56" rx="5" fill="black" />
            <rect x="195" y="30" width="10" height="40" rx="5" fill="black" />
            <rect x="215" y="15" width="10" height="70" rx="5" fill="black" />
            <rect x="235" y="35" width="10" height="30" rx="5" fill="black" />
            <rect x="255" y="42" width="10" height="16" rx="5" fill="black" />
        </svg>*/