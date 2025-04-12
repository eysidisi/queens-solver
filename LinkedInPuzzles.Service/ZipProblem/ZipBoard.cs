namespace LinkedInPuzzles.Service.ZipProblem
{
    // Represents the overall board.
    public class ZipBoard
    {
        public int Rows { get; private set; }
        public int Cols { get; private set; }

        public ZipNode[,] Nodes { get; private set; }

        public Dictionary<int, ZipNode> OrderMap { get; private set; }

        public ZipBoard(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            Nodes = new ZipNode[rows, cols];
            OrderMap = new Dictionary<int, ZipNode>();
            InitializeBoard();
        }

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

        public ZipNode GetNode(int row, int col)
        {
            if (IsValidCoordinate(row, col))
            {
                return Nodes[row, col];
            }
            return null;
        }

        private bool IsValidCoordinate(int row, int col)
        {
            return row >= 0 && row < Rows && col >= 0 && col < Cols;
        }

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

        /// <summary>
        /// Validates the board configuration and throws exceptions with detailed messages for any validation failures.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the board configuration is invalid with details about the specific issue.</exception>
        public void ValidateBoard()
        {
            // check if numbers are unique also no missing numbers
            HashSet<int> numbers = new HashSet<int>();
            Dictionary<int, (int Row, int Col)> numberPositions = new Dictionary<int, (int Row, int Col)>();

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    int order = Nodes[i, j].Order;
                    if (order != 0)
                    {
                        if (numbers.Contains(order))
                        {
                            var existingPos = numberPositions[order];
                            throw new InvalidOperationException(
                                $"Duplicate number {order} found at positions [{existingPos.Row},{existingPos.Col}] and [{i},{j}]");
                        }
                        numbers.Add(order);
                        numberPositions[order] = (i, j);
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
                        throw new InvalidOperationException(
                            $"Missing number {i} in sequence. The sequence must be continuous from 1 to {maxNumber}");
                    }
                }
            }
        }

    }
}


