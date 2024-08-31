function renderDotString(dotString) {
    const vizUrl = 'https://unpkg.com/viz.js@2.1.2/viz.js';
    const fullUrl = 'https://unpkg.com/viz.js@2.1.2/full.render.js';
    const svgPanZoomUrl = 'https://cdn.jsdelivr.net/npm/svg-pan-zoom@3.6.1/dist/svg-pan-zoom.min.js';

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
        const overlay = document.createElement('div');
        overlay.style.position = 'fixed';
        overlay.style.top = '0';
        overlay.style.left = '0';
        overlay.style.width = '100%';
        overlay.style.height = '100%';
        overlay.style.backgroundColor = 'rgba(255, 255, 255, 0.9)';
        overlay.style.zIndex = '10002';
        overlay.style.display = 'flex';
        overlay.style.alignItems = 'center';
        overlay.style.justifyContent = 'center';
        return overlay;
    }

    function createCloseButton(overlay) {
        const closeButton = document.createElement('button');
        closeButton.textContent = 'X';
        closeButton.style.position = 'absolute';
        closeButton.style.top = '10px';
        closeButton.style.right = '10px';
        closeButton.style.fontSize = '24px';
        closeButton.style.fontWeight = 'bold';
        closeButton.style.cursor = 'pointer';
        closeButton.style.background = 'none';
        closeButton.style.border = 'none';
        closeButton.style.color = 'black';
        closeButton.onclick = () => document.body.removeChild(overlay);
        return closeButton;
    }

    function resizeSVG(svgElement, containerWidth, containerHeight) {
        const svgWidth = svgElement.viewBox.baseVal.width;
        const svgHeight = svgElement.viewBox.baseVal.height;
        const svgRatio = svgWidth / svgHeight;
        const containerRatio = containerWidth / containerHeight;

        let width, height;
        //if (containerRatio > svgRatio) {
        //    // Fit to height
        //    height = containerHeight;
            width = height * svgRatio;
        //} else {
        //    // Fit to width
        //    width = containerWidth;
            height = width / svgRatio;
        //}

        svgElement.style.width = `${width}px`;
        svgElement.style.height = `${height}px`;
    }

    loadScript(vizUrl)
        .then(() => loadScript(fullUrl))
        .then(() => loadScript(svgPanZoomUrl))
        .then(() => {
            const viz = new Viz();
            return viz.renderSVGElement(dotString);
        })
        .then(svgElement => {
            const overlay = createOverlay();
            const closeButton = createCloseButton(overlay);

            overlay.appendChild(closeButton);
            overlay.appendChild(svgElement);
            document.body.appendChild(overlay);

            // Resize the SVG to fit the window
            const resizeObserver = new ResizeObserver(() => {
                resizeSVG(svgElement, overlay.clientWidth, overlay.clientHeight);
                panZoom.resize(); // Resize the panZoom instance
                panZoom.fit(); // Fit the graph to the view
                panZoom.center(); // Center the graph
            });
            resizeObserver.observe(overlay);

            // Initial resize
            resizeSVG(svgElement, overlay.clientWidth, overlay.clientHeight);

            // Initialize svg-pan-zoom
            const panZoom = svgPanZoom(svgElement, {
                zoomEnabled: true,
                controlIconsEnabled: true,
                fit: true,
                center: true,
            });

            // Fit and center the graph initially
            panZoom.fit();
            panZoom.center();
        })
        .catch(error => {
            console.error('Error rendering graph:', error);
        });
}