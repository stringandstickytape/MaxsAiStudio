// note no imports or exports needed because of calling context
const { useState, useRef, useEffect } = React;

const AiStudioButton = ({ label, color, dropdownItems }) => {
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef(null);

    const toggleDropdown = () => {
        setIsOpen(!isOpen);
    };

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
                    .ai-studio-button-container {
                        position: relative;
                        display: inline-block;
                    }
                    .ai-studio-button {
                        background-color: ${color};
                        color: white;
                        padding: 10px 15px;
                        border: none;
                        cursor: pointer;
                    }
                    .ai-studio-dropdown {
                        position: absolute;
                        top: 100%;
                        left: 0;
                        background-color: #f9f9f9;
                        min-width: 160px;
                        box-shadow: 0px 8px 16px 0px rgba(0,0,0,0.2);
                        z-index: 1;
                    }
                    .ai-studio-dropdown-item {
                        color: black;
                        padding: 12px 16px;
                        text-decoration: none;
                        display: block;
                        border: none;
                        width: 100%;
                        text-align: left;
                        background-color: transparent;
                        cursor: pointer;
                    }
                    .ai-studio-dropdown-item:hover {
                        background-color: #f1f1f1;
                    }
                `}
            </style>
            <div className="ai-studio-button-container" ref={dropdownRef}>
                <button
                    className="ai-studio-button"
                    onClick={toggleDropdown}
                >
                    {label}
                </button>
                {isOpen && dropdownItems && dropdownItems.length > 0 && (
                    <div className="ai-studio-dropdown">
                        {dropdownItems.map((item, index) => (
                            <button
                                key={index}
                                className="ai-studio-dropdown-item"
                                onClick={() => setIsOpen(false)}
                            >
                                {item}
                            </button>
                        ))}
                    </div>
                )}
            </div>
        </>
    );
};