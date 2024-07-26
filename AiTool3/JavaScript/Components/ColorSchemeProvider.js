const { createContext, useState, useContext } = React;

const ColorSchemeContext = createContext();

const initialColorSchemes = {
    light: {
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
        messagesPaneBackgroundCss: 'linear-gradient(160deg, #1A1A1A 0%, #2C5F5C 100%)',
        mainContentBackgroundCss: 'linear-gradient(160deg, #ffffff 0%, #f0f0f0 100%)'
    },
    dark: {
        backgroundColor: '#121212',
        headerBackgroundColor: '#1E1E1E',
        inputBackgroundColor: '#1A1A1A',
        buttonBackgroundColor: '#2C5F5C',
        dropdownBackgroundColor: '#1A1A1A',
        messageUserBackgroundColor: '#1E2A3D',
        messageAIBackgroundColor: '#1A1A1A',
        messageRootBackgroundColor: '#151C2C',
        codeBlockBackgroundColor: '#1E1E1E',
        codeBlockHeaderBackgroundColor: '#252525',
        scrollbarBackgroundColor: '#1E1E1E',
        toolbarBackgroundColor: '#1A1A1A',
        toolbarButtonBackgroundColor: '#2A3F5F',
        selectedItemBackgroundColor: '#202020',
        textColor: '#E0E0E0',
        headerTextColor: '#FFFFFF',
        inputTextColor: '#E0E0E0',
        buttonTextColor: '#FFFFFF',
        dropdownTextColor: '#E0E0E0',
        messageUserTextColor: '#E6E6E6',
        messageAITextColor: '#E0E0E0',
        messageRootTextColor: '#E6E6E6',
        codeBlockTextColor: '#E0E0E0',
        codeBlockHeaderTextColor: '#FFFFFF',
        toolbarButtonTextColor: '#FFFFFF',
        selectedItemTextColor: '#4ECDC4',
        linkColor: '#81A1C1',
        buttonDisabledBackgroundColor: '#2A2A2A',
        buttonDisabledTextColor: '#6E6E6E',
        messagesPaneBackgroundCss: 'linear-gradient(160deg, #1A1A1A 0%, #2C5F5C 100%)',
        mainContentBackgroundCss: 'linear-gradient(160deg, #121212 0%, #1E1E1E 100%)'
    }
};

const ColorSchemeProvider = ({ children }) => {
    const [colorSchemes, setColorSchemes] = useState(initialColorSchemes);
    const [currentSchemeId, setCurrentSchemeId] = useState('dark');

    const addColorScheme = (id, scheme) => {
        setColorSchemes(prevSchemes => ({
            ...prevSchemes,
            [id]: scheme
        }));

        setTimeout(() => {
            window.createThemeEditor();
        }, 1000);
    };

    const updateColorScheme = (id, newColors) => {
        setColorSchemes(prevSchemes => ({
            ...prevSchemes,
            [id]: {
                ...prevSchemes[id],
                ...newColors,
                messagesPaneBackgroundCss: newColors.messagesPaneBackgroundCss || prevSchemes[id].messagesPaneBackgroundCss,
                mainContentBackgroundCss: newColors.mainContentBackgroundCss || prevSchemes[id].mainContentBackgroundCss
            }
        }));
    };

    const getAllColorSchemes = () => {
        return colorSchemes;
    };

    const setAllColorSchemes = (newColorSchemes) => {
        setColorSchemes(newColorSchemes);
    };

    const selectColorScheme = (id) => {
        if (colorSchemes[id]) {
            setCurrentSchemeId(id);
        } else {
            console.error(`Color scheme with id "${id}" not found.`);
        }
    };

    // Console commands
    window.addColorScheme = addColorScheme;
    window.updateColorScheme = updateColorScheme;
    window.getAllColorSchemes = getAllColorSchemes;
    window.setAllColorSchemes = setAllColorSchemes;
    window.selectColorScheme = selectColorScheme;

    return (
        <ColorSchemeContext.Provider value={{
            colorScheme: colorSchemes[currentSchemeId],
            currentSchemeId,
            colorSchemes,
            addColorScheme,
            updateColorScheme,
            getAllColorSchemes,
            setAllColorSchemes,
            selectColorScheme
        }}>
            {children}
        </ColorSchemeContext.Provider>
    );
};

const useColorScheme = () => useContext(ColorSchemeContext);

window.ColorSchemeProvider = ColorSchemeProvider;
window.useColorScheme = useColorScheme;
React.useColorScheme = useColorScheme;

window.getColorSchemeData = () => {
    return {
        colorSchemes: window.getAllColorSchemes(),
        currentSchemeId: window.useColorScheme().currentSchemeId,
        updateColorScheme: window.updateColorScheme,
        selectColorScheme: window.selectColorScheme
    };
};

// New function to set all color schemes
window.setColorSchemeData = (data) => {
    window.setAllColorSchemes(data.colorSchemes);
    window.selectColorScheme(data.currentSchemeId);
};