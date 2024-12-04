let svg, g, zoom, root;
let isVertical = true;
let selectedNode;

const nodeWidth = 300;
const nodeHeight = 80;
const spacing = 60;

const tooltip = d3.select('body')
    .append('div')
    .attr('class', 'tooltip')
    .style('position', 'absolute')
    .style('visibility', 'hidden')
    .style('background-color', 'white')
    .style('padding', '5px')
    .style('border', '1px solid #ddd')
    .style('border-radius', '3px')
    .style('pointer-events', 'none');
let tooltipTimeout;

function initializeGraph() {
    const svgContainer = document.createElement('div');
    svgContainer.id = 'svg-container';
    document.body.appendChild(svgContainer);

    const toggleButton = document.createElement('button');
    toggleButton.id = 'toggle-button';
    toggleButton.textContent = '⯐';
    document.body.appendChild(toggleButton);

    svg = d3.select('#svg-container')
        .append('svg')
        .attr('width', '100%')
        .attr('height', '100%');

    svg.append('rect')
        .attr('width', '100%')
        .attr('height', '100%')
        .attr('fill', '#404040');

    zoom = d3.zoom()
        .on('zoom', (event) => {
            g.attr('transform', event.transform);
        });

    svg.call(zoom);

    g = svg.append('g');

    root = { id: 'root', label: 'Conversation Start', children: [] };

    const contextMenu = createContextMenu();

    document.addEventListener('click', () => {
        contextMenu.style('display', 'none');
    });

    d3.select('#toggle-button').on('click', toggleLayout);
}

function createContextMenu() {
    const menu = d3.select('body')
        .append('div')
        .attr('class', 'context-menu')
        .style('position', 'absolute')
        .style('display', 'none');

    const menuItems = [
        { text: 'Save this branch as TXT', type: 'saveTxt' },
        { text: 'Save this branch as HTML', type: 'saveHtml' },
        { text: 'Edit Raw', type: 'editRaw' }
    ];

    menuItems.forEach(item => {
        menu.append('div')
            .text(item.text)
            .attr('class', 'context-menu-item')
            .on('click', () => {
                window.chrome.webview.postMessage({
                    type: item.type,
                    nodeId: selectedNode
                });
                menu.style('display', 'none');
            });
    });





    return menu;
}

function clear() {
    root.children = [];
    updateGraph();
}

function fitAll() {
    if (!root || !root.children.length) return;

    const bounds = g.node().getBBox();
    const fullWidth = svg.node().clientWidth;
    const fullHeight = svg.node().clientHeight;
    const scale = 0.95 / Math.max(bounds.width / fullWidth, bounds.height / fullHeight);
    const translate = [
        fullWidth / 2 - scale * (bounds.x + bounds.width / 2),
        fullHeight / 2 - scale * (bounds.y + bounds.height / 2)
    ];

    svg.transition().duration(750)
        .call(zoom.transform, d3.zoomIdentity.translate(translate[0], translate[1]).scale(scale));
}

function addNodes(nodes) {
    nodes.forEach(node => {
        root.children.push({
            id: node.id,
            label: node.label.length > 160 ? node.label.substring(0, 157) + '...' : node.label,
            role: node.role,
            color: node.colour,
            tooltip: node.tooltip, // Add this line
            children: []
        });
    });
    updateGraph();
}

