namespace QueensProblem.Service.ZipProblem
{
    // Represents the overall board.
    public class ZipBoard
    {
        public int Rows { get; private set; }
        public int Cols { get; private set; }

        // 2D array representing the board.
        public ZipNode[,] Board { get; private set; }

        // Mapping order numbers (if any) to nodes for quick lookup.
        public Dictionary<int, ZipNode> OrderMap { get; private set; }

        public ZipBoard(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            Board = new ZipNode[rows, cols];
            OrderMap = new Dictionary<int, ZipNode>();
            InitializeBoard();
        }

        // Create all nodes with default order 0.
        private void InitializeBoard()
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    Board[i, j] = new ZipNode(i, j);
                }
            }
        }

        // Set the fixed order for a particular cell.
        // If order is non-zero, it is added to the OrderMap.
        public void SetNodeOrder(int row, int col, int order)
        {
            if (IsValidCoordinate(row, col))
            {
                Board[row, col].Order = order;
                if (order != 0)
                {
                    OrderMap[order] = Board[row, col];
                }
            }
        }

        // Helper method to get a node at the specified coordinate.
        public ZipNode GetNode(int row, int col)
        {
            if (IsValidCoordinate(row, col))
            {
                return Board[row, col];
            }
            return null;
        }

        // Check if the coordinates are within the board bounds.
        private bool IsValidCoordinate(int row, int col)
        {
            return row >= 0 && row < Rows && col >= 0 && col < Cols;
        }

        // Set up neighbors for each node.
        // The connectivityPredicate allows you to specify when two adjacent cells are connected.
        // For example, if there is a wall between two cells, connectivityPredicate would return false.
        public void SetupNeighbors(Func<ZipNode, ZipNode, bool> connectivityPredicate)
        {
            // Define the four potential directions: up, down, left, right.
            int[] dRow = { -1, 1, 0, 0 };
            int[] dCol = { 0, 0, -1, 1 };

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    ZipNode current = Board[i, j];
                    for (int k = 0; k < dRow.Length; k++)
                    {
                        int newRow = i + dRow[k];
                        int newCol = j + dCol[k];
                        if (IsValidCoordinate(newRow, newCol))
                        {
                            ZipNode neighbor = Board[newRow, newCol];
                            // Only add as a neighbor if the connectivity rule allows it.
                            if (connectivityPredicate(current, neighbor))
                            {
                                current.AddNeighbor(neighbor);
                            }
                        }
                    }
                }
            }
        }
    }
}


