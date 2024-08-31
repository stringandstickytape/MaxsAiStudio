function createSvgViewer(svgJsonString) {
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
        overlay.style.backgroundColor = 'rgba(255,255,255, 1)';
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
        container.id = 'svg-container';
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

    function renderSvg(container, svgString) {
        container.innerHTML = svgString;
        const svgElement = container.querySelector('svg');
        svgElement.setAttribute('width', '100%');
        svgElement.setAttribute('height', '100%');
        svgElement.style.width = '100%';
        svgElement.style.height = '100%';
        return svgElement;
    }

    async function init() {
        try {
            const container = createOverlay();
            const svgString = svgJsonString;
            const svgElement = renderSvg(container, svgString);

            await loadScript('https://cdn.jsdelivr.net/npm/svg-pan-zoom@3.6.1/dist/svg-pan-zoom.min.js');

            panZoomInstance = svgPanZoom(svgElement, {
                zoomEnabled: true,
                controlIconsEnabled: true,
                fit: true,
                center: true,
                maxZoom: 1000,
                minZoom: .01,
                zoomScaleSensitivity: 0.3,
                mouseWheelZoomEnabled: true,
                mouseWheelZoomSpeed: 2
            });

            window.addEventListener('resize', resizeHandler);
            resizeHandler(); // Initial call to ensure proper sizing
        } catch (error) {
            console.error('Error initializing SVG viewer:', error);
        }
    }

    init();
}