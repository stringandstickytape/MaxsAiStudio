// ToolsDropdown.js
const { useState, useEffect, useRef } = React;
const { useColorScheme } = React;

const ToolsDropdown = () => {
    const [isOpen, setIsOpen] = useState(false);
    const [tools, setTools] = useState([
        { id: 'tool-1', name: 'Find-and-Replace', isSelected: false },
        { id: 'tool-2', name: 'Tool2', isSelected: false }
    ]);
    const dropdownRef = useRef(null);
    const { colorScheme } = useColorScheme();

    const toggleTool = (id) => {
        setTools(tools.map(tool =>
            tool.id === id ? { ...tool, isSelected: !tool.isSelected } : tool
        ));
    };

    const getSelectedTools = () => {
        return tools.filter(tool => tool.isSelected).map(tool => tool.id).join(',');
    };

    // Export method to get selected tools
    window.getSelectedTools = getSelectedTools;

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

    return (
        <>
            <style>
                {`
                    .tools-dropdown {
                        position: relative;
                        display: inline-block;
                    }
                    .tools-dropdown-button {
                        background-color: ${colorScheme.buttonBackgroundColor};
                        background: ${colorScheme.buttonBackgroundCss};
                        color: ${colorScheme.buttonTextColor};
                        padding: 5px 7px;
                        border: none;
                        cursor: pointer;
                        border-radius: 4px;
                    }
                    .tools-dropdown-content {
                        display: ${isOpen ? 'block' : 'none'};
                        position: absolute;
                        background-color: ${colorScheme.dropdownBackgroundColor};
                        min-width: 160px;
                        box-shadow: 0px 8px 16px 0px rgba(0,0,0,0.5);
                        z-index: 1;
                        border-radius: 4px;
                    }
                    .tools-dropdown-item {
                        color: ${colorScheme.dropdownTextColor};
                        padding: 6px 8px;
                        text-decoration: none;
                        display: block;
                        border: none;
                        width: 100%;
                        text-align: left;
                        background-color: transparent;
                        cursor: pointer;
                    }
                    .tools-dropdown-item:hover {
                        background-color: ${colorScheme.selectedItemBackgroundColor};
                    }
                    .tool-selected {
                        background-color: ${colorScheme.selectedItemBackgroundColor};
                        color: ${colorScheme.selectedItemTextColor};
                    }
                    .tool-selected::before {
                        content: ' ★';
                        color: gold;
                    }
                `}
            </style>
            <div className="tools-dropdown" ref={dropdownRef}>
                <button className="tools-dropdown-button" onClick={() => setIsOpen(!isOpen)}>
                    Tools ▼
                </button>
                <div className="tools-dropdown-content">
                    {tools.map(tool => (
                        <button
                            key={tool.id}
                            className={`tools-dropdown-item ${tool.isSelected ? 'tool-selected' : ''}`}
                            onClick={() => toggleTool(tool.id)}
                        >
                            {tool.name}
                        </button>
                    ))}
                </div>
            </div>
        </>
    );
};