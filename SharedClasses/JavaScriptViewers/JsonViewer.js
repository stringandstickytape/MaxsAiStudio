function createJsonViewer(jsonString) {
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

    // Create JSON viewer wrapper
    const wrapper = document.createElement('div');
    wrapper.style.width = 'calc(100% - 40px)';
    wrapper.style.height = 'calc(100% - 40px)';
    wrapper.style.backgroundColor = '#3A3A3A';
    wrapper.style.overflow = 'auto';
    container.appendChild(wrapper);

    // Create close button
    const closeButton = document.createElement('button');
    closeButton.textContent = 'Close';
    closeButton.style.position = 'absolute';
    closeButton.style.top = '10px';
    closeButton.style.left = '50%';
    closeButton.style.transform = 'translateX(-50%)';
    closeButton.style.zIndex = '10001';
    closeButton.style.padding = '8px 16px';
    closeButton.style.cursor = 'pointer';
    closeButton.style.backgroundColor = '#4c4c4c';
    closeButton.style.color = '#e0e0e0';
    closeButton.style.border = 'none';
    closeButton.style.borderRadius = '4px';
    closeButton.style.fontSize = '14px';
    closeButton.style.fontWeight = 'bold';
    closeButton.style.textTransform = 'uppercase';
    closeButton.style.letterSpacing = '1px';
    closeButton.style.boxShadow = '0 2px 4px rgba(0, 0, 0, 0.2)';
    closeButton.onmouseover = () => {
        closeButton.style.backgroundColor = '#6a6a6a';
    };
    closeButton.onmouseout = () => {
        closeButton.style.backgroundColor = '#2c2c2c';
    };
    closeButton.onclick = () => document.body.removeChild(container);
    container.appendChild(closeButton);

    // Load JSONEditor CSS
    if (!document.getElementById('jsoneditor-css')) {
        const link = document.createElement('link');
        link.id = 'jsoneditor-css';
        link.rel = 'stylesheet';
        link.type = 'text/css';
        link.href = 'https://cdnjs.cloudflare.com/ajax/libs/jsoneditor/9.9.2/jsoneditor.min.css';
        document.head.appendChild(link);
    }

    // Load JSONEditor script
    if (typeof JSONEditor === 'undefined') {
        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/jsoneditor/9.9.2/jsoneditor.min.js';
        script.onload = () => initEditor();
        document.body.appendChild(script);
    } else {
        initEditor();
    }

    function initEditor() {
        const options = {
            mode: 'view',
            modes: ['view', 'form', 'text', 'tree', 'code'],
            theme: 'ace/theme/monokai'
        };

        const editor = new JSONEditor(wrapper, options);

        try {
            const json = JSON.parse(jsonString);
            editor.set(json);
        } catch (e) {
            console.error('Invalid JSON string:', e);
            editor.setText('Invalid JSON string: ' + e.message);
        }

        // Add resize listener
        window.addEventListener('resize', () => editor.resize());

        // Apply dark theme styles
        container.querySelectorAll('.jsoneditor').forEach(el => {
            el.style.border = 'none';
            el.style.backgroundColor = '#1e1e1e';
        });
        container.querySelectorAll('.jsoneditor-menu').forEach(el => {
            el.style.backgroundColor = '#252526';
            el.style.border = 'none';
        });
        container.querySelectorAll('.jsoneditor-navigation-bar').forEach(el => {
            el.style.backgroundColor = '#252526';
            el.style.border = 'none';
            el.style.color = '#d4d4d4';
        });
        container.querySelectorAll('.jsoneditor-tree').forEach(el => {
            el.style.backgroundColor = '#3A3A3A';
            el.style.color = '#d4d4d4';
        });

        container.querySelectorAll('.jsoneditor-field').forEach(el => {
            el.style.color = '#d4d4d4';
        });

        // Custom styles for dark theme
        const style = document.createElement('style');
        style.textContent = `
            .jsoneditor-tree {
                color: #d4d4d4 !important;
            }
            .jsoneditor-readonly {
                color: #d4d4d4 !important;
            }
            .jsoneditor-power-of-two {
                color: #569cd6 !important;
            }
            .jsoneditor-value.jsoneditor-string {
                color: #33a033 !important;
            }
            .jsoneditor-value.jsoneditor-number {
                color: #b5cea8 !important;


            }

            .jsoneditor-value.jsoneditor-array {
                color: #9cdcfe !important;
            }
            .jsoneditor-value.jsoneditor-boolean {
                color: #569cd6 !important;
            }
            .jsoneditor-value.jsoneditor-null {
                color: #569cd6 !important;
            }
                        .jsoneditor-value.jsoneditor-object {
                color: #d4d4d4 !important;
            }
            .jsoneditor-field {
                background-color: #3A3A3A;
                color: #d4d4d4 !important;
            }
            .jsoneditor-empty {
                background: none !important;
                border-color: #d4d4d4 !important;
            }
                        .jsoneditor-frame,.jsoneditor-frame>input {
                background-color: #000000 !important;
                color: #d4d4d4 !important;
            }

            .jsoneditor-button{

                background-color: #b9b9b9;
            }
        `;
        document.head.appendChild(style);

        // Set ace editor theme if available
        if (editor.aceEditor) {
            editor.aceEditor.setTheme("ace/theme/monokai");
        }
    }

    // Add container to body
    document.body.insertBefore(container, document.body.firstChild);
}
