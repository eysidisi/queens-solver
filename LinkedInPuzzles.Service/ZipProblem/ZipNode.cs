namespace LinkedInPuzzles.Service.ZipProblem
{
    // Represents a single cell (node) on the board.
    public class ZipNode
    {
        // The row and column of this node.
        public int Row { get; set; }
        public int Col { get; set; }

        // If a cell has a fixed selection order (e.g. 1, 2, 3, …),
        // a value of 0 means "no fixed order".
        public int Order { get; set; }

        // List of neighboring nodes that are connected (i.e. no wall between).
        public List<ZipNode> Neighbors { get; private set; }

        public ZipNode(int row, int col, int order = 0)
        {
            Row = row;
            Col = col;
            Order = order;
            Neighbors = new List<ZipNode>();
        }

        // Adds a neighbor if it isn't already added.
        public void AddNeighbor(ZipNode neighbor)
        {
            if (!Neighbors.Contains(neighbor))
            {
                Neighbors.Add(neighbor);
            }
        }

        public override string ToString()
        {
            return $"Node({Row},{Col}) Order:{Order}";
        }
    }
}
