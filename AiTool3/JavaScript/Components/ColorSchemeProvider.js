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
    backgroundColor: '#1a1a1a',
    headerBackgroundColor: '#333333',
    inputBackgroundColor: '#2a2a2a',
    buttonBackgroundColor: '#4a7c4c',
    dropdownBackgroundColor: '#2a2a2a',
    messageUserBackgroundColor: '#1e3a5f',
    messageAIBackgroundColor: '#2a2a2a',
    messageRootBackgroundColor: '#1a1a2e',
    codeBlockBackgroundColor: '#333333',
    codeBlockHeaderBackgroundColor: '#444444',
    scrollbarBackgroundColor: '#333333',
    toolbarBackgroundColor: '#2a2a2a',
    toolbarButtonBackgroundColor: '#4a617c',
    selectedItemBackgroundColor: '#3a3a3a',
    textColor: '#ffffff',
    headerTextColor: '#ffffff',
    inputTextColor: '#ffffff',
    buttonTextColor: '#ffffff',
    dropdownTextColor: '#ffffff',
    messageUserTextColor: '#ffffff',
    messageAITextColor: '#ffffff',
    messageRootTextColor: '#ffffff',
    codeBlockTextColor: '#ffffff',
    codeBlockHeaderTextColor: '#ffffff',
    toolbarButtonTextColor: '#ffffff',
    selectedItemTextColor: '#4a7c4c',
    linkColor: '#6ca6ff',
    buttonDisabledBackgroundColor: '#555555',
    buttonDisabledTextColor: '#999999',
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