function addLinks(links) {
    links.forEach(link => {
        const sourceNode = findNode(root, link.source);
        const targetNode = findNode(root, link.target);
        if (sourceNode && targetNode) {
            if (!sourceNode.children) sourceNode.children = [];
            sourceNode.children.push(targetNode);
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


    function positionNode(node, x, y, level) {
        node.x = x;
        node.y = y;

        if (node.children && node.children.length > 0) {
            let totalSize = 0;
            node.children.forEach(child => {
                const childSize = getSubtreeSize(child);
                totalSize += isVertical ? childSize.width : childSize.height;
            });
            totalSize += (node.children.length - 1) * spacing;

            let childPos = isVertical ? x - totalSize / 2 : y;

            node.children.forEach(child => {
                const childSize = getSubtreeSize(child);
                if (isVertical) {
                    positionNode(child, childPos + childSize.width / 2, y + nodeHeight + spacing, level + 1);
                    childPos += childSize.width + spacing;
                } else {
                    positionNode(child, x + nodeWidth + spacing, childPos + childSize.height / 2, level + 1);
                    childPos += childSize.height + spacing;
                }
            });
        }
    }

    function getSubtreeSize(node) {
        if (!node.children || node.children.length === 0) {
            return { width: nodeWidth, height: nodeHeight };
        }

        let totalWidth = 0;
        let totalHeight = 0;

        node.children.forEach(child => {
            const childSize = getSubtreeSize(child);
            totalWidth += childSize.width;
            totalHeight += childSize.height;
        });

        if (isVertical) {
            totalWidth = Math.max(nodeWidth, totalWidth + (node.children.length - 1) * spacing);
            totalHeight += nodeHeight + spacing;
        } else {
            totalWidth += nodeWidth + spacing;
            totalHeight = Math.max(nodeHeight, totalHeight + (node.children.length - 1) * spacing);
        }

        return { width: totalWidth, height: totalHeight };
    }

    positionNode(root, 0, 0, 0);

    const links = g.selectAll('.link')
        .data(getLinks(root), d => `${d.source.id}-${d.target.id}`);

    links.enter()
        .append('path')
        .attr('class', 'link')
        .merge(links)
        .attr('fill', 'none')
        .attr('stroke-width', 2)
        .attr('opacity', 0)
        .attr('d', d => {
            const sourceX = d.source.x + nodeWidth / 2;
            const sourceY = d.source.y + nodeHeight;
            const targetX = d.target.x + nodeWidth / 2;
            const targetY = d.target.y;
            const midY = (sourceY + targetY) / 2;
            return isVertical
                ? `M${sourceX},${sourceY} C${sourceX},${midY} ${targetX},${midY} ${targetX},${targetY}`
                : `M${sourceX},${sourceY} L${targetX},${targetY}`;
        })
        .transition()
        .duration(500)
        .attr('opacity', 1);

    links.exit().remove();

    const nodes = g.selectAll('.node')
        .data(getNodes(root), d => d.id);

    const nodeEnter = nodes.enter()
        .append('g')
        .attr('class', 'node')
        .attr('id', d => d.id)
        .attr('opacity', 0);

    nodeEnter
        .on('mouseover', function (event, d) {
            // Only show tooltip if the node has a tooltip property
            if (d.tooltip) {
                clearTimeout(tooltipTimeout);
                tooltipTimeout = setTimeout(() => {
                    tooltip
                        .style('visibility', 'visible')
                        .text(d.tooltip);
                }, 500);
            }
        })
        .on('mousemove', function (event) {
            tooltip
                .style('top', (event.pageY - 10) + 'px')
                .style('left', (event.pageX + 10) + 'px');
        })
        .on('mouseout', function () {
            clearTimeout(tooltipTimeout);
            tooltip.style('visibility', 'hidden');
        });

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
        .attr('stroke-width', 2);

    nodeEnter.append('foreignObject')
        .attr('width', nodeWidth)
        .attr('height', nodeHeight)
        .append('xhtml:div')
        .style('width', '100%')
        .style('height', '100%')
        .style('display', 'flex')
        .style('align-items', 'center')
        .style('justify-content', 'center')
        .style('padding', '5px')
        .style('box-sizing', 'border-box')
        .style('overflow', 'hidden')
        .append('xhtml:p')
        .style('margin', '0')
        .style('font-size', '14px')
        .style('line-height', '1.2')
        .style('text-align', 'center')
        .text(d => d.label);

    nodes.merge(nodeEnter)
        .transition()
        .duration(500)
        .attr('transform', d => `translate(${d.x},${d.y})`)
        .attr('opacity', 1);

    nodes.exit().remove();

    setTimeout(fitAll, 550);
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
    const node = g.select(`#${id}`);
    if (node.empty()) {
        console.warn(`Node with id '${id}' not found.`);
        return;
    }

    const bounds = node.node().getBBox();
    const fullWidth = svg.node().clientWidth;
    const fullHeight = svg.node().clientHeight;
    const scale = 0.5;
    const x = bounds.x + bounds.width / 2;
    const y = bounds.y + bounds.height / 2;

    svg.transition().duration(750)
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
}

function getAllNodes(node) {
    let nodes = [node];
    if (node.children) {
        node.children.forEach(child => {
            nodes = nodes.concat(getAllNodes(child));
        });
    }
    return nodes;
}

// Initialize the graph
initializeGraph();

// Expose functions to the global scope for external use
window.clear = clear;
window.fitAll = fitAll;
window.addNodes = addNodes;
window.addLinks = addLinks;
window.centerOnNode = centerOnNode;
window.getAllNodes = getAllNodes;