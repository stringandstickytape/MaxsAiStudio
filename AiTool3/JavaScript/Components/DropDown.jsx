const DropDown = ({ id, label, options, value, onChange, helpText, columnData }) => {
    const { colorScheme } = React.useColorScheme();
    const [isOpen, setIsOpen] = React.useState(false);
    const dropdownRef = React.useRef(null);

    const handleSelect = (selectedValue, selectedText) => {
        onChange(selectedValue);
        setIsOpen(false);

        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({
                type: 'dropdownChanged',
                id: id,
                content: selectedText
            });
        }
    };

    React.useEffect(() => {
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

    const dropdownStyle = {
        position: 'relative',
        minWidth: '300px',
        fontSize: '13px',
        border: '1px solid ' + colorScheme.borderColor
    };

    const selectedStyle = {
        color: colorScheme.dropdownTextColor,
        backgroundColor: colorScheme.dropdownBackgroundColor,
        borderRadius: '4px',
        cursor: 'pointer',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
    };

    const optionsStyle = {
        position: 'absolute',
        top: '100%',
        left: 0,
        right: 0,
        backgroundColor: colorScheme.dropdownBackgroundColor,
        borderRadius: '4px',
        boxShadow: '0 2px 5px rgba(0,0,0,0.2)',
        maxHeight: '200px',
        overflowY: 'auto',
        zIndex: 1000,
    };

    const optionStyle = {
        padding: '5px 10px',
        cursor: 'pointer',
        color: colorScheme.dropdownTextColor,
    };

    const labelStyle = {
        color: colorScheme.textColor,
        display: 'flex',
        alignItems: 'center',
    };

    const tableStyle = {
        width: '100%',
        borderCollapse: 'collapse',
    };

    const cellStyle = {
        padding: '5px 10px',
        borderBottom: `1px solid ${colorScheme.borderColor}`,
    };

    const costStyle = {
        padding: '5px 10px',
        borderBottom: `1px solid ${colorScheme.borderColor}`,
        textAlign: 'center'
    };

    return (
        <div className="dropdown-container" ref={dropdownRef}>
            <div style={labelStyle}>
                <label htmlFor={id}>{label}</label>
            </div>
            <div style={dropdownStyle}>
                <div
                    style={selectedStyle}
                    onClick={() => setIsOpen(!isOpen)}
                    title={helpText}
                >
                    <span>{value}</span>
                    <span>▼</span>
                </div>
                {isOpen && (
                    <div style={optionsStyle}>
                        <table style={tableStyle}>
                            <thead>
                                <tr>
                                    <th style={cellStyle}>Model</th>
                                    <th style={costStyle}>Input Cost</th>
                                    <th style={costStyle}>Output Cost</th>
                                </tr>
                            </thead>
                            <tbody>
                                {options.map((option, index) => (
                                    <tr
                                        key={index}
                                        style={optionStyle}
                                        onClick={() => handleSelect(option, option)}
                                    >
                                        <td style={cellStyle}>{option}</td>
                                        <td style={costStyle}>{columnData && columnData[index] ? columnData[index].inputCost : ''}</td>
                                        <td style={costStyle}>{columnData && columnData[index] ? columnData[index].outputCost : ''}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
        </div>
    );
};