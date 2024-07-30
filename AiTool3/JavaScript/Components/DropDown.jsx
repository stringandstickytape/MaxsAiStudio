// DropDown.js
const DropDown = ({ id, label, options, value, onChange, helpText }) => {
    const { colorScheme } = React.useColorScheme();

    const handleChange = (e) => {
        const selectedValue = e.target.value;
        const selectedText = e.target.options[e.target.selectedIndex].text;

        // Call the original onChange function
        onChange(selectedValue);

        // Post message to chrome.webview
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({
                type: 'dropdownChanged',
                id: id,
                content: selectedText
            });
        }
    };

    const dropdownStyle = {
        color: colorScheme.dropdownTextColor,
        backgroundColor: colorScheme.dropdownBackgroundColor,
    };

    const labelStyle = {
        color: colorScheme.textColor,
        display: 'flex',
        alignItems: 'center',
        marginBottom: '5px',
    };

    const helpIconStyle = {
        marginLeft: '5px',
        cursor: 'help',
        fontSize: '14px',
        color: colorScheme.textColor,
    };

    return (
        <div className="dropdown-container">
            <div style={labelStyle}>
                <label htmlFor={id}>{label}</label>

            </div>
            <select
                id={id}
                value={value}
                onChange={handleChange}
                title={helpText}
                style={dropdownStyle}
            >
                {options.map((option, index) => (
                    <option key={index} value={option}>
                        {option}
                    </option>
                ))}
            </select>
        </div>
    );
};