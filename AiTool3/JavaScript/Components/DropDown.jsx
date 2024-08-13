const DropDown = ({ id, label, options, value, onChange, helpText, columnData, starredModels, onStarToggle }) => {
    React.useEffect(() => {
        if (columnData && columnData.length > 0) {
            const initialStarredModels = {};
            columnData.forEach((data, index) => {
                if (data.starred) {
                    initialStarredModels[options[index]] = true;
                }
            });
            onStarToggle(initialStarredModels);
        }
    }, [columnData]);
    const [sortedOptions, sortedColumnData] = React.useMemo(() => {
        const optionsWithIndex = options.map((option, index) => ({ option, index }));
        const sorted = optionsWithIndex.sort((a, b) => {
            if (starredModels[a.option] && !starredModels[b.option]) return -1;
            if (!starredModels[a.option] && starredModels[b.option]) return 1;
            return a.option.localeCompare(b.option);
        });
        const sortedOptions = sorted.map(item => item.option);
        const sortedColumnData = sorted.map(item => columnData[item.index]);
        return [sortedOptions, sortedColumnData];
    }, [options, starredModels, columnData]);
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

    const toggleStar = (event, modelName) => {
        event.stopPropagation();
        onStarToggle(modelName);
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
        padding: '3px 3px',
        borderBottom: `1px solid ${colorScheme.borderColor}`,
    };

    const costStyle = {
        padding: '5px 3px',
        borderBottom: `1px solid ${colorScheme.borderColor}`,
        textAlign: 'center'
    };

    const starStyle = {
        cursor: 'pointer',
        fontSize: '20px',
        color: 'goldenrod',

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
                                    <th style={cellStyle}></th>
                                    <th style={cellStyle}>Model</th>
                                    <th style={costStyle}>Input Cost</th>
                                    <th style={costStyle}>Output Cost</th>
                                </tr>
                            </thead>
                            <tbody>
                                {sortedOptions.map((option, index) => (
                                    <tr
                                        key={index}
                                        style={optionStyle}
                                        onClick={() => handleSelect(option, option)}
                                    >
                                        <td style={cellStyle}>
                                            <span
                                                style={starStyle}
                                                onClick={(e) => toggleStar(e, option)}
                                            >
                                                {starredModels[option] ? '★' : '☆'}
                                            </span>
                                        </td>
                                        <td style={cellStyle}>{option}</td>
                                        <td style={costStyle}>{sortedColumnData && sortedColumnData[index] ? sortedColumnData[index].inputCost : ''}</td>
                                        <td style={costStyle}>{sortedColumnData && sortedColumnData[index] ? sortedColumnData[index].outputCost : ''}</td>
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