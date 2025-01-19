const { useState, useEffect, useRef } = React;

if (typeof window !== 'undefined') {
    window.buttonIndicators = {};
    window.buttonControls = {};
    window.toggleButtonControls = {};
}

const SplitButton = ({ label, onClick, dropdownItems = [], disabled, color = '#007bff', svgString, alternateLabel, alternateColor, title, hidden = false }) => {
    const { colorScheme } = window.useColorScheme();
    const [isOpen, setIsOpen] = useState(false);
    const [isVisible, setIsVisible] = useState(!hidden);
    const [isEnabled, setIsEnabled] = useState(!disabled);
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
        if (isOpen && buttonRef.current && dropdownRef.current) {
            const buttonRect = buttonRef.current.getBoundingClientRect();
            const dropdownRect = dropdownRef.current.getBoundingClientRect();
            const viewportWidth = window.innerWidth;
            const viewportHeight = window.innerHeight;

            let left = 0;
            let top = null;
            let bottom = null;

            // Check horizontal positioning
            if (buttonRect.left + dropdownRect.width > viewportWidth) {
                // If dropdown would overflow right side, align it to the right edge of the button
                left = buttonRect.width - dropdownRect.width;
            }

            // Check vertical positioning
            if (buttonRect.bottom + dropdownRect.height > viewportHeight) {
                // If dropdown would overflow bottom, position above the button
                bottom = '100%';
            } else {
                // Position below the button
                top = '100%';
            }

            setDropdownPosition({
                left: `${left}px`,
                top: top !== null ? top : 'auto',
                bottom: bottom !== null ? bottom : 'auto'
            });
        }
    }, [isOpen]);

    useEffect(() => {
        // Expose enable/disable methods to window
        if (typeof window !== 'undefined') {
            window.buttonControls[label] = {
                enable: () => setIsEnabled(true),
                disable: () => setIsEnabled(false),
                show: () => setIsVisible(true),
                hide: () => setIsVisible(false)
            };
        }

        return () => {
            if (typeof window !== 'undefined' && window.buttonControls) {
                delete window.buttonControls[label];
            }
        };
    }, [label]);


    const buttonStyle = {
        backgroundColor: currentColor,
        color: colorScheme.buttonTextColor,
        border: colorScheme.buttonBorder ? colorScheme.buttonBorder : 'none',
        borderRadius: colorScheme.borderRadius ? colorScheme.borderRadius : '3px',
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
        borderRadius: hasSplit ? '4px 0 0 4px' : (colorScheme.borderRadius ? colorScheme.borderRadius : '4px'),
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
        minWidth: '150px',
        backgroundColor: colorScheme.dropdownBackgroundColor,
        border: `1px solid ${colorScheme.textColor}`,
        borderRadius: '4px',
        boxShadow: '0 2px 5px rgba(0,0,0,0.1)',
        zIndex: 1000,
    };

    const dropdownItemStyle = {
        display: 'block',
        width: '100%',
        padding: '4px 6px',
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

    // If button is not visible, don't render anything
    if (!isVisible) return null;

    return (
        <div style={{ display: 'inline-flex', position: 'relative', padding: '4px' }} id={uniqueId} ref={buttonRef}>
            <button
                style={mainButtonStyle}
                onClick={onClick}
                disabled={!isEnabled}
                title={title}
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
                        disabled={!isEnabled}
                        title={title}
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

const ToggleSplitButton = ({ label, onToggle, dropdownItems = [], disabled, color = '#007bff', svgString, title, hidden = false }) => {
    const hasSplit = dropdownItems.length > 0;
    const [mainState, setMainState] = useState(false);
    const [itemStates, setItemStates] = useState(dropdownItems.map(() => false));
    const [isVisible, setIsVisible] = useState(!hidden);
    const [isEnabled, setIsEnabled] = useState(!disabled);

    // Add this line to make the states accessible globally
    window[`splitButtonState_${label}`] = { mainState, itemStates };

    useEffect(() => {
        // Expose enable/disable methods to window for ToggleSplitButton
        if (typeof window !== 'undefined') {
            window.toggleButtonControls[label] = {
                enable: () => setIsEnabled(true),
                disable: () => setIsEnabled(false),
                show: () => setIsVisible(true),
                hide: () => setIsVisible(false)
            };
        }

        return () => {
            if (typeof window !== 'undefined' && window.toggleButtonControls) {
                delete window.toggleButtonControls[label];
            }
        };
    }, [label]);


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
                window.setSendButtonLabel && window.setSendButtonLabel("Send Using Tools");
            } else window.setSendButtonLabel && window.setSendButtonLabel("Send");

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

    // If button is not visible, don't render anything
    if (!isVisible) return null;

    return (
        <SplitButton
            label={mainLabel}
            onClick={hasSplit ? undefined : handleMainToggle}
            dropdownItems={modifiedDropdownItems}
            disabled={!isEnabled}
            color={color}
            svgString={svgString}
            title={title}
            hidden={hidden}
        />
    );
};
