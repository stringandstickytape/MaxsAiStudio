const CustomContextMenu = ({ x, y, items, onClose }) => {
    const { colorScheme } = React.useColorScheme();
    const menuRef = React.useRef(null);

    React.useEffect(() => {
        const handleClickOutside = (event) => {
            if (menuRef.current && !menuRef.current.contains(event.target)) {
                onClose();
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, [onClose]);

    return (
        <div
            ref={menuRef}
            style={{
                position: 'fixed',
                top: y,
                left: x,
                backgroundColor: colorScheme.dropdownBackgroundColor,
                border: `1px solid ${colorScheme.borderColor}`,
                borderRadius: colorScheme.borderRadius || '4px',
                boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
                zIndex: 1000,
                minWidth: '150px',
            }}
        >
            {items.map((item, index) => (
                <div
                    key={index}
                    onClick={() => {
                        item.onClick();
                        onClose();
                    }}
                    style={{
                        padding: '8px 12px',
                        cursor: 'pointer',
                        color: colorScheme.dropdownTextColor,
                        transition: 'background-color 0.2s',
                        ':hover': {
                            backgroundColor: colorScheme.selectedItemBackgroundColor,
                        },
                    }}
                >
                    {item.label}
                </div>
            ))}
            <div
                onClick={() => {
                    alert("Hold CTRL while right-clicking to show the default menu.");
                    onClose();
                }}
                style={{
                    padding: '8px 12px',
                    cursor: 'pointer',
                    color: colorScheme.dropdownTextColor,
                    borderTop: `1px solid ${colorScheme.borderColor}`,
                    transition: 'background-color 0.2s',
                    ':hover': {
                        backgroundColor: colorScheme.selectedItemBackgroundColor,
                    },
                }}
            >
                Show Default Menu
            </div>
        </div>
    );
};