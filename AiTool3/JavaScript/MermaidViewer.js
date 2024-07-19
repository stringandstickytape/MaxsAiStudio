
        
 //       { url: 'https://cdnjs.cloudflare.com/ajax/libs/svg.js/3.2.4/svg.min.js', check: () => window.SVG },
 //       { url: 'https://cdn.jsdelivr.net/npm/svg-pan-zoom@3.6.1/src/svg-pan-zoom.js', check: () => window.svgPanZoom },
 //       { url: 'https://cdnjs.cloudflare.com/ajax/libs/mermaid/10.9.1/mermaid.min.js', check: () => window.mermaid }
function createMermaidViewer(mermaidString) {
    let panZoomInstance;
    let overlay;

    function loadScript(url) {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = url;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }

    function createOverlay() {
        overlay = document.createElement('div');
        overlay.style.position = 'fixed';
        overlay.style.top = '0';
        overlay.style.left = '0';
        overlay.style.width = '100vw';
        overlay.style.height = '100vh';
        overlay.style.backgroundColor = 'rgba(30, 30, 30, 1)';
        overlay.style.zIndex = '10000';
        overlay.style.display = 'flex';
        overlay.style.flexDirection = 'column';
        document.body.appendChild(overlay);

        const closeButton = document.createElement('button');
        closeButton.textContent = 'X';
        closeButton.style.position = 'absolute';
        closeButton.style.top = '10px';
        closeButton.style.right = '10px';
        closeButton.style.fontSize = '20px';
        closeButton.style.cursor = 'pointer';
        closeButton.onclick = closeOverlay;
        overlay.appendChild(closeButton);

        const container = document.createElement('div');
        container.id = 'mermaid-container';
        container.style.flex = '1';
        container.style.display = 'flex';
        container.style.justifyContent = 'center';
        container.style.alignItems = 'center';
        overlay.appendChild(container);

        return container;
    }

    function closeOverlay() {
        if (overlay) {
            document.body.removeChild(overlay);
            window.removeEventListener('resize', resizeHandler);
            if (panZoomInstance) {
                panZoomInstance.destroy();
            }
        }
    }

    function resizeHandler() {
        if (panZoomInstance) {
            panZoomInstance.resize();
            panZoomInstance.fit();
            panZoomInstance.center();
        }
    }

    async function renderMermaidDiagram(container, diagram) {
        const { svg } = await mermaid.render('mermaid-diagram', diagram);
        container.innerHTML = svg;

        const svgElement = container.querySelector('svg');
        svgElement.setAttribute('width', '100%');
        svgElement.setAttribute('height', '100%');
        svgElement.style.width = '100%';
        svgElement.style.height = '100%';

        const gElement = svgElement.querySelector('g');
        gElement.removeAttribute('transform');

        return svgElement;
    }

    async function init() {
        try {
            await loadScript('https://cdn.jsdelivr.net/npm/mermaid@10.2.3/dist/mermaid.min.js');

            mermaid.initialize({
                startOnLoad: false,
                theme: 'default',
                flowchart: {
                    useMaxWidth: false,
                    htmlLabels: true
                }
            });

            const container = createOverlay();
            const svgElement = await renderMermaidDiagram(container, mermaidString);

            await loadScript('https://cdn.jsdelivr.net/npm/svg-pan-zoom@3.6.1/dist/svg-pan-zoom.min.js');

            panZoomInstance = svgPanZoom(svgElement, {
                zoomEnabled: true,
                controlIconsEnabled: true,
                fit: true,
                center: true,
                maxZoom: 1000,
                zoomScaleSensitivity: 0.3,
                mouseWheelZoomEnabled: true,
                mouseWheelZoomSpeed: 2
            });

            window.addEventListener('resize', resizeHandler);
            resizeHandler(); // Initial call to ensure proper sizing
        } catch (error) {
            console.error('Error initializing diagram:', error);
        }
    }

    init();
}

// Usage example:
// createMermaidViewer(`
// graph TD
//     A[Start] --> B{Is it?}
//     B -->|Yes| C[OK]
//     C --> D[Rethink]
//     D --> B
//     B ---->|No| E[End]
// `);