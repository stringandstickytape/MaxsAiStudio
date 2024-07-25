debugger;

// ColorSchemeProvider.js
const { createContext, useState, useContext } = React;

const ColorSchemeContext = createContext();

const lightScheme = {
    backgroundColor: '#ffffff',
    headerBackgroundColor: '#f0f0f0',
    inputBackgroundColor: '#ffffff',
    buttonBackgroundColor: '#007bff',
    dropdownBackgroundColor: '#ffffff',
    messageUserBackgroundColor: '#e6f2ff',
    messageAIBackgroundColor: '#f0f0f0',
    messageRootBackgroundColor: '#fff8e1',
    codeBlockBackgroundColor: '#f8f8f8',
    codeBlockHeaderBackgroundColor: '#e8e8e8',
    scrollbarBackgroundColor: '#f0f0f0',
    toolbarBackgroundColor: '#f8f8f8',
    toolbarButtonBackgroundColor: '#e0e0e0',
    selectedItemBackgroundColor: '#e6f2ff',
    textColor: '#333333',
    headerTextColor: '#000000',
    inputTextColor: '#333333',
    buttonTextColor: '#ffffff',
    dropdownTextColor: '#333333',
    messageUserTextColor: '#333333',
    messageAITextColor: '#333333',
    messageRootTextColor: '#333333',
    codeBlockTextColor: '#333333',
    codeBlockHeaderTextColor: '#333333',
    toolbarButtonTextColor: '#333333',
    selectedItemTextColor: '#007bff',
    linkColor: '#0056b3',
    buttonDisabledBackgroundColor: '#cccccc',
    buttonDisabledTextColor: '#666666',
};

const darkScheme = {
    backgroundColor: '#0E1C4D', // Dark blue
    headerBackgroundColor: '#1C3A9F', // Slightly lighter blue
    inputBackgroundColor: '#2A4CC6', // Medium blue
    buttonBackgroundColor: '#FFFFFF', // White
    dropdownBackgroundColor: '#2A4CC6', // Medium blue
    messageUserBackgroundColor: '#FFFFFF', // White
    messageAIBackgroundColor: '#1C3A9F', // Slightly lighter blue
    messageRootBackgroundColor: '#0E1C4D', // Dark blue
    codeBlockBackgroundColor: '#1C3A9F', // Slightly lighter blue
    codeBlockHeaderBackgroundColor: '#2A4CC6', // Medium blue
    scrollbarBackgroundColor: '#1C3A9F', // Slightly lighter blue
    toolbarBackgroundColor: '#2A4CC6', // Medium blue
    toolbarButtonBackgroundColor: '#FFFFFF', // White
    selectedItemBackgroundColor: '#3B5DE8', // Lighter blue
    textColor: '#FFFFFF', // White
    headerTextColor: '#FFFFFF', // White
    inputTextColor: '#FFFFFF', // White
    buttonTextColor: '#0E1C4D', // Dark blue
    dropdownTextColor: '#FFFFFF', // White
    messageUserTextColor: '#0E1C4D', // Dark blue
    messageAITextColor: '#FFFFFF', // White
    messageRootTextColor: '#FFFFFF', // White
    codeBlockTextColor: '#FFFFFF', // White
    codeBlockHeaderTextColor: '#FFFFFF', // White
    toolbarButtonTextColor: '#0E1C4D', // Dark blue
    selectedItemTextColor: '#FFFFFF', // White
    linkColor: '#FFFFFF', // White
    buttonDisabledBackgroundColor: '#7A8CD9', // Light blue
    buttonDisabledTextColor: '#B8C2E8', // Very light blue
};


const ColorSchemeProvider = ({ children }) => {
    const [colorScheme, setColorScheme] = useState(darkScheme);

    const toggleColorScheme = () => {
        setColorScheme(prevScheme => prevScheme === lightScheme ? darkScheme : lightScheme);
    };

    window.toggleColorScheme = toggleColorScheme;

    return (
        <ColorSchemeContext.Provider value={{ colorScheme, toggleColorScheme }}>
            {children}
        </ColorSchemeContext.Provider>
    );

    
};

const useColorScheme = () => useContext(ColorSchemeContext);

window.ColorSchemeProvider = ColorSchemeProvider;
window.useColorScheme = () => useContext(ColorSchemeContext);
React.useColorScheme = useColorScheme;
