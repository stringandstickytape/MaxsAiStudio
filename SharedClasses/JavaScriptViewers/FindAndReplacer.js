function showOverlay(content) {
    // Create overlay element
    const overlay = document.createElement('div');
    overlay.style.cssText = `
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.9);
    z-index: 30000;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
  `;

    // Create close button
    const closeButton = document.createElement('button');
    closeButton.textContent = 'X';
    closeButton.style.cssText = `
    position: absolute;
    top: 10px;
    right: 10px;
    background: none;
    border: none;
    color: white;
    font-size: 24px;
    cursor: pointer;
  `;
    closeButton.onclick = () => document.body.removeChild(overlay);

    // Create content container
    const contentContainer = document.createElement('pre');
    contentContainer.textContent = content;
    contentContainer.style.cssText = `
    width: 80%;
    height: 80%;
    overflow: auto;
    background-color: #1e1e1e;
    color: #d4d4d4;
    padding: 20px;
    border-radius: 5px;
    font-family: 'Courier New', monospace;
    font-size: 14px;
    white-space: pre;
    margin: 0;
  `;

    // Assemble and append overlay
    overlay.appendChild(closeButton);
    overlay.appendChild(contentContainer);
    document.body.appendChild(overlay);
}

function applyFindAndReplace(originalFile, replacements) {

    let modifiedFile = originalFile;
    console.log(replacements,"!");
    for (const replacement of replacements) {
        let { find, replace } = replacement;

        // Escape special regex characters in the find string
        find = find.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

        // Create a regex that matches the 'find' string, ignoring all whitespace
        const regex = new RegExp(find.replace(/\s+/g, '\\s*'), 'gs');

        // Check if the 'find' string exists in the file
        if (!regex.test(modifiedFile)) {
            console.log(`Find string not found: "${replacement.find}"`);
            throw new Error(`Find string not found: "${replacement.find}"`);
        }

        // Reset the regex lastIndex
        regex.lastIndex = 0;

        // Collect all matches and their indices
        const matches = [];
        let match;
        while ((match = regex.exec(modifiedFile)) !== null) {
            matches.push({
                index: match.index,
                length: match[0].length
            });
        }

        // Apply replacements in reverse order to avoid index shifting
        for (let i = matches.length - 1; i >= 0; i--) {
            const { index, length } = matches[i];
            modifiedFile =
                modifiedFile.substring(0, index) +
                replace +
                modifiedFile.substring(index + length);
        }
    }
    //console.log(modifiedFile);
    //showOverlay(modifiedFile);
    return modifiedFile;
}

