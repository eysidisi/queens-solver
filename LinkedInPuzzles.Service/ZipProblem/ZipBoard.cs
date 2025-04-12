namespace LinkedInPuzzles.Service.ZipProblem
{
    // Represents the overall board.
    public class ZipBoard
    {
        public int Rows { get; private set; }
        public int Cols { get; private set; }

        // 2D array representing the board.
        public ZipNode[,] Nodes { get; private set; }

        // Mapping order numbers (if any) to nodes for quick lookup.
        public Dictionary<int, ZipNode> OrderMap { get; private set; }

        public ZipBoard(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            Nodes = new ZipNode[rows, cols];
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
                    Nodes[i, j] = new ZipNode(i, j);
                }
            }
        }

        // Set the fixed order for a particular cell.
        // If order is non-zero, it is added to the OrderMap.
        public void SetNodeOrder(int row, int col, int order)
        {
            if (IsValidCoordinate(row, col))
            {
                Nodes[row, col].Order = order;
                if (order != 0)
                {
                    OrderMap[order] = Nodes[row, col];
                }
            }
        }

        // Helper method to get a node at the specified coordinate.
        public ZipNode GetNode(int row, int col)
        {
            if (IsValidCoordinate(row, col))
            {
                return Nodes[row, col];
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
        public void ResetAndSetupNeighbors(Func<ZipNode, ZipNode, bool> connectivityPredicate)
        {
            // Clear all existing neighbors.
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    Nodes[i, j].Neighbors.Clear();
                }
            }
            // Define the four potential directions: up, down, left, right.
            int[] dRow = { -1, 1, 0, 0 };
            int[] dCol = { 0, 0, -1, 1 };

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    ZipNode current = Nodes[i, j];
                    for (int k = 0; k < dRow.Length; k++)
                    {
                        int newRow = i + dRow[k];
                        int newCol = j + dCol[k];
                        if (IsValidCoordinate(newRow, newCol))
                        {
                            ZipNode neighbor = Nodes[newRow, newCol];
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
        /// <summary>
        /// Returns a more compact representation of blocked cells as a list of wall positions.
        /// Each tuple contains (row, col, isHorizontal) where:
        /// - row, col: coordinates of the cell
        /// - isHorizontal: true if wall is above the cell, false if wall is to the left
        /// </summary>
        public List<(int Row, int Col, bool IsHorizontal)> GetWalls()
        {
            List<(int Row, int Col, bool IsHorizontal)> walls = new List<(int Row, int Col, bool IsHorizontal)>();

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    ZipNode currentNode = Nodes[row, col];

                    // Check for horizontal wall (with cell above)
                    if (row > 0)
                    {
                        ZipNode nodeAbove = Nodes[row - 1, col];
                        if (!currentNode.Neighbors.Contains(nodeAbove))
                        {
                            walls.Add((row, col, true));
                        }
                    }

                    // Check for vertical wall (with cell to the left)
                    if (col > 0)
                    {
                        ZipNode nodeLeft = Nodes[row, col - 1];
                        if (!currentNode.Neighbors.Contains(nodeLeft))
                        {
                            walls.Add((row, col, false));
                        }
                    }
                }
            }

            return walls;
        }
        public bool IsValid()
        {
            // check if numbers are unique also no missing numbers
            HashSet<int> numbers = new HashSet<int>();
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    int order = Nodes[i, j].Order;
                    if (order != 0)
                    {
                        if (numbers.Contains(order))
                        {
                            return false; // Duplicate number found
                        }
                        numbers.Add(order);
                    }
                }
            }

            // Check if any missing number, for example if there's 1 and 3 but no 2
            if (numbers.Count > 0)
            {
                int maxNumber = numbers.Max();

                // Ensure we have a continuous sequence from 1 to maxNumber
                for (int i = 1; i <= maxNumber; i++)
                {
                    if (!numbers.Contains(i))
                    {
                        return false; // Missing number in sequence
                    }
                }
            }

            return true;
        }
    }
}


