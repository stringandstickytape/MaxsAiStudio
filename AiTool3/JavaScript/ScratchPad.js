const ScratchPad = (function () {
    let scratchPadWindow = null;
    let pillButton = null;
    let currentSelection = '';
    let selectionInProgress = false;
    let mouseX = 0;
    let mouseY = 0;

    function initScratchPad() {
        createPillButton();
        document.addEventListener('mousedown', () => { selectionInProgress = true; });
        document.addEventListener('mouseup', handleSelectionEnd);
        document.addEventListener('keydown', handleKeyDown);
        document.addEventListener('keyup', handleKeyUp);
        document.addEventListener('click', handleDocumentClick);
        document.addEventListener('mousemove', updateMousePosition);
    }

    function updateMousePosition(e) {
        mouseX = e.clientX;
        mouseY = e.clientY;
    }

    function handleSelectionEnd(e) {
        if (selectionInProgress) {
            selectionInProgress = false;
            setTimeout(() => checkSelection(e.clientX, e.clientY), 10);
        }
    }

    function handleKeyDown(e) {
        if (e.shiftKey) {
            selectionInProgress = true;
        }
    }

    function handleKeyUp(e) {
        if (!e.shiftKey && selectionInProgress) {
            selectionInProgress = false;
            setTimeout(() => checkSelection(mouseX, mouseY), 10);
        }
    }

    function checkSelection(x, y) {
        const selection = window.getSelection();
        currentSelection = selection.toString().trim();
        if (currentSelection.length > 0) {
            showPillButton(x, y);
        } else {
            hidePillButton();
        }
    }

    function handleDocumentClick(e) {
        if (pillButton && !pillButton.contains(e.target)) {
            hidePillButton();
        }
    }

    function createPillButton() {
        pillButton = document.createElement('button');
        pillButton.id = 'scratchPadPillButton';
        pillButton.textContent = 'Copy to Scratch Pad';
        pillButton.style.position = 'fixed';
        pillButton.style.zIndex = '1000';
        pillButton.style.borderRadius = '20px';
        pillButton.style.display = 'none';
        pillButton.style.padding = '5px 10px';
        pillButton.style.backgroundColor = '#007bff';
        pillButton.style.color = 'white';
        pillButton.style.border = 'none';
        pillButton.style.cursor = 'pointer';
        pillButton.addEventListener('click', copyToScratchPad);
        document.body.appendChild(pillButton);
    }

    function showPillButton(x, y) {
        const buttonRect = pillButton.getBoundingClientRect();
        const windowWidth = window.innerWidth;
        const windowHeight = window.innerHeight;

        let left = x + 10;
        let top = y + 10;

        if (left + buttonRect.width > windowWidth) {
            left = windowWidth - buttonRect.width - 10;
        }
        if (top + buttonRect.height > windowHeight) {
            top = windowHeight - buttonRect.height - 10;
        }

        pillButton.style.left = `${left}px`;
        pillButton.style.top = `${top}px`;
        pillButton.style.display = 'block';
    }

    function hidePillButton() {
        if (pillButton) {
            pillButton.style.display = 'none';
        }
    }

    function copyToScratchPad(e) {
        e.stopPropagation();
        if (!scratchPadWindow || scratchPadWindow.closed) {
            createScratchPadWindow();
        }
        scratchPadWindow.document.getElementById('scratchPadContent').value += currentSelection + '\n\n';
        hidePillButton();
        clearSelection();
    }

    function clearSelection() {
        if (window.getSelection) {
            window.getSelection().removeAllRanges();
        } else if (document.selection) {
            document.selection.empty();
        }
    }

    function createScratchPadWindow() {
        scratchPadWindow = window.open('', 'ScratchPad', 'width=400,height=400');
        scratchPadWindow.document.write(`
            <html>
            <head>
                <title>Scratch Pad</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 0; padding: 10px; }
                    #scratchPadContent { width: 100%; height: 90vh; resize: none; }
                </style>
            </head>
            <body>
                <textarea id="scratchPadContent"></textarea>
            </body>
            </html>
        `);
    }

    return {
        init: initScratchPad
    };
})();

ScratchPad.init();