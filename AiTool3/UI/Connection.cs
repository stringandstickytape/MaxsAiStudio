namespace AiTool3.UI
{
    public partial class NetworkDiagramControl
    {
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
}