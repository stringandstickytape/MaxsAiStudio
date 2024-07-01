using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Diagnostics;
using static AiTool3.UI.NetworkDiagramControl;

namespace AiTool3.UI
{
    [Designer(typeof(NetworkDiagramControlDesigner))]
    [ToolboxItem(true)]
    public partial class NetworkDiagramControl : Control
    {
        private List<Node> nodes;
        private List<Connection> connections;
        private Node selectedNode;
        private Node highlightedNode;
        private Point dragOffset;
        private float zoomFactor;
        private const float ZoomIncrement = 0.1f;
        private FitAllAnimation fitAllAnimation;

        private ContextMenuStrip contextMenu;
        public event EventHandler<MenuOptionSelectedEventArgs> MenuOptionSelected;
        public float ZoomFactor
        {
            get => zoomFactor;
            set
            {
                zoomFactor = value;
                Invalidate();
            }
        }

        public Point PanOffset
        {
            get => panOffset;
            set
            {
                panOffset = value;
                Invalidate();
            }
        }

        public const float MaxZoom = 3.0f;
        public const float MinZoom = 0.05f;
        private Point panOffset;
        private bool isPanning;
        private Point lastMousePosition;

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
            fitAllAnimation = new FitAllAnimation(this);
            contextMenu = new ContextMenuStrip();
            contextMenu.ItemClicked += ContextMenu_ItemClicked;
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
                using (GraphicsPath shadowPath = NetworkDiagramControlHelpers.CreateRoundedRectangle(bounds.X + 10, bounds.Y + 10, bounds.Width, bounds.Height, NodeCornerRadius))
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = Color.FromArgb(100, Color.Black);
                    shadowBrush.SurroundColors = new Color[] { Color.FromArgb(90, Color.Black) };
                    g.FillPath(shadowBrush, shadowPath);
                }
            }

            using (GraphicsPath path = NetworkDiagramControlHelpers.CreateRoundedRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, NodeCornerRadius))
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
                    g.DrawString(node.Label.Substring(0, node.Label.Length < 500 ? node.Label.Length : 500), Font, foregroundBrush, bounds, sf);
                }

                if (!string.IsNullOrEmpty(node.NodeInfoLabel))
                {
                    using (Font labelFont = new Font(Font.FontFamily, 8f, FontStyle.Bold))
                    using (SolidBrush labelBrush = new SolidBrush(Color.White))
                    {
                        SizeF labelSize = g.MeasureString(node.NodeInfoLabel, labelFont);
                        float labelX = bounds.Right - labelSize.Width;
                        float labelY = bounds.Bottom;
                        RectangleF labelRect = new RectangleF(labelX, labelY, labelSize.Width, labelSize.Height);

                        // fill rect 50% opacity
                        g.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Black)), labelRect);

                        g.DrawString(node.NodeInfoLabel, labelFont, labelBrush, labelX, labelY);
                    }
                }
            }
        }

        public Node GetNodeForGuid(string guid)
        {
            return nodes.FirstOrDefault(n => n.Guid == guid);
        }

        public void AddNode(string label, Point location, string guid, string nodeInfoLabel)
        {
            nodes.Add(new Node(label, location, guid, nodeInfoLabel));
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

            if (e.Button == MouseButtons.Right && selectedNode != null)
            {
                HighlightedNode = selectedNode;
                NodeClicked?.Invoke(this, new NodeClickEventArgs(selectedNode));
                ShowContextMenu(e.Location);
            }
            else if (selectedNode != null)
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
            fitAllAnimation.Start(nodes, zoomFactor, panOffset);
        }


        internal void CenterOnNode(Node node)
        {
            var centerPoint = new PointF(
                Width / 2f / zoomFactor - node.Location.X,
                Height / 2f / zoomFactor - node.Location.Y);

            panOffset = new Point((int)(centerPoint.X * zoomFactor), (int)(centerPoint.Y * zoomFactor));
            Invalidate();
        }



        private void ShowContextMenu(Point location)
        {
            contextMenu.Show(this, location);
        }

        private void ContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

            MenuOptionSelected?.Invoke(this, new MenuOptionSelectedEventArgs(highlightedNode, e.ClickedItem.Text));
        }

        public void SetContextMenuOptions(IEnumerable<string> options)
        {
            contextMenu.Items.Clear();
            foreach (var option in options)
            {
                contextMenu.Items.Add(option);
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

    public class MenuOptionSelectedEventArgs : EventArgs
    {
        public Node SelectedNode { get; }
        public string SelectedOption { get; }

        public MenuOptionSelectedEventArgs(Node selectedNode, string selectedOption)
        {
            SelectedNode = selectedNode;
            SelectedOption = selectedOption;
        }
    }

    

}