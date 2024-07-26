function fixNewlinesInStrings(jsonString) {
    return jsonString.replace(
        /("find"|"replace")\s*:\s*"((?:\\.|[^"\\])*?)"/g,
        (match, key, value) => {
            const fixedValue = value.replace(/\n/g, '\\n');
            return `${key}: "${fixedValue}"`;
        }
    );
}

const FormattedContent = ({ content, guid, codeBlockCounter, onCodeBlockRendered }) => {
    const { colorScheme } = React.useColorScheme();
    const [currentlySelectedFindAndReplaceSet, setCurrentlySelectedFindAndReplaceSet] = useState(window.currentlySelectedFindAndReplaceSet);
    const [selectedMessageGuid, setSelectedMessageGuid] = useState(window.selectedMessageGuid);

    useEffect(() => {
        const handleFindAndReplaceUpdate = () => {
            setCurrentlySelectedFindAndReplaceSet(window.currentlySelectedFindAndReplaceSet);
            setSelectedMessageGuid(window.selectedMessageGuid);
        };

        window.addEventListener('findAndReplaceUpdate', handleFindAndReplaceUpdate);
        return () => window.removeEventListener('findAndReplaceUpdate', handleFindAndReplaceUpdate);
    }, []);

    const fileTypes = {
        webView: ["html", "js"],
        viewJsonStringArray: ["json"],
        viewSvg: ["svg", "xml", "html"],
        installTheme: ["maxtheme.json"],
        browseJsonObject: ["json"],
        viewMermaidDiagram: ["mermaid"],
        viewPlantUMLDiagram: ["plantuml"],
        viewDOTDiagram: ["dot"],
        runPythonScript: ["python"],
        launchSTL: ["stl"],
        runPowerShellScript: ["powershell"],
        selectFindAndReplaceScript: ["findandreplace.json"],
    };

    const addMessageButton = (label, action, dataType) => (
        <button
            onClick={action}
            style={{
                backgroundColor: colorScheme.buttonBackgroundColor,
                color: colorScheme.buttonTextColor,
                border: 'none',
                padding: '3px 8px',
                borderRadius: '3px',
                cursor: 'pointer',
                marginRight: '5px',
            }}
        >
            {label}
        </button>
    );

    const formatContent = (text) => {
        const codeBlockRegex = /\u0060\u0060\u0060(.*?)\n([\s\S]*?)\u0060\u0060\u0060/g;
        console.log(text);
        debugger;
        const parts = [];
        let lastIndex = 0;

        text.replace(codeBlockRegex, (match, fileType, code, offset) => {
            if (offset > lastIndex) {
                parts.push(text.slice(lastIndex, offset));
            }

            let trimmedFileType = fileType.trim().toLowerCase();

            let fileExt = trimmedFileType;
            if (fileExt.indexOf('.') > -1) {
                fileExt = fileExt.split('.').reverse()[0];
            }
            console.log(trimmedFileType);

            parts.push(
                <div key={offset}>
                    <div style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        fontWeight: 'bold',
                        backgroundColor: colorScheme.codeBlockHeaderBackgroundColor,
                        color: colorScheme.codeBlockHeaderTextColor,
                        padding: '5px 10px',
                        borderTopLeftRadius: '5px',
                        borderTopRightRadius: '5px',
                    }}>
                        <span>{fileType.trim()}</span>
                        <div>
                            {addMessageButton("Copy", () => {
                                window.chrome.webview.postMessage({
                                    type: 'Copy',
                                    content: code.trim()
                                });
                            })}
                            {addMessageButton("Save As", () => {
                                window.chrome.webview.postMessage({
                                    type: 'Save As',
                                    dataType: trimmedFileType,
                                    content: code.trim()
                                });
                            })}
                            {fileTypes.webView.includes(fileExt) &&
                                addMessageButton("WebView", () => {
                                    window.chrome.webview.postMessage({
                                        type: 'WebView',
                                        content: code.trim()
                                    });
                                })
                            }
                            {fileTypes.viewSvg.includes(fileExt) &&
                                addMessageButton("View SVG", () => {
                                    createSvgViewer(code.trim());
                                })
                            }
                            {fileTypes.viewJsonStringArray.includes(fileExt) &&
                                addMessageButton("View JSON String Array", () => {
                                    window.chrome.webview.postMessage({
                                        type: 'View JSON String Array',
                                        content: code.trim()
                                    });
                                })
                            }
                            {fileTypes.installTheme.includes(trimmedFileType) &&
                                addMessageButton("Install Theme", () => {
                                    var obj = JSON.parse(code.trim());
                                    var themeName = Object.keys(obj)[0];
                                    window.addColorScheme(themeName, Object.values(obj)[0]);
                                    
                                })
                            }
                            {fileTypes.browseJsonObject.includes(fileExt) &&
                                addMessageButton("Browse JSON Object", () => {
                                    createJsonViewer(code.trim());
                                })
                            }
                            {fileTypes.viewMermaidDiagram.includes(fileExt) &&
                                addMessageButton("View Mermaid Diagram", () => {
                                    createMermaidViewer(code.trim());
                                })
                            }
                            {fileTypes.viewPlantUMLDiagram.includes(fileExt) &&
                                addMessageButton("View PlantUML Diagram", () => {
                                    window.chrome.webview.postMessage({
                                        type: 'View PlantUML Diagram',
                                        content: code.trim()
                                    });
                                })
                            }
                            {fileTypes.viewDOTDiagram.includes(fileExt) &&
                                addMessageButton("View DOT Diagram", () => {
                                    renderDotString(code.trim());
                                })
                            }
                            {fileTypes.runPythonScript.includes(fileExt) &&
                                addMessageButton("Run Python Script", () => {
                                    window.chrome.webview.postMessage({
                                        type: 'Run Python Script',
                                        content: code.trim()
                                    });
                                })
                            }
                            {fileTypes.launchSTL.includes(fileExt) &&
                                addMessageButton("Launch STL", () => {
                                    window.chrome.webview.postMessage({
                                        type: 'Launch STL',
                                        content: code.trim()
                                    });
                                })
                            }
                            {fileTypes.runPowerShellScript.includes(fileExt) &&
                                addMessageButton("Run PowerShell Script", () => {
                                    window.chrome.webview.postMessage({
                                        type: 'Run PowerShell Script',
                                        content: code.trim()
                                    });
                                })
                            }

                            {fileTypes.selectFindAndReplaceScript.includes(trimmedFileType) &&
                                addMessageButton("Select Find-And-Replace Script", () => {
                                    try {
                                        const fixedJsonString = fixNewlinesInStrings(code.trim());
                                        const parsedJson = JSON.parse(fixedJsonString);
                                        window.currentlySelectedFindAndReplaceSet = parsedJson;
                                        window.selectedMessageGuid = guid;
                                        window.dispatchEvent(new Event('findAndReplaceUpdate'));
                                    } catch (error) {
                                        alert('Error parsing Find-And-Replace script: ' + error);
                                    }
                                })
                            }
                            {currentlySelectedFindAndReplaceSet && selectedMessageGuid &&
                                addMessageButton("Apply Find-And-Replace Script", () => {
                                    window.chrome.webview.postMessage({
                                        type: 'applyFindAndReplace',
                                        content: code.trim(),
                                        guid: guid,
                                        dataType: trimmedFileType,
                                        codeBlockIndex: codeBlockCounter.toString(),
                                        findAndReplaces: JSON.stringify(currentlySelectedFindAndReplaceSet),
                                        selectedMessageGuid: selectedMessageGuid
                                    });
                                    // Reset currentlySelectedFindAndReplaceSet and hide 'Apply...' buttons
                                    window.currentlySelectedFindAndReplaceSet = null;
                                    window.selectedMessageGuid = null;
                                    window.dispatchEvent(new Event('findAndReplaceUpdate'));
                                })
                            }
                        </div>
                    </div>
                    <div style={{
                        fontFamily: 'monospace',
                        whiteSpace: 'pre-wrap',
                        backgroundColor: colorScheme.codeBlockBackgroundColor,
                        color: colorScheme.codeBlockTextColor,
                        padding: '10px',
                        borderBottomLeftRadius: '5px',
                        borderBottomRightRadius: '5px',
                        marginBottom: '10px'
                    }}>
                        {code.trim()}
                    </div>
                </div>
            );
            lastIndex = offset + match.length;
            onCodeBlockRendered(); // Increment the counter after rendering a code block
        });

        if (lastIndex < text.length) {
            parts.push(text.slice(lastIndex));
        }

        return parts;
    };

    return <>{formatContent(content)}</>;
};