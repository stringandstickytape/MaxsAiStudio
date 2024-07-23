const SplitButton = ({ label, onClick, dropdownItems, disabled }) => {
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef(null);

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
        <div className="split-button-container">
            <button className="split-button-main" onClick={onClick} disabled={disabled}>
                {label}
            </button>
            <button className="split-button-arrow" onClick={() => setIsOpen(!isOpen)} disabled={disabled}>
                ▼
            </button>
            {isOpen && (
                <div className="split-button-dropdown">
                    {dropdownItems.map((item, index) => (
                        <button
                            key={index}
                            className="split-button-dropdown-item"
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
        </div>
    );
};