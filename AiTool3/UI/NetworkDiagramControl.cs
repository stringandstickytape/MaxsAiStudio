using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Diagnostics;

namespace AiTool3.UI
{
    [Designer(typeof(NetworkDiagramControlDesigner))]
    [ToolboxItem(true)]
    public class NetworkDiagramControl : Control
    {
        private List<Node> nodes;
        private List<Connection> connections;
        private Node selectedNode;
        private Node highlightedNode;
        private Point dragOffset;
        private float zoomFactor;
        private const float ZoomIncrement = 0.1f;
        private const float MaxZoom = 3.0f;
        private const float MinZoom = 0.3f;
        private Point panOffset;
        private bool isPanning;
        private Point lastMousePosition;
        private System.Windows.Forms.Timer fitAllTimer;
        private float targetZoomFactor;
        private Point targetPanOffset;
        private float initialZoomFactor;
        private Point initialPanOffset;
        private const int AnimationDuration = 250; // 1 second
        private int animationProgress;

        public event EventHandler<NodeClickEventArgs> NodeClicked;

        // New properties for node colors
        public Color NodeBackgroundColor { get; set; } = Color.LightBlue;
        public Color NodeForegroundColor { get; set; } = Color.Black;
        public Color NodeBorderColor { get; set; } = Color.Blue;
        public Color HighlightedNodeBorderColor { get; set; } = Color.Red;

        public NetworkDiagramControl()
        {
            nodes = new List<Node>();
            connections = new List<Connection>();
            DoubleBuffered = true;
            zoomFactor = 1.0f;
            panOffset = Point.Empty;
            fitAllTimer = new System.Windows.Forms.Timer();
            fitAllTimer.Interval = 16; // Approximately 60 updates per second
            fitAllTimer.Tick += FitAllTimerTick;
        }

