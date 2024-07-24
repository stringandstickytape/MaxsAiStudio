// DropDown.js
const DropDown = ({ id, label, options, value, onChange }) => {
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

    return (
        <div className="dropdown-container">
            <label>{label}</label>
            <select id={id} value={value} onChange={handleChange}>
                {options.map((option, index) => (
                    <option key={index} value={option}>
                        {option}
                    </option>
                ))}
            </select>
        </div>
    );
};