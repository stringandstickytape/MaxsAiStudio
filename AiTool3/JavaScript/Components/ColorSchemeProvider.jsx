const { createContext, useState, useContext } = React;

const ColorSchemeContext = createContext();

const initialColorSchemes = {
    "Serene": {
        "name": "Serene",
        "id": "Serene",
        "backgroundColor": "#121212",
        "headerBackgroundColor": "#1E1E1E",
        "inputBackgroundColor": "#1A1A1A",
        "buttonBackgroundColor": "#2C5F5C",
        "buttonBackgroundCss": "linear-gradient(135deg, #2C5F5C 0%, #3A7A77 100%)",
        "dropdownBackgroundColor": "#1A1A1A",
        "messageUserBackgroundColor": "#1E2A3D",
        "messageAIBackgroundColor": "#1A1A1A",
        "messageRootBackgroundColor": "#151C2C",
        "codeBlockBackgroundColor": "#1E1E1E",
        "codeBlockHeaderBackgroundColor": "#252525",
        "scrollbarBackgroundColor": "#1E1E1E",
        "toolbarBackgroundColor": "#1A1A1A",
        "toolbarButtonBackgroundColor": "#2A3F5F",
        "selectedItemBackgroundColor": "#202020",
        "textColor": "#E0E0E0",
        "headerTextColor": "#FFFFFF",
        "inputTextColor": "#E0E0E0",
        "buttonTextColor": "#FFFFFF",
        "dropdownTextColor": "#E0E0E0",
        "messageUserTextColor": "#E6E6E6",
        "messageAITextColor": "#E0E0E0",
        "messageRootTextColor": "#E6E6E6",
        "codeBlockTextColor": "#E0E0E0",
        "codeBlockHeaderTextColor": "#FFFFFF",
        "toolbarButtonTextColor": "#FFFFFF",
        "selectedItemTextColor": "#4ECDC4",
        "linkColor": "#81A1C1",
        "buttonDisabledBackgroundColor": "#2A2A2A",
        "buttonDisabledTextColor": "#6E6E6E",
        "headerBarBackgroundCss": "linear-gradient(160deg, #1A1A1A 0%, #2C5F5C 100%)",
        "messagesPaneBackgroundCss": "linear-gradient(135deg, rgba(30, 42, 61, 0.8) 0%, rgba(26, 26, 26, 0.8) 100%)",
        "mainContentBackgroundCss": "linear-gradient(135deg, #2C3E50 0%, #4A5568 50%, #2C3E50 100%)",
        "mainContentBackgroundImage": "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'%3E%3Crect width='100' height='100' fill='none'/%3E%3Cpath d='M0 0L50 50L100 0L100 100L50 50L0 100Z' fill='%23ffffff' fill-opacity='0.05'/%3E%3C/svg%3E\")",
        "fontFamily": "Segoe UI, sans-serif"
    }
};

const ColorSchemeProvider = ({ children }) => {
    const [colorSchemes, setColorSchemes] = useState(initialColorSchemes);
    const [currentSchemeId, setCurrentSchemeId] = useState('Serene');

    const addColorScheme = (id, scheme) => {
        const schemeId = scheme.id || scheme.themeName || id;
        setColorSchemes(prevSchemes => ({
            ...prevSchemes,
            [schemeId]: scheme
        }));

        window.selectColorScheme(schemeId);
    };

    const updateColorScheme = (id, newColors) => {
        setColorSchemes(prevSchemes => {
            const schemeId = prevSchemes[id].id || id;
            return {
                ...prevSchemes,
                [schemeId]: {
                    ...prevSchemes[id],
                    ...newColors,
                    messagesPaneBackgroundCss: newColors.messagesPaneBackgroundCss || prevSchemes[id].messagesPaneBackgroundCss,
                    mainContentBackgroundCss: newColors.mainContentBackgroundCss || prevSchemes[id].mainContentBackgroundCss,
                    mainContentBackgroundImage: newColors.mainContentBackgroundImage || prevSchemes[id].mainContentBackground,
                    messagesPaneBackgroundImage: newColors.messagesPaneBackgroundImage || prevSchemes[id].messagesPaneBackgroundImage,
                    messagesPaneBackgroundFilter: newColors.messagesPaneBackgroundFilter || prevSchemes[id].messagesPaneBackgroundFilter
                }
            };
        });
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