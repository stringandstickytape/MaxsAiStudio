using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private const int AnimationDuration = 250;
        private int animationProgress;

        public event EventHandler<NodeClickEventArgs> NodeClicked;

        public Color NodeBackgroundColor { get; set; } = Color.LightBlue;
        public Color NodeForegroundColor { get; set; } = Color.Black;
        public Color NodeBorderColor { get; set; } = Color.Blue;
        public Color HighlightedNodeBorderColor { get; set; } = Color.Red;
        public Color NodeGradientStart { get; set; } = Color.White;
        public Color NodeGradientEnd { get; set; } = Color.LightSkyBlue;
        public int NodeCornerRadius { get; set; } = 10;
        public bool UseDropShadow { get; set; } = true;

        public NetworkDiagramControl()
        {
            nodes = new List<Node>();
            connections = new List<Connection>();
            DoubleBuffered = true;
            zoomFactor = 1.0f;
            panOffset = Point.Empty;
            fitAllTimer = new System.Windows.Forms.Timer();
            fitAllTimer.Interval = 16;
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
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TranslateTransform(panOffset.X, panOffset.Y);
            g.ScaleTransform(zoomFactor, zoomFactor);

            foreach (Connection connection in connections)
            {
                DrawConnection(g, connection);
            }

            foreach (Node node in nodes)
            {
                DrawNode(g, node);
            }
        }

        private void DrawConnection(Graphics g, Connection connection)
        {
            using (GraphicsPath path = new GraphicsPath())
            using (Pen pen = new Pen(Color.FromArgb(180, 180, 180), 2f))
            {
                Point start = connection.StartNode.Location;
                Point end = connection.EndNode.Location;
                Point control1 = new Point(start.X + (end.X - start.X) / 2, start.Y);
                Point control2 = new Point(start.X + (end.X - start.X) / 2, end.Y);

                path.AddBezier(start, control1, control2, end);
                g.DrawPath(pen, path);

                DrawArrow(g, pen, control2, end);
            }
        }

        private void DrawArrow(Graphics g, Pen pen, Point start, Point end)
        {
            float arrowSize = 10f;
            PointF tip = end;
            PointF[] arrowHead = new PointF[3];
            float angle = (float)Math.Atan2(start.Y - end.Y, start.X - end.X);
            arrowHead[0] = tip;
            arrowHead[1] = new PointF(tip.X + arrowSize * (float)Math.Cos(angle - Math.PI / 6),
                                      tip.Y + arrowSize * (float)Math.Sin(angle - Math.PI / 6));
            arrowHead[2] = new PointF(tip.X + arrowSize * (float)Math.Cos(angle + Math.PI / 6),
                                      tip.Y + arrowSize * (float)Math.Sin(angle + Math.PI / 6));

            g.FillPolygon(pen.Brush, arrowHead);
        }

        private void DrawNode(Graphics g, Node node)
        {
            Rectangle bounds = node.Bounds;

            if (UseDropShadow)
            {
                using (GraphicsPath shadowPath = CreateRoundedRectangle(bounds.X + 3, bounds.Y + 3, bounds.Width, bounds.Height, NodeCornerRadius))
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = Color.FromArgb(100, Color.Black);
                    shadowBrush.SurroundColors = new Color[] { Color.Transparent };
                    g.FillPath(shadowBrush, shadowPath);
                }
            }

            using (GraphicsPath path = CreateRoundedRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, NodeCornerRadius))
            using (LinearGradientBrush gradientBrush = new LinearGradientBrush(bounds,
                node.BackColor ?? NodeGradientStart,
                node.BackColor != null ? Color.FromArgb(Math.Abs(node.BackColor.Value.R - 40), Math.Abs(node.BackColor.Value.G - 40), Math.Abs(node.BackColor.Value.B - 40)) : NodeGradientEnd,
                LinearGradientMode.Vertical))
            using (Pen borderPen = new Pen(node == HighlightedNode ? HighlightedNodeBorderColor : node.BorderColor ?? NodeBorderColor, 2f))
            using (SolidBrush foregroundBrush = new SolidBrush(node.ForeColor ?? NodeForegroundColor))
            {
                g.FillPath(gradientBrush, path);
                g.DrawPath(borderPen, path);

                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(node.Label, Font, foregroundBrush, bounds, sf);
                }
            }
        }

        private GraphicsPath CreateRoundedRectangle(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(x, y, diameter, diameter);

            path.AddArc(arc, 180, 90);
            arc.X = x + width - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = y + height - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = x;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
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
                PointF pt = new PointF((e.X - panOffset.X) / oldZoom, (e.Y - panOffset.Y) / oldZoom);
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

            int left = int.MaxValue;
            int top = int.MaxValue;
            int right = int.MinValue;
            int bottom = int.MinValue;

            foreach (var node in nodes)
            {
                if (node.Bounds.Left < left) left = node.Bounds.Left;
                if (node.Bounds.Top < top) top = node.Bounds.Top;
                if (node.Bounds.Right > right) right = node.Bounds.Right;
                if (node.Bounds.Bottom > bottom) bottom = node.Bounds.Bottom;
            }

            left = left - 5;
            right = right + 5;
            top = top - 5;
            bottom = bottom + 5;

            Rectangle boundingRect = new Rectangle(left, top, right - left, bottom - top);

            float horizontalZoom = (float)Width / boundingRect.Width;
            float verticalZoom = (float)Height / boundingRect.Height;
            float newZoomFactor = Math.Min(horizontalZoom, verticalZoom);

            newZoomFactor = Math.Min(MaxZoom, Math.Max(MinZoom, newZoomFactor));

            float centeredOffsetX = (Width - boundingRect.Width * newZoomFactor) / 2f - boundingRect.Left * newZoomFactor;
            float centeredOffsetY = (Height - boundingRect.Height * newZoomFactor) / 2f - boundingRect.Top * newZoomFactor;
            Point newPanOffset = new Point((int)centeredOffsetX, (int)centeredOffsetY);

            initialZoomFactor = zoomFactor;
            initialPanOffset = panOffset;
            targetZoomFactor = newZoomFactor;
            targetPanOffset = newPanOffset;
            animationProgress = 0;

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