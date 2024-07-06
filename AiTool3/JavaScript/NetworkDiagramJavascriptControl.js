function loadScript(url) {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = url;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

// Create Cytoscape container div
const cyDiv = document.createElement('div');
cyDiv.id = 'cy';
cyDiv.style.width = '100%';
cyDiv.style.height = '100%';
document.body.appendChild(cyDiv);
console.log("!!!");
loadScript('https://cdnjs.cloudflare.com/ajax/libs/cytoscape/3.21.1/cytoscape.min.js')
    .then(() => loadScript('https://cdnjs.cloudflare.com/ajax/libs/dagre/0.8.5/dagre.min.js'))
    .then(() => loadScript('https://cdn.jsdelivr.net/npm/cytoscape-dagre@2.3.2/cytoscape-dagre.min.js'))
    .then(() => {
        const cy = cytoscape({
            container: document.getElementById('cy'),
            elements: [
                { data: { id: 'a', label: 'This is node A with a very long label that should be truncated and wrapped' } },
                { data: { id: 'b', label: 'Node B with some more text' } },
                { data: { id: 'c', label: 'Node C' } },
                { data: { id: 'd', label: 'Node D with a bit more text to show wrapping' } },
                { data: { id: 'e', label: 'Node E' } },
                { data: { id: 'ab', source: 'a', target: 'b' } },
                { data: { id: 'bc', source: 'b', target: 'c' } },
                { data: { id: 'cd', source: 'c', target: 'd' } },
                { data: { id: 'de', source: 'd', target: 'e' } },
                { data: { id: 'ae', source: 'a', target: 'e' } }
            ],
            style: [
                {
                    selector: 'node',
                    style: {
                        'shape': 'rectangle',
                        'background-color': '#f2f2f2',
                        'border-color': '#666',
                        'border-width': 1,
                        'padding': '10px',
                        'text-valign': 'center',
                        'text-halign': 'center',
                        'text-wrap': 'wrap',
                        'text-max-width': '100px',
                        'width': 'label',
                        'height': 'label',
                        'label': function (ele) {
                            return ele.data('label').substring(0, 100) + (ele.data('label').length > 100 ? '...' : '');
                        }
                    }
                },
                {
                    selector: 'edge',
                    style: {
                        'width': 2,
                        'line-color': '#999',
                        'target-arrow-color': '#999',
                        'target-arrow-shape': 'triangle',
                        'curve-style': 'bezier'
                    }
                }
            ],
            layout: {
                name: 'dagre',
                rankDir: 'TB',
                nodeSep: 60,
                rankSep: 120,
                padding: 10
            }
        });

    }).catch(error => {
        console.error('Error loading scripts:', error);
    })

