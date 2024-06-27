namespace AiTool3.UI
{
    public partial class NetworkDiagramControl
    {
        public class NodeClickEventArgs : EventArgs
        {
            public Node ClickedNode { get; }

            public NodeClickEventArgs(Node clickedNode)
            {
                ClickedNode = clickedNode;
            }
        }
    }
}