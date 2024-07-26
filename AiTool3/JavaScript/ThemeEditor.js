function createThemeEditor() {
    const allColorSchemes = window.getAllColorSchemes();
    const currentSchemeId = Object.keys(allColorSchemes)[0];

    const container = createContainer();
    const wrapper = createWrapper();
    container.appendChild(wrapper);

    const title = createTitle('Theme Editor');
    wrapper.appendChild(title);

    const schemeSelector = createSchemeSelector(allColorSchemes, currentSchemeId);
    wrapper.appendChild(schemeSelector);

    const form = document.createElement('form');
    form.style.display = 'flex';
    form.style.flexWrap = 'wrap';
    form.style.justifyContent = 'space-between';
    wrapper.appendChild(form);

    function updateForm(schemeId) {
        form.innerHTML = '';
        const scheme = allColorSchemes[schemeId];

        const entries = Object.entries(scheme);
        const columnCount = Math.min(3, Math.ceil(entries.length / 5)); // Max 3 columns, min 5 items per column

        for (let i = 0; i < columnCount; i++) {
            const column = document.createElement('div');
            column.style.flex = `1 1 ${100 / columnCount}%`;
            column.style.minWidth = '250px';
            column.style.padding = '0 10px';
            form.appendChild(column);
        }

        entries.forEach((entry, index) => {
            const [key, value] = entry;
            const formGroup = createFormGroup(key, value);
            form.children[index % columnCount].appendChild(formGroup);
        });
    }

    updateForm(currentSchemeId);

    schemeSelector.addEventListener('change', (e) => {
        updateForm(e.target.value);
    });

    const buttonContainer = createButtonContainer();
    wrapper.appendChild(buttonContainer);

    const okButton = createButton('OK', '#4CAF50');
    const cancelButton = createButton('Cancel', '#F44336');
    buttonContainer.appendChild(okButton);
    buttonContainer.appendChild(cancelButton);

    okButton.addEventListener('click', handleOkClick);
    cancelButton.addEventListener('click', handleCancelClick);

    document.body.appendChild(container);

    function createContainer() {
        const container = document.createElement('div');
        container.style.position = 'fixed';
        container.style.top = '0';
        container.style.left = '0';
        container.style.width = '100%';
        container.style.height = '100%';
        container.style.backgroundColor = 'rgba(0, 0, 0, 0.8)';
        container.style.zIndex = '10000';
        container.style.display = 'flex';
        container.style.justifyContent = 'center';
        container.style.alignItems = 'center';
        return container;
    }

    function createWrapper() {
        const wrapper = document.createElement('div');
        wrapper.style.width = 'calc(100% - 40px)';
        wrapper.style.height = 'calc(100% - 40px)';
        wrapper.style.backgroundColor = '#3A3A3A';
        wrapper.style.overflow = 'auto';
        wrapper.style.padding = '20px';
        wrapper.style.borderRadius = '10px';
        return wrapper;
    }

    function createTitle(text) {
        const title = document.createElement('h2');
        title.textContent = text;
        title.style.color = '#FFFFFF';
        title.style.marginBottom = '20px';
        return title;
    }

    function createSchemeSelector(schemes, currentId) {
        const selector = document.createElement('select');
        selector.style.marginBottom = '20px';
        selector.style.padding = '5px';
        selector.style.backgroundColor = '#2C2C2C';
        selector.style.color = '#FFFFFF';
        selector.style.border = 'none';

        Object.keys(schemes).forEach(schemeId => {
            const option = document.createElement('option');
            option.value = schemeId;
            option.textContent = schemeId;
            option.selected = schemeId === currentId;
            selector.appendChild(option);
        });

        return selector;
    }

    function createFormGroup(key, value) {
        const formGroup = document.createElement('div');
        formGroup.style.marginBottom = '10px';

        const label = document.createElement('label');
        label.textContent = key;
        label.style.color = '#FFFFFF';
        label.style.display = 'block';
        label.style.marginBottom = '5px';
        formGroup.appendChild(label);

        if (key === 'messagesPaneBackgroundCss' || key === 'mainContentBackgroundCss') {
            const textarea = createTextarea(value);
            formGroup.appendChild(textarea);
        } else {
            const inputContainer = document.createElement('div');
            inputContainer.style.display = 'flex';
            inputContainer.style.alignItems = 'center';

            const colorInput = createColorInput(value);
            const textInput = createTextInput(value);
            inputContainer.appendChild(colorInput);
            inputContainer.appendChild(textInput);
            formGroup.appendChild(inputContainer);

            colorInput.addEventListener('input', () => {
                textInput.value = colorInput.value;
            });
            textInput.addEventListener('input', () => {
                colorInput.value = textInput.value;
            });
        }

        return formGroup;
    }

    function createTextInput(value) {
        const input = document.createElement('input');
        input.type = 'text';
        input.value = value;
        input.style.marginLeft = '10px';
        input.style.flex = '1';
        input.style.backgroundColor = '#2C2C2C';
        input.style.color = '#FFFFFF';
        input.style.border = 'none';
        input.style.padding = '5px';
        return input;
    }

    function createTextarea(value) {
        const textarea = document.createElement('textarea');
        textarea.value = value;
        textarea.style.width = '100%';
        textarea.style.height = '60px';
        textarea.style.backgroundColor = '#2C2C2C';
        textarea.style.color = '#FFFFFF';
        textarea.style.border = 'none';
        textarea.style.padding = '5px';
        return textarea;
    }

    function createColorInput(value) {
        const input = document.createElement('input');
        input.type = 'color';
        input.value = value;
        input.style.width = '50px';
        input.style.height = '30px';
        input.style.verticalAlign = 'middle';
        return input;
    }

    function createTextInput(value) {
        const input = document.createElement('input');
        input.type = 'text';
        input.value = value;
        input.style.marginLeft = '10px';
        input.style.width = '100px';
        input.style.backgroundColor = '#2C2C2C';
        input.style.color = '#FFFFFF';
        input.style.border = 'none';
        input.style.padding = '5px';
        return input;
    }

    function createButtonContainer() {
        const container = document.createElement('div');
        container.style.marginTop = '20px';
        container.style.textAlign = 'right';
        return container;
    }

    function createButton(text, color) {
        const button = document.createElement('button');
        button.textContent = text;
        button.style.padding = '10px 20px';
        button.style.marginRight = text === 'OK' ? '10px' : '0';
        button.style.backgroundColor = color;
        button.style.color = '#FFFFFF';
        button.style.border = 'none';
        button.style.cursor = 'pointer';
        return button;
    }

    function handleOkClick(e) {
        e.preventDefault();

        window.chrome.webview.postMessage({
            type: 'allThemes',
            content: JSON.stringify(window.getAllColorSchemes())
        });

        const updatedTheme = {};
        form.querySelectorAll('input[type="color"]').forEach(input => {
            updatedTheme[input.previousElementSibling.textContent] = input.value;
        });

        const messagesPaneBackgroundCss = form.querySelector(`textarea[value="${allColorSchemes[schemeSelector.value].messagesPaneBackgroundCss}"]`);
        const mainContentBackgroundCss = form.querySelector(`textarea[value="${allColorSchemes[schemeSelector.value].mainContentBackgroundCss}"]`);

        if (messagesPaneBackgroundCss) {
            updatedTheme['messagesPaneBackgroundCss'] = messagesPaneBackgroundCss.value;
        }
        if (mainContentBackgroundCss) {
            updatedTheme['mainContentBackgroundCss'] = mainContentBackgroundCss.value;
        }

        window.updateColorScheme(schemeSelector.value, updatedTheme);
        window.selectColorScheme(schemeSelector.value);

        window.chrome.webview.postMessage({
            type: 'selectTheme',
            content: JSON.stringify(schemeSelector.value)
        });

        document.body.removeChild(container);
    }

    function handleCancelClick() {
        document.body.removeChild(container);
    }
}