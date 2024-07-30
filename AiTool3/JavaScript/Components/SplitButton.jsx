const { useState, useEffect, useRef } = React;

if (typeof window !== 'undefined') {
    window.buttonIndicators = {};
}

const SplitButton = ({ label, onClick, dropdownItems = [], disabled, color = '#007bff', svgString, alternateLabel, alternateColor, title }) => {
    const { colorScheme } = window.useColorScheme();
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef(null);
    const hasSplit = dropdownItems.length > 0;
    const uniqueId = useRef(`split-button-${Math.random().toString(36).substr(2, 9)}`).current;

    const currentLabel = alternateLabel || label;
    const currentColor = colorScheme.buttonBackgroundColor;

    const [dropdownPosition, setDropdownPosition] = useState({ left: '-44px', bottom: '15px' });
    const buttonRef = useRef(null);

    const [indicator, setIndicator] = useState(null);

    useEffect(() => {
        const updateIndicator = () => {
            setIndicator(window.buttonIndicators[label] || null);
        };

        // Initial update
        updateIndicator();

        // Set up event listener
        window.addEventListener(`indicatorUpdate_${label}`, updateIndicator);

        return () => {
            window.removeEventListener(`indicatorUpdate_${label}`, updateIndicator);
        };
    }, [label]);

    useEffect(() => {
        if (!window.addIndicator) {
            window.addIndicator = (buttonLabel, indicatorColor) => {
                window.buttonIndicators[buttonLabel] = indicatorColor;
                window.dispatchEvent(new Event(`indicatorUpdate_${buttonLabel}`));
            };
        }

        if (!window.clearIndicator) {
            window.clearIndicator = (buttonLabel) => {
                delete window.buttonIndicators[buttonLabel];
                window.dispatchEvent(new Event(`indicatorUpdate_${buttonLabel}`));
            };
        }
    }, []);

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

    useEffect(() => {
        if (isOpen && buttonRef.current) {
            const buttonRect = buttonRef.current.getBoundingClientRect();

            const dropdownWidth = 200; // Approximate width of the dropdown
            const dropdownHeight = dropdownItems.length * 40; // Approximate height of the dropdown

            if (buttonRect.left - dropdownWidth < 0 || buttonRect.top - dropdownHeight < 0) {
                // If dropdown would appear off-screen to the left or top, position it to the bottom-right
                setDropdownPosition({ left: '0', top: '100%' });
            } else {
                // Otherwise, keep the original position
                setDropdownPosition({ left: '-44px', bottom: '15px' });
            }
        }
    }, [isOpen, dropdownItems.length]);

    const buttonStyle = {
        backgroundColor: currentColor,
        color: colorScheme.buttonTextColor,
        border: '1px solid black',
        padding: '4px 4px',
        cursor: 'pointer',
        transition: 'background-color 0.3s',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
    };

    if (colorScheme.buttonBackgroundCss && colorScheme.buttonBackgroundCss.length > 0) buttonStyle.background = colorScheme.buttonBackgroundCss;

    const mainButtonStyle = {
        ...buttonStyle,
        flexGrow: 1,
        textAlign: 'center',
        borderRadius: hasSplit ? '4px 0 0 4px' : '4px',
    };

    const arrowButtonStyle = {
        ...buttonStyle,
        borderRadius: '0 4px 4px 0',
        padding: '4px 4px',
        borderLeft: `3px solid ${colorScheme.backgroundColor}`,
    };

    const dropdownStyle = {
        position: 'absolute',
        ...dropdownPosition,
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

    const indicatorStyle = {
        position: 'absolute',
        top: '10px',
        right: '10px',
        width: '25%',
        height: '25%',
        borderRadius: '50%',
        backgroundColor: indicator,
        border: '1px solid black'
    };

    return (
        <div style={{ display: 'inline-flex', position: 'relative', padding: '4px' }} id={uniqueId} ref={buttonRef}>
            <button
                style={mainButtonStyle}
                onClick={onClick}
                disabled={disabled}
                title={title} // Added title prop here
            >
                {svgString && (
                    <div
                        style={svgStyle}
                        dangerouslySetInnerHTML={{ __html: svgString }}
                    />
                )}
                <span style={labelStyle}>{currentLabel}</span>
                {indicator && <div style={indicatorStyle} />}
            </button>
            {hasSplit && (
                <>
                    <button
                        style={arrowButtonStyle}
                        onClick={() => setIsOpen(!isOpen)}
                        disabled={disabled}
                        title={title} // Added title prop here
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

const ToggleSplitButton = ({ label, onToggle, dropdownItems = [], disabled, color = '#007bff', svgString, title }) => {
    const hasSplit = dropdownItems.length > 0;
    const [mainState, setMainState] = useState(false);
    const [itemStates, setItemStates] = useState(dropdownItems.map(() => false));

    // Add this line to make the states accessible globally
    window[`splitButtonState_${label}`] = { mainState, itemStates };

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

            // are any items checked?
            const anyChecked = newStates.some(state => state);
            if (anyChecked) {
                window.setSendButtonLabel("Send Using Tools");
            } else window.setSendButtonLabel("Send");

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
            title={title} // Pass title prop here
        />
    );
};