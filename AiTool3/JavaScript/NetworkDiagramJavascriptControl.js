function loadScript(url) {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = url;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

let svg, g, zoom, root;
let isVertical = true; // New variable to track the current layout
let selectedNode;

// Create SVG container
const svgContainer = document.createElement('div');
svgContainer.id = 'svg-container';
svgContainer.style.width = '100%';
svgContainer.style.height = '100%';
document.body.appendChild(svgContainer);

// Create toggle button
const toggleButton = document.createElement('button');
toggleButton.textContent = '⯐';
toggleButton.style.color = 'white';
toggleButton.style.backgroundColor = '#444444';
toggleButton.style.position = 'absolute';
toggleButton.style.top = '10px';
toggleButton.style.left = '10px';
toggleButton.addEventListener('click', toggleLayout);
document.body.appendChild(toggleButton);


svg = d3.select('#svg-container')
    .append('svg')
    .attr('width', '100%')
    .attr('height', '100%');

// Add this block to create a background rectangle
svg.append('rect')
    .attr('width', '100%')
    .attr('height', '100%')
    .attr('fill', '#444444');


// Initialize the graph
initializeGraph();


function createContextMenu() {
    const menu = d3.select('body')
        .append('div')
        .attr('class', 'context-menu')
        .style('position', 'absolute')
        .style('background-color', 'white')
        .style('border', '1px solid black')
        .style('padding', '5px')
        .style('display', 'none');

    menu.append('div')
        .text('Save this branch as TXT')
        .attr('class', 'context-menu-item')
        .on('click', () => {
            window.chrome.webview.postMessage({
                type: 'saveTxt',
                nodeId: selectedNode
            });

            menu.style('display', 'none');
        });

    menu.append('div')
        .text('Save this branch as HTML')
        .attr('class', 'context-menu-item')
        .on('click', (a,b) => {
            window.chrome.webview.postMessage({
                type: 'saveHtml',
                nodeId: selectedNode
            });

            menu.style('display', 'none');
        });

    menu.append('div')
        .text('Disable')
        .attr('class', 'context-menu-item')
        .on('click', () => {
            console.log('lol');
            menu.style('display', 'none');
        });


    return menu;
}
function toggleGraphVisibility() {
    const container = document.getElementById('svg-container');
    if (container.style.display === 'none') {
        container.style.display = 'block';
    } else {
        container.style.display = 'none';
    }
}
function initializeGraph() {
    zoom = d3.zoom()
        .on('zoom', (event) => {
            g.attr('transform', event.transform);
        });

    svg.call(zoom);

    // Add a group for all graph elements
    g = svg.append('g')
        .attr('transform', `translate(${svg.node().clientWidth / 2},${svg.node().clientHeight / 2})`);

    // Initialize root of the tree
    root = { id: 'root', label: 'Conversation Start', children: [] };

    // Create context menu
    const contextMenu = createContextMenu();
    window.chrome.webview.postMessage({
        type: 'getContextMenuOptions'
    });


    // Add event listener to hide context menu on document click
    document.addEventListener('click', () => {
        contextMenu.style('display', 'none');
    });


}

function clear() {
    console.log("Clearing graph");
    root.children = [];
    updateGraph();
    console.log("Cleared");
}

function fitAll() {
    if (!root || !root.children.length) return;

    const bounds = g.node().getBBox();
    const fullWidth = svg.node().clientWidth;
    const fullHeight = svg.node().clientHeight;
    const width = bounds.width;
    const height = bounds.height;
    const midX = bounds.x + width / 2;
    const midY = bounds.y + height / 2;

    if (width === 0 || height === 0) return; // nothing to fit

    const scale = 0.95 / Math.max(width / fullWidth, height / fullHeight);
    const translate = [fullWidth / 2 - scale * midX, fullHeight / 2 - scale * midY];

    svg.transition().duration(1000)
        .call(
            zoom.transform,
            d3.zoomIdentity
                .translate(translate[0], translate[1])
                .scale(scale)
        );
}

function addNodes(nodes) {
    
    console.log("Adding nodes: ", nodes);
    nodes.forEach(node => {
        root.children.push({
            id: node.id,
            label: node.label.length > 160 ? node.label.substring(0, 157) + '...' : node.label,
            role: node.role,
            color: node.colour, // Add this line to store the color
            children: []
        });
    });
    updateGraph();
    
}
function addLinks(links) {
    console.log("Adding links: ", links);
    links.forEach(link => {
        const sourceNode = findNode(root, link.source);
        const targetNode = findNode(root, link.target);
        if (sourceNode && targetNode) {
            if (!sourceNode.children) sourceNode.children = [];
            sourceNode.children.push(targetNode);
            // Remove targetNode from root's direct children
            root.children = root.children.filter(n => n.id !== targetNode.id);
        }
    });
    updateGraph();
}

function findNode(node, id) {
    if (node.id === id) return node;
    if (node.children) {
        for (let child of node.children) {
            const found = findNode(child, id);
            if (found) return found;
        }
    }
    return null;
}

function updateGraph() {
    const nodeWidth = 300;
    const nodeHeight = 80;
    const spacing = 40;
    
    // Recursive function to position nodes
    function positionNode(node, x, y, level) {
        node.x = x;
        node.y = y;

        if (node.children && node.children.length > 0) {
            const childrenSize = (node.children.length - 1) * (isVertical ? (nodeWidth + spacing) : (nodeHeight + spacing));
            let childPos = isVertical ? x - childrenSize / 2 : y - childrenSize / 2;

            node.children.forEach(child => {
                if (isVertical) {
                    positionNode(child, childPos, y + nodeHeight + spacing, level + 1);
                    childPos += nodeWidth + spacing;
                } else {
                    positionNode(child, x + nodeWidth + spacing, childPos, level + 1);
                    childPos += nodeHeight + spacing;
                }
            });
        }
    }

    // Position all nodes starting from root
    positionNode(root, 0, 0, 0);

    // Update links
    const links = g.selectAll('.link')
        .data(getLinks(root), d => `${d.source.id}-${d.target.id}`);

    links.enter()
        .append('path')
        .attr('class', 'link')
        .merge(links)
        .attr('fill', 'none')
        .attr('stroke', '#ae3')
        .attr('stroke-width', 3)
        .attr('opacity', 0) // Set initial opacity to 0
        .attr('d', d => {
            if (isVertical) {
                return `M${d.source.x + nodeWidth / 2},${d.source.y + nodeHeight} C${d.source.x + nodeWidth / 2},${(d.source.y + d.target.y + nodeHeight) / 2} ${d.target.x + nodeWidth / 2},${(d.source.y + d.target.y + nodeHeight) / 2} ${d.target.x + nodeWidth / 2},${d.target.y}`;
            } else {
                return `M${d.source.x + nodeWidth},${d.source.y + nodeHeight / 2} C${(d.source.x + d.target.x + nodeWidth) / 2},${d.source.y + nodeHeight / 2} ${(d.source.x + d.target.x + nodeWidth) / 2},${d.target.y + nodeHeight / 2} ${d.target.x},${d.target.y + nodeHeight / 2}`;
            }

        })
        .transition() // Add transition
        .duration(500) // Set duration (adjust as needed)
        .attr('opacity', 1); // Fade in to full opacity

    links.exit().remove();

    // Update nodes
    const nodes = g.selectAll('.node')
        .data(getNodes(root), d => d.id);

    const nodeEnter = nodes.enter()
        .append('g')
        .attr('class', 'node')
        .attr('id', d => d.id)
        .attr('opacity', 0); // Set initial opacity to 0



    nodeEnter.on('click', function (event, d) {
        selectedNode = d.id;
            
            window.chrome.webview.postMessage({
                type: 'nodeClicked',
                nodeId: selectedNode 
            });
    });

    nodeEnter.on('contextmenu', function (event, d) {
        event.preventDefault();
        selectedNode = d.id;
        window.chrome.webview.postMessage({
            type: 'nodeClicked',
            nodeId: selectedNode
        });
        const contextMenu = d3.select('.context-menu');
        contextMenu.style('left', (event.pageX + 5) + 'px')
            .style('top', (event.pageY + 5) + 'px')
            .style('display', 'block');
    });


    nodeEnter.append('rect')
        .attr('width', nodeWidth)
        .attr('height', nodeHeight)
        .attr('fill', d => d.color || (d.role === 'Assistant' ? '#e6f3ff' : d.role === 'User' ? '#fff0e6' : '#f2f2f2'))
        .attr('stroke', d => d.role === 'Assistant' ? '#4da6ff' : d.role === 'User' ? '#ffa64d' : '#666')
        .attr('stroke-width', 1);



    nodeEnter.append('foreignObject')
        .attr('width', nodeWidth)
        .attr('height', nodeHeight)
        .append('xhtml:div')
        .style('width', '100%')
        .style('height', '100%')
        .style('display', 'flex')
        .style('align-items', 'center')
        .style('justify-content', 'center') // Add this line
        .style('padding', '5px')
        .style('box-sizing', 'border-box')
        .style('overflow', 'hidden')
        .append('xhtml:p')
        .style('margin', '0')
        .style('font-size', '14px')
        .style('line-height', '1.2')
        .style('font-family', 'Calibri, Arial, sans-serif')
        .style('text-align', 'center') // Add this line
        .text(d => d.label);
    
    nodes.merge(nodeEnter)
        .transition()
        .duration(500) // Set duration (adjust as needed)
        .attr('transform', d => `translate(${d.x},${d.y})`)
        .attr('opacity', 1); // Fade in to full opacity

    nodes.exit().remove();

    setTimeout(fitAll, 550); // Slightly longer than the transition duration

    
}

function getNodes(node) {
    let nodes = [node];
    if (node.children) {
        node.children.forEach(child => {
            nodes = nodes.concat(getNodes(child));
        });
    }
    return nodes;
}

function getLinks(node) {
    let links = [];
    if (node.children) {
        node.children.forEach(child => {
            links.push({ source: node, target: child });
            links = links.concat(getLinks(child));
        });
    }
    return links;
}

function centerOnNode(id) {
    console.log("Centre on node");
    const node = g.select(`#${id}`);
    if (node.empty()) {
        console.warn(`Node with id '${id}' not found.`);
        return;
    }

    const transform = d3.zoomTransform(svg.node());
    const bounds = node.node().getBBox();
    const fullWidth = svg.node().clientWidth;
    const fullHeight = svg.node().clientHeight;
    const scale = 0.5; // You can adjust this value to change the zoom level
    const x = bounds.x + bounds.width / 2;
    const y = bounds.y + bounds.height / 2;

    svg.transition().duration(1)
        .call(
            zoom.transform,
            d3.zoomIdentity
                .translate(fullWidth / 2, fullHeight / 2)
                .scale(scale)
                .translate(-x, -y)
        );
}

function toggleLayout() {
    isVertical = !isVertical;
    updateGraph();
    fitAll();
}