        public Node HighlightedNode
        {
            get => highlightedNode;
            set
            {
                highlightedNode = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.TranslateTransform(panOffset.X, panOffset.Y);
            g.ScaleTransform(zoomFactor, zoomFactor);

            // Draw connections
            foreach (Connection connection in connections)
            {
                g.DrawLine(Pens.Black, connection.StartNode.Location, connection.EndNode.Location);
            }

            // Draw nodes
            foreach (Node node in nodes)
            {
                using (SolidBrush backgroundBrush = new SolidBrush(node.BackColor ?? NodeBackgroundColor))
                using (Pen borderPen = new Pen(node == HighlightedNode ? HighlightedNodeBorderColor : node.BorderColor ?? NodeBorderColor))
                using (SolidBrush foregroundBrush = new SolidBrush(node.ForeColor ?? NodeForegroundColor))
                {
                    g.FillRectangle(backgroundBrush, node.Bounds);
                    g.DrawRectangle(borderPen, node.Bounds);
                    g.DrawString(node.Label, Font, foregroundBrush, node.Bounds, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }
            }
        }

        public Node GetNodeForGuid(string guid)
        {
            return nodes.FirstOrDefault(n => n.Guid == guid);
        }

        public void AddNode(string label, Point location, string guid)
        {
            nodes.Add(new Node(label, location, guid));
            Invalidate();
        }

        public void AddNode(Node newNode)
        {
            nodes.Add(newNode);
            Invalidate();
        }

        public void AddConnection(Node startNode, Node endNode)
        {
            connections.Add(new Connection(startNode, endNode));
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Point transformedPoint = TransformPoint(e.Location);
            selectedNode = nodes.Find(n => n.Bounds.Contains(transformedPoint));
            if (selectedNode != null)
            {
                dragOffset = new Point(transformedPoint.X - selectedNode.Location.X, transformedPoint.Y - selectedNode.Location.Y);

                // Print the GUID of the clicked node to the console
                Debug.WriteLine($"Clicked node GUID: {selectedNode.Guid}");

                HighlightedNode = selectedNode;
                NodeClicked?.Invoke(this, new NodeClickEventArgs(selectedNode));
            }
            else
            {
                isPanning = true;
                lastMousePosition = e.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (selectedNode != null)
            {
                Point transformedPoint = TransformPoint(e.Location);
                selectedNode.Location = new Point(transformedPoint.X - dragOffset.X, transformedPoint.Y - dragOffset.Y);
                Invalidate();
            }
            else if (isPanning)
            {
                int dx = e.X - lastMousePosition.X;
                int dy = e.Y - lastMousePosition.Y;
                panOffset.X += dx;
                panOffset.Y += dy;
                lastMousePosition = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            selectedNode = null;
            isPanning = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (e.Delta == 0) return;

            float oldZoom = zoomFactor;
            float zoomChange = e.Delta > 0 ? ZoomIncrement : -ZoomIncrement;

            if (e.Delta > 0 && zoomFactor < MaxZoom ||
                e.Delta < 0 && zoomFactor > MinZoom)
            {
                zoomFactor = Math.Min(MaxZoom, Math.Max(MinZoom, zoomFactor + zoomChange));
            }

            if (zoomFactor != oldZoom)
            {
                // Get the mouse position on the screen in the context of the current zoom level and pan offset
                PointF pt = new PointF((e.X - panOffset.X) / oldZoom, (e.Y - panOffset.Y) / oldZoom);

                // Adjust the pan offset so the point under the cursor remains under the cursor after the zoom
                panOffset = new Point((int)(e.X - pt.X * zoomFactor), (int)(e.Y - pt.Y * zoomFactor));

                Invalidate();
            }
        }

        private Point TransformPoint(Point originalPoint)
        {
            return new Point((int)((originalPoint.X - panOffset.X) / zoomFactor), (int)((originalPoint.Y - panOffset.Y) / zoomFactor));
        }

        internal void Clear()
        {
            nodes = new List<Node>();
            connections = new List<Connection>();
            DoubleBuffered = true;
            zoomFactor = 1.0f;
            panOffset = Point.Empty;
            Invalidate();
        }

        public void FitAll()
        {
            if (nodes.Count == 0)
            {
                return;
            }

            // Calculate the bounding rectangle for all nodes
            int left = int.MaxValue;
            int top = int.MaxValue;
            int right = int.MinValue;
            int bottom = int.MinValue;

            foreach (var node in nodes)
            {
                if (node.Bounds.Left < left)
                {
                    left = node.Bounds.Left;
                }
                if (node.Bounds.Top < top)
                {
                    top = node.Bounds.Top;
                }
                if (node.Bounds.Right > right)
                {
                    right = node.Bounds.Right;
                }
                if (node.Bounds.Bottom > bottom)
                {
                    bottom = node.Bounds.Bottom;
                }
            }

            left = left - 5;
            right = right + 5;
            top = top - 5;
            bottom = bottom + 5;

            Rectangle boundingRect = new Rectangle(left, top, right - left, bottom - top);

            // Calculate the zoom factor to fit the bounding rectangle within the control's client area
            float horizontalZoom = (float)Width / boundingRect.Width;
            float verticalZoom = (float)Height / boundingRect.Height;
            float newZoomFactor = Math.Min(horizontalZoom, verticalZoom);

            // Ensure the new zoom factor is within the allowed range
            newZoomFactor = Math.Min(MaxZoom, Math.Max(MinZoom, newZoomFactor));

            // Calculate the new pan offset to center the bounding rectangle in the control
            float centeredOffsetX = (Width - boundingRect.Width * newZoomFactor) / 2f - boundingRect.Left * newZoomFactor;
            float centeredOffsetY = (Height - boundingRect.Height * newZoomFactor) / 2f - boundingRect.Top * newZoomFactor;
            Point newPanOffset = new Point((int)centeredOffsetX, (int)centeredOffsetY);

            // Prepare animation
            initialZoomFactor = zoomFactor;
            initialPanOffset = panOffset;
            targetZoomFactor = newZoomFactor;
            targetPanOffset = newPanOffset;
            animationProgress = 0;

            // Start animation timer
            fitAllTimer.Start();
        }

        private void FitAllTimerTick(object sender, EventArgs e)
        {
            animationProgress += fitAllTimer.Interval;
            float percentageComplete = Math.Min(1, (float)animationProgress / AnimationDuration);

            zoomFactor = Lerp(initialZoomFactor, targetZoomFactor, percentageComplete);
            panOffset = Lerp(initialPanOffset, targetPanOffset, percentageComplete);

            Invalidate();

            if (percentageComplete >= 1)
            {
                fitAllTimer.Stop();
            }
        }

        private float Lerp(float start, float end, float percentage)
        {
            return start + (end - start) * percentage;
        }

        private Point Lerp(Point start, Point end, float percentage)
        {
            return new Point(
                (int)(start.X + (end.X - start.X) * percentage),
                (int)(start.Y + (end.Y - start.Y) * percentage)
            );
        }

        internal void CenterOnNode(Node node)
        {
            // Calculate the new pan offset to center the node, taking the zoom factor into account
            var centerPoint = new PointF(
                Width / 2f / zoomFactor - node.Location.X,
                Height / 2f / zoomFactor - node.Location.Y);

            panOffset = new Point((int)(centerPoint.X * zoomFactor), (int)(centerPoint.Y * zoomFactor));
            Invalidate();
        }

        public class Node
        {
            public string Label { get; set; }
            public Point Location { get; set; }
            public Rectangle Bounds => new Rectangle(Location.X - 175, Location.Y - 35, 350, 70);

            public Color? BackColor = null;
            public Color? ForeColor = null;
            public Color? BorderColor = null;

            public string Guid { get; set; }

            public Node(string label, Point location, string guid)
            {
                Label = label;
                Location = location;
                Guid = guid;
            }
        }

        public class NodeClickEventArgs : EventArgs
        {
            public Node ClickedNode { get; }

            public NodeClickEventArgs(Node clickedNode)
            {
                ClickedNode = clickedNode;
            }
        }

        private class Connection
        {
            public Node StartNode { get; set; }
            public Node EndNode { get; set; }

            public Connection(Node startNode, Node endNode)
            {
                StartNode = startNode;
                EndNode = endNode;
            }
        }
    }

    public class NetworkDiagramControlDesigner : ControlDesigner
    {
        public override void Initialize(IComponent component)
        {
            base.Initialize(component);
            if (component is NetworkDiagramControl control)
            {
                EnableDesignMode(control, "NetworkDiagramControl");
            }
        }
    }
}