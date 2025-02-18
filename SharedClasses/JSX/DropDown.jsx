const DropDown = ({ id, label, options, value, onChange, helpText, columnData, starredModels, onStarToggle }) => {
    const [filterText, setFilterText] = React.useState('');
    const [isNotesModalOpen, setIsNotesModalOpen] = useState(false);
    const [editingModelGuid, setEditingModelGuid] = useState(null);
    const [editingModelNotes, setEditingModelNotes] = useState('');

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

    const filteredOptionsAndData = React.useMemo(() => {
        const tokens = filterText.toLowerCase().split(' ').filter(token => token.trim() !== '');
        return sortedOptions.reduce((acc, option, index) => {
            const columnDataValues = sortedColumnData[index] ? Object.values(sortedColumnData[index]).join(' ').toLowerCase() : '';
            const optionLower = option.toLowerCase();
            if (tokens.length === 0 || tokens.every(token => optionLower.includes(token) || columnDataValues.includes(token))) {
                acc.options.push(option);
                acc.columnData.push(sortedColumnData[index]);
            }
            return acc;
        }, { options: [], columnData: [] });
    }, [sortedOptions, sortedColumnData, filterText]);

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

    const handleNotesEdit = (modelGuid, currentNotes) => {
        setEditingModelGuid(modelGuid);
        setEditingModelNotes(currentNotes);
        setIsNotesModalOpen(true);
    };

    const handleNotesSave = (modelGuid, newNotes) => {
        // Send a message to the C# side to update the notes.
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({
                type: 'userNotesChanged',
                modelGuid: modelGuid,
                content: newNotes
            });
        }

        // Update the tooltip and data model with the new notes.
        const updatedColumnData = columnData.map(data => {
            if (data.modelGuid === modelGuid) {
                return { ...data, userNotes: newNotes };
            } else {
                return data;
            }
        });
        onStarToggle({
            ...starredModels,
            columnData: updatedColumnData, 
        });

        setIsNotesModalOpen(false);
        // Optionally update local state of notes, or re-fetch
    };

    const handleNotesClose = () => {
        setIsNotesModalOpen(false);
        setEditingModelGuid(null); // Clear the editing guid
        setEditingModelNotes('');   // Clear the editing notes
    }

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
        fontSize: '12px',
        border: `1px solid ${colorScheme.borderColor}`
    };

    const selectedStyle = {
        color: colorScheme.dropdownTextColor,
        backgroundColor: colorScheme.dropdownBackgroundColor,
        padding: '6px 10px',
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
        fontSize: '10px',
        zIndex: 1000,
        padding: '6px',
    };

    const scrollableContainerStyle = {
        maxHeight: '400px',
        overflowY: 'auto',
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
        padding: '3px 0px 0px 3px',
        borderBottom: `1px solid ${colorScheme.borderColor}`,
    };

    const costStyle = {
        padding: '5px 3px',
        borderBottom: `1px solid ${colorScheme.borderColor}`,
        textAlign: 'center'
    };

    const starStyle = {
        cursor: 'pointer',
        fontSize: '30px',
        color: 'goldenrod',
    };

    const filterInputStyle = {
        width: '100%',
        padding: '4px',
        border: `1px solid ${colorScheme.borderColor}`,
        borderRadius: '4px',
        boxSizing: 'border-box',
        backgroundColor: colorScheme.dropdownBackgroundColor,
        color: colorScheme.dropdownTextColor,
    };
    const modelCardStyle = {
        border: `1px solid ${colorScheme.borderColor}`,
        borderRadius: '4px',
        padding: '6px',
        margin: '4px 0',
        backgroundColor: colorScheme.dropdownBackgroundColor,
        cursor: 'pointer',
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
                    <span style={{ marginLeft: '8px', fontSize: '12px' }}>▼</span>
                </div>
                {isOpen && (
                    <div style={optionsStyle}>
                        <input
                            type="text"
                            placeholder="Filter options..."
                            value={filterText}
                            onChange={(e) => setFilterText(e.target.value)}
                            style={filterInputStyle}
                            onClick={(e) => e.stopPropagation()}
                        />
                        <div style={scrollableContainerStyle}>
                            {filteredOptionsAndData.options.map((option, index) => (
                                <div
                                    key={index}
                                    style={modelCardStyle}
                                    onClick={() => handleSelect(option, option)}
                                >
                                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                        <span style={{ fontSize: '16px', fontWeight: 'bold' }}>{option}</span>
                                        <span
                                            style={starStyle}
                                            onClick={(e) => toggleStar(e, option)}
                                        >
                                            {starredModels[option] ? '★' : '☆'}
                                        </span>
                                    </div>
                                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '12px', marginTop: '4px' }}>
                                        <span>Provider: {filteredOptionsAndData.columnData[index]?.provider || ''}</span>
                                        <span style={{ textAlign: 'right' }}>Protocol: {filteredOptionsAndData.columnData[index]?.protocol || ''}</span>
                                    </div>
                                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '12px', marginTop: '4px' }}>
                                        <span>Input: {filteredOptionsAndData.columnData[index]?.inputCost || ''}</span>
                                        <span style={{textAlign: 'right'} }>Output: {filteredOptionsAndData.columnData[index]?.outputCost || ''}</span>
                                    </div>
                                    <div style={{ marginTop: '4px' }}>
                                        <span
                                            title={filteredOptionsAndData.columnData[index]?.userNotes || ''}
                                            style={{ textDecoration: 'underline', cursor: 'pointer', fontSize: '12px' }}
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                handleNotesEdit(filteredOptionsAndData.columnData[index]?.modelGuid || '', filteredOptionsAndData.columnData[index]?.userNotes || '');
                                            }}
                                        >
                                            Notes: {filteredOptionsAndData.columnData[index]?.userNotes || ''}
                                        </span>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                )}
            </div>
            {isNotesModalOpen && (
                <ModelNotesEditor
                    modelGuid={editingModelGuid}
                    initialNotes={editingModelNotes}
                    onSave={handleNotesSave}
                    onClose={handleNotesClose}
                />
            )}
        </div>

    );
};

//<td style={costStyle}>{filteredOptionsAndData.columnData[index] ? filteredOptionsAndData.columnData[index].modelGuid : ''}</td>