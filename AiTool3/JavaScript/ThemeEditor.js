function createThemeEditor() {
    // Get color schemes and current scheme ID
    const allColorSchemes = window.getAllColorSchemes();
    const currentSchemeId = Object.keys(allColorSchemes)[0]; // Default to first scheme

    // Create container
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

    // Create theme editor wrapper
    const wrapper = document.createElement('div');
    wrapper.style.width = 'calc(100% - 40px)';
    wrapper.style.height = 'calc(100% - 40px)';
    wrapper.style.backgroundColor = '#3A3A3A';
    wrapper.style.overflow = 'auto';
    wrapper.style.padding = '20px';
    wrapper.style.borderRadius = '10px';
    container.appendChild(wrapper);

    // Create title
    const title = document.createElement('h2');
    title.textContent = 'Theme Editor';
    title.style.color = '#FFFFFF';
    title.style.marginBottom = '20px';
    wrapper.appendChild(title);

    // Create scheme selector
    const schemeSelector = document.createElement('select');
    schemeSelector.style.marginBottom = '20px';
    schemeSelector.style.padding = '5px';
    schemeSelector.style.backgroundColor = '#2C2C2C';
    schemeSelector.style.color = '#FFFFFF';
    schemeSelector.style.border = 'none';
    wrapper.appendChild(schemeSelector);

    // Populate scheme selector
    Object.keys(allColorSchemes).forEach(schemeId => {
        const option = document.createElement('option');
        option.value = schemeId;
        option.textContent = schemeId;
        if (schemeId === currentSchemeId) {
            option.selected = true;
        }
        schemeSelector.appendChild(option);
    });

    // Create form
    const form = document.createElement('form');
    wrapper.appendChild(form);

    // Function to update form with selected scheme
    function updateForm(schemeId) {
        form.innerHTML = ''; // Clear existing form fields
        const scheme = allColorSchemes[schemeId];

        for (const [key, value] of Object.entries(scheme)) {
            const formGroup = document.createElement('div');
            formGroup.style.marginBottom = '10px';
            form.appendChild(formGroup);

            const label = document.createElement('label');
            label.textContent = key;
            label.style.color = '#FFFFFF';
            label.style.display = 'block';
            label.style.marginBottom = '5px';
            formGroup.appendChild(label);

            const input = document.createElement('input');
            input.type = 'color';
            input.value = value;
            input.style.width = '50px';
            input.style.height = '30px';
            input.style.verticalAlign = 'middle';
            formGroup.appendChild(input);

            const textInput = document.createElement('input');
            textInput.type = 'text';
            textInput.value = value;
            textInput.style.marginLeft = '10px';
            textInput.style.width = '100px';
            textInput.style.backgroundColor = '#2C2C2C';
            textInput.style.color = '#FFFFFF';
            textInput.style.border = 'none';
            textInput.style.padding = '5px';
            formGroup.appendChild(textInput);

            // Sync color input and text input
            input.addEventListener('input', () => {
                textInput.value = input.value;
            });
            textInput.addEventListener('input', () => {
                input.value = textInput.value;
            });
        }
    }

    // Initial form update
    updateForm(currentSchemeId);

    // Update form when scheme selection changes
    schemeSelector.addEventListener('change', (e) => {
        updateForm(e.target.value);
    });

    // Create button container
    const buttonContainer = document.createElement('div');
    buttonContainer.style.marginTop = '20px';
    buttonContainer.style.textAlign = 'right';
    wrapper.appendChild(buttonContainer);

    // Create OK button
    const okButton = document.createElement('button');
    okButton.textContent = 'OK';
    okButton.style.padding = '10px 20px';
    okButton.style.marginRight = '10px';
    okButton.style.backgroundColor = '#4CAF50';
    okButton.style.color = '#FFFFFF';
    okButton.style.border = 'none';
    okButton.style.cursor = 'pointer';
    buttonContainer.appendChild(okButton);

    // Create Cancel button
    const cancelButton = document.createElement('button');
    cancelButton.textContent = 'Cancel';
    cancelButton.style.padding = '10px 20px';
    cancelButton.style.backgroundColor = '#F44336';
    cancelButton.style.color = '#FFFFFF';
    cancelButton.style.border = 'none';
    cancelButton.style.cursor = 'pointer';
    buttonContainer.appendChild(cancelButton);

    // Handle OK button click
    okButton.addEventListener('click', (e) => {
        e.preventDefault();
        
        window.chrome.webview.postMessage({
            type: 'allThemes',
            content: JSON.stringify(window.getAllColorSchemes())
        });

        const updatedTheme = {};
        form.querySelectorAll('input[type="color"]').forEach(input => {
            updatedTheme[input.previousElementSibling.textContent] = input.value;
        });
        window.updateColorScheme(schemeSelector.value, updatedTheme);
        window.selectColorScheme(schemeSelector.value);

        window.chrome.webview.postMessage({
            type: 'selectTheme',
            content: JSON.stringify(schemeSelector.value)
        });

        document.body.removeChild(container);
    });

    // Handle Cancel button click
    cancelButton.addEventListener('click', () => {
        document.body.removeChild(container);
    });

    // Add container to body
    document.body.appendChild(container);
}