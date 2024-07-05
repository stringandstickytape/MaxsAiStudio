(function () {
    // Load Cytoscape
    function loadScript(url) {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = url;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }

    loadScript('https://cdnjs.cloudflare.com/ajax/libs/cytoscape/3.21.1/cytoscape.min.js').then(() => {
        // Create and append necessary DOM elements

        // Create and append necessary DOM elements
        const style = document.createElement('style');

        style.textContent = `
        {magiccsstoken}
        #cy {
            -webkit-user-select: none;
            -moz-user-select: none;
            -ms-user-select: none;
            user-select: none;
        }
        #cy * {
            -webkit-user-select: none;
            -moz-user-select: none;
            -ms-user-select: none;
            user-select: none;
        }
        `;
        document.head.appendChild(style);

        const toggleBtn = document.createElement('button');
        toggleBtn.id = 'toggleBtn';
        toggleBtn.textContent = 'Change Layout';
        document.body.appendChild(toggleBtn);

        const cyDiv = document.createElement('div');
        cyDiv.id = 'cy';
        document.body.appendChild(cyDiv);

        // Cytoscape initialization
        var cy = cytoscape({
            container: document.getElementById('cy'),
            elements: [],
            style: [
                {
                    selector: 'node',
                    style: {
                        'shape': 'rectangle',
                        'background-color': '#f6f6f6',
                        'border-color': '#999',
                        'border-width': 1,
                        'padding': '10px',
                        'text-valign': 'center',
                        'text-halign': 'center',
                        'text-wrap': 'wrap',
                        'text-max-width': '100px',
                        'width': 'label',
                        'height': 'label',
                        'font-size': '12px',
                        'content': function (ele) {
                            return ele.data('label').length > 100 ? ele.data('label').substring(0, 97) + '...' : ele.data('label');
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
                },
                {
                    selector: '[id = "start"]',
                    style: {
                        'background-color': '#4CAF50',
                        'color': 'white'
                    }
                },
                {
                    selector: 'node[id ^= "u"]',
                    style: {
                        'background-color': '#E1F5FE',
                    }
                },
                {
                    selector: 'node[id ^= "ai"]',
                    style: {
                        'background-color': '#FFF3E0',
                    }
                }
            ],
            layout: {
                name: 'breadthfirst',
                directed: true,
                padding: 10,
                spacingFactor: 1.25,
                animate: false
            },
            userZoomingEnabled: true,
            userPanningEnabled: true,
            boxSelectionEnabled: false,
            wheelSensitivity: 0.1
        });


        document.addEventListener('contextmenu', function (e) {
            e.preventDefault();
            return false;
        }, { capture: true });


        // Right-click context menu

        cy.on('cxttap', 'node', function (evt) {
            evt.preventDefault();
            var node = evt.target;

            // Get context menu options from C#
            window.chrome.webview.postMessage({
                type: 'getContextMenuOptions',
                nodeId: node.id(),
                nodeLabel: node.data('label')
            });

            // Show context menu immediately with placeholder options
            showContextMenu(['Loading...'], node.id(), node.data('label'), evt.renderedPosition.x, evt.renderedPosition.y);
        });




        cy.on('tap', 'node', function (evt) {
            var node = evt.target;
            console.log('tapped ' + node.id());
            // Call back to C# with the node ID
            window.chrome.webview.postMessage({
                type: 'nodeClicked',
                nodeId: node.id(),
                nodeLabel: node.data('label')
            });
        });

        var layouts = [
            {
                name: 'breadthfirst',
                directed: true,
                padding: 10,
                spacingFactor: 1.25,
            },
            {
                name: 'circle',
            },
            {
                name: 'concentric',
            },
            {
                name: 'grid',
            },
            {
                name: 'random',
            },
            {
                name: 'cose',
            },
        ];

        var currentLayoutIndex = 0;

        function runLayout() {
            var layout = layouts[currentLayoutIndex];
            cy.layout({
                ...layout,
                animate: true,
                animationDuration: 1000,
                animationEasing: 'ease-in-out'
            }).run();
        }


        ///// C# interface
        function addNode(id, label) {
            cy.add({
                group: 'nodes',
                data: { id: id, label: label }
            });
            cy.layout(layouts[currentLayoutIndex]).run();
        }

        function addNodes(nodesData) {
            const newNodes = nodesData.map(node => ({
                group: 'nodes',
                data: { id: node.id, label: node.label }
            }));

            cy.add(newNodes);
            cy.layout(layouts[currentLayoutIndex]).run();
        }


        function addLink(sourceId, targetId) {
            cy.add({
                group: 'edges',
                data: { source: sourceId, target: targetId }
            });
            cy.layout(layouts[currentLayoutIndex]).run();
        }

        function addLinks(linksData) {
            const newLinks = linksData.map(link => ({
                group: 'edges',
                data: { source: link.source, target: link.target }
            }));

            cy.add(newLinks);
            cy.layout(layouts[currentLayoutIndex]).run();
        }

        // Function to show context menu



        // Right-click context menu
        cy.on('cxttap', 'node', function (evt) {
            evt.preventDefault();
            var node = evt.target;

            // Get context menu options from C#
            window.chrome.webview.postMessage({
                type: 'getContextMenuOptions',
                nodeId: node.id(),
                nodeLabel: node.data('label')
            });

            // Show context menu immediately with placeholder options
            showContextMenu(['Loading...'], node.id(), node.data('label'), evt.renderedPosition.x, evt.renderedPosition.y);
        });

        // Function to show context menu
        function showContextMenu(options, nodeId, nodeLabel, x, y) {
            // Remove existing context menu if any
            var existingMenu = document.getElementById('context-menu');
            if (existingMenu) {
                existingMenu.remove();
            }

            // Create context menu
            var menu = document.createElement('div');
            menu.id = 'context-menu';
            menu.style.position = 'absolute';
            menu.style.left = x + 'px';
            menu.style.top = y + 'px';
            menu.style.backgroundColor = 'white';
            menu.style.border = '1px solid black';
            menu.style.padding = '5px';
            menu.style.zIndex = '1000';

            // Store nodeId and nodeLabel for later use
            menu.dataset.nodeId = nodeId;
            menu.dataset.nodeLabel = nodeLabel;

            options.forEach(function (option) {
                var item = document.createElement('div');
                item.textContent = option;
                item.style.cursor = 'pointer';
                item.style.padding = '5px';
                item.addEventListener('click', function () {
                    // Callback to C# with the selected option
                    window.chrome.webview.postMessage({
                        type: 'contextMenuOptionSelected',
                        nodeId: nodeId,
                        nodeLabel: nodeLabel,
                        option: option
                    });
                    menu.remove();
                });
                menu.appendChild(item);
            });

            document.body.appendChild(menu);

            // Close menu when clicking outside
            document.addEventListener('click', function closeMenu(e) {
                if (!menu.contains(e.target)) {
                    menu.remove();
                    document.removeEventListener('click', closeMenu);
                }
            });
        }

        function updateContextMenuOptions(options) {
            var menu = document.getElementById('context-menu');
            if (menu) {
                menu.innerHTML = '';
                options.forEach(function (option) {
                    var item = document.createElement('div');
                    item.textContent = option;
                    item.style.cursor = 'pointer';
                    item.style.padding = '5px';
                    item.addEventListener('click', function () {
                        window.chrome.webview.postMessage({
                            type: 'contextMenuOptionSelected',
                            nodeId: menu.dataset.nodeId,
                            nodeLabel: menu.dataset.nodeLabel,
                            option: option
                        });
                        menu.remove();
                    });
                    menu.appendChild(item);
                });
            }
        }

        window.updateContextMenuOptions = updateContextMenuOptions;
        window.showContextMenu = showContextMenu;
        window.addNode = addNode;
        window.addLink = addLink;
        window.addNodes = addNodes;
        window.addLinks = addLinks;

        ///// end C# interface

        document.getElementById('toggleBtn').addEventListener('click', function () {
            currentLayoutIndex = (currentLayoutIndex + 1) % layouts.length;
            runLayout();
        });

        // Redraw on window resize
        var resizeTimer;
        window.addEventListener('resize', function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                cy.resize();
                runLayout();
            }, 250);
        });
    });
})();