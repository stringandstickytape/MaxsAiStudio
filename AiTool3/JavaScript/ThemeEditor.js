function createThemeEditor() {
    const allColorSchemes = window.getAllColorSchemes();
    let currentSchemeId = Object.keys(allColorSchemes)[0];

    const container = createContainer();
    const wrapper = createWrapper();
    container.appendChild(wrapper);

    const header = createHeader();
    wrapper.appendChild(header);

    const content = createContent();
    wrapper.appendChild(content);

    const footer = createFooter();
    wrapper.appendChild(footer);

    document.body.appendChild(container);

    updateContent(currentSchemeId);  // Move this call here, after appending to DOM
    function createContainer() {
        return createElement('div', {
            style: {
                position: 'fixed',
                top: 0,
                left: 0,
                width: '100%',
                height: '100%',
                backgroundColor: 'rgba(0, 0, 0, 0.8)',
                zIndex: 10000,
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center'
            }
        });
    }

    function createWrapper() {
        return createElement('div', {
            style: {
                width: 'calc(100% - 40px)',
                height: 'calc(100% - 40px)',
                backgroundColor: '#2C2C2C',
                borderRadius: '10px',
                display: 'flex',
                flexDirection: 'column'
            }
        });
    }

    function createHeader() {
        const header = createElement('div', {
            style: {
                padding: '20px',
                borderBottom: '1px solid #444'
            }
        });

        const title = createElement('h2', {
            textContent: 'Theme Editor',
            style: {
                color: '#FFFFFF',
                marginBottom: '20px'
            }
        });

        const schemeSelector = createSchemeSelector();

        header.appendChild(title);
        header.appendChild(schemeSelector);

        return header;
    }

    function createSchemeSelector() {
        const selector = createElement('select', {
            style: {
                padding: '10px',
                backgroundColor: '#3A3A3A',
                color: '#FFFFFF',
                border: 'none',
                borderRadius: '5px',
                fontSize: '16px'
            },
            onchange: (e) => updateContent(e.target.value)
        });

        Object.keys(allColorSchemes).forEach(schemeId => {
            const option = createElement('option', {
                value: schemeId,
                textContent: schemeId,
                selected: schemeId === currentSchemeId
            });
            selector.appendChild(option);
        });

        return selector;
    }

    function createContent() {
        return createElement('div', {
            style: {
                flex: 1,
                padding: '20px',
                overflowY: 'auto'
            }
        });
    }

    function updateContent(schemeId) {
        currentSchemeId = schemeId;
        const scheme = allColorSchemes[schemeId];
        const content = wrapper.querySelector('div:nth-child(2)');
        content.innerHTML = '';

        const form = createElement('form', {
            style: {
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
                gap: '20px'
            }
        });
        debugger;
        Object.entries(scheme).forEach(([key, value]) => {
            const formGroup = createFormGroup(key, value);
            form.appendChild(formGroup);
        });

        content.appendChild(form);
    }

    function createFormGroup(key, value) {
        const formGroup = createElement('div');

        const label = createElement('label', {
            textContent: key,
            style: {
                color: '#FFFFFF',
                display: 'block',
                marginBottom: '5px'
            }
        });

        formGroup.appendChild(label);

        if (key === 'messagesPaneBackgroundCss' || key === 'mainContentBackgroundCss') {
            const textarea = createElement('textarea', {
                value: value,
                name: key,  // Add name attribute
                style: {
                    width: '100%',
                    height: '80px',
                    backgroundColor: '#3A3A3A',
                    color: '#FFFFFF',
                    border: 'none',
                    borderRadius: '5px',
                    padding: '10px',
                    resize: 'vertical'
                }
            });
            formGroup.appendChild(textarea);
        } else {
            const inputContainer = createElement('div', {
                style: {
                    display: 'flex',
                    alignItems: 'center'
                }
            });

            const colorInput = createElement('input', {
                type: 'color',
                value: value,
                name: key,  // Add name attribute
                style: {
                    width: '50px',
                    height: '50px',
                    border: 'none',
                    borderRadius: '5px',
                    marginRight: '10px'
                }
            });

            const textInput = createElement('input', {
                type: 'text',
                value: value,
                name: key,  // Add name attribute
                style: {
                    flex: 1,
                    backgroundColor: '#3A3A3A',
                    color: '#FFFFFF',
                    border: 'none',
                    borderRadius: '5px',
                    padding: '10px'
                }
            });

            colorInput.addEventListener('input', () => textInput.value = colorInput.value);
            textInput.addEventListener('input', () => colorInput.value = textInput.value);

            inputContainer.appendChild(colorInput);
            inputContainer.appendChild(textInput);
            formGroup.appendChild(inputContainer);
        }

        return formGroup;
    }

    function createFooter() {
        const footer = createElement('div', {
            style: {
                padding: '20px',
                borderTop: '1px solid #444',
                display: 'flex',
                justifyContent: 'flex-end'
            }
        });

        const cancelButton = createButton('Cancel', '#F44336', handleCancel);
        const saveButton = createButton('Save', '#4CAF50', handleSave);

        footer.appendChild(cancelButton);
        footer.appendChild(saveButton);

        return footer;
    }

    function createButton(text, color, onClick) {
        return createElement('button', {
            textContent: text,
            style: {
                padding: '10px 20px',
                backgroundColor: color,
                color: '#FFFFFF',
                border: 'none',
                borderRadius: '5px',
                cursor: 'pointer',
                marginLeft: '10px',
                fontSize: '16px'
            },
            onclick: onClick
        });
    }

    function handleCancel() {
        document.body.removeChild(container);
    }

    function handleSave() {
        const form = wrapper.querySelector('form');
        const updatedTheme = {};

        form.querySelectorAll('input[type="color"], input[type="text"], textarea').forEach(input => {
            let key;
            const formGroup = input.closest('div');
            const label = formGroup.querySelector('label');

            if (label) {
                key = label.textContent;
            } else {
                // Fallback: use the input's name attribute or a default
                key = input.name || 'unknown';
            }

            updatedTheme[key] = input.value;
        });

        window.updateColorScheme(currentSchemeId, updatedTheme);
        window.selectColorScheme(currentSchemeId);

        window.chrome.webview.postMessage({
            type: 'selectTheme',
            content: JSON.stringify(currentSchemeId)
        });

        document.body.removeChild(container);
    }

    function createElement(tag, options = {}) {
        const element = document.createElement(tag);
        Object.entries(options).forEach(([key, value]) => {
            if (key === 'style') {
                Object.assign(element.style, value);
            } else if (key.startsWith('on')) {
                element.addEventListener(key.slice(2).toLowerCase(), value);
            } else {
                element[key] = value;
            }
        });
        return element;
    }
}