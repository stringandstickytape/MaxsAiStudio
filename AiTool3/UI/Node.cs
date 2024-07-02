namespace AiTool3.UI
{
    public partial class NetworkDiagramControl
    {
        public class Node
        {
            public string Label { get; set; }
            public Point Location { get; set; }
            public Rectangle Bounds => new Rectangle(Location.X - 175, Location.Y - 35, 350, 70);

            public Color? BackColor = null;
            public Color? ForeColor = null;
            public Color? BorderColor = null;

            public string Guid { get; set; }
            public string NodeInfoLabel { get; set; }

            public bool IsDisabled { get; set; }

            public Node(string label, Point location, string guid, string nodeInfoLabel, bool isDisabled)
            {
                Label = label;
                Location = location;
                Guid = guid;
                NodeInfoLabel = nodeInfoLabel;
                IsDisabled = isDisabled;
            }
        }
    }
}