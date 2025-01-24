const { createContext, useState, useContext } = React;

const ColorSchemeContext = createContext();

const initialColorSchemes = {
    "Default": {
        "id": "Default",
        "backgroundColor": "#0A1929",
        "headerBackgroundColor": "#142F45",
        "inputBackgroundColor": "#1C3F5E",
        "buttonBackgroundColor": "#2D5F8B",
        "dropdownBackgroundColor": "#183754",
        "messageUserBackgroundColor": "#2B4C6F",
        "messageAIBackgroundColor": "#1D364F",
        "messageRootBackgroundColor": "#15293E",
        "codeBlockBackgroundColor": "#0D2137",
        "codeBlockHeaderBackgroundColor": "#1E425F",
        "scrollbarBackgroundColor": "#0F2A40",
        "toolbarBackgroundColor": "#142F45",
        "toolbarButtonBackgroundColor": "#2D5F8B",
        "selectedItemBackgroundColor": "#3A77AD",
        "textColor": "#E6F3FF",
        "headerTextColor": "#7CC2FF",
        "inputTextColor": "#B8E2FF",
        "buttonTextColor": "#FFFFFF",
        "dropdownTextColor": "#A1D6FF",
        "messageUserTextColor": "#E6F3FF",
        "messageAITextColor": "#B8E2FF",
        "messageRootTextColor": "#7CC2FF",
        "codeBlockTextColor": "#56B6C2",
        "codeBlockHeaderTextColor": "#61AFEF",
        "toolbarButtonTextColor": "#FFFFFF",
        "selectedItemTextColor": "#FFFFFF",
        "linkColor": "#61AFEF",
        "buttonDisabledBackgroundColor": "#1C3F5E",
        "buttonDisabledTextColor": "#4A6F94",
        "fontLink": "https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;700&family=Inter:wght@400;600&display=swap",
        "fontFamily": "Inter, system-ui, -apple-system, sans-serif",
        "fixedWidthFontFamily": "JetBrains Mono, monospace",
        "borderRadius": "4px",
        "buttonBorder": "1px solid #3A77AD",
        "headerBarBackgroundCss": "linear-gradient(180deg, #142F45 0%, #1C3F5E 100%)",
        "mainContentBackgroundCss": "radial-gradient(circle at 50% -20%, #142F45 0%, #0A1929 50%)",
        "messagesPaneBackgroundCss": "linear-gradient(180deg, rgba(13, 33, 55, 0.8) 0%, rgba(10, 25, 41, 0.9) 100%)",
        "messagesPaneBackgroundFilter": "blur(10px)",
        "buttonBackgroundCss": "linear-gradient(180deg, #2D5F8B 0%, #245174 100%)"
    }
};

const ColorSchemeProvider = ({ children }) => {
    debugger;
    const [colorSchemes, setColorSchemes] = useState(initialColorSchemes);
    const [currentSchemeId, setCurrentSchemeId] = useState(Object.keys(initialColorSchemes)[0]);

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

        // bugfix: if the default theme got eaten somehow, reinstate it

        if (!newColorSchemes['Default']) {
            setColorSchemes({
                'Default': initialColorSchemes['Default'],
                ...newColorSchemes
            });
        } else {
            setColorSchemes(newColorSchemes);
        }
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