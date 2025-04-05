namespace QueensProblem.Service.ZipProblem
{
    public class ZipSolver
    {
        private readonly ZipBoard board;

        private int totalAvailable;
        private Dictionary<(int, int), bool> visited;
        private List<ZipNode> solutionPath;
        private int nextFixedExpected;

        public ZipSolver(ZipBoard board)
        {
            this.board = board;
        }

        public List<ZipNode> Solve()
        {
            totalAvailable = board.Rows * board.Cols; // Since all cells are available.

            // Initialize visited dictionary for all cells.
            visited = new Dictionary<(int, int), bool>();
            for (int i = 0; i < board.Rows; i++)
            {
                for (int j = 0; j < board.Cols; j++)
                {
                    visited[(i, j)] = false;
                }
            }

            solutionPath = new List<ZipNode>();
            nextFixedExpected = 1;

            // Choose starting node (must be the node labeled with "1" if present).
            ZipNode startNode = null;
            if (board.OrderMap.TryGetValue(1, out startNode))
            {
                // Found the required starting node
            }
            else
            {
                // If no fixed start node, pick any node as a fallback.
                startNode = board.GetNode(0, 0);
            }

            if (startNode == null)
            {
                Console.WriteLine("No starting node available.");
                return null;
            }

            if (startNode.Order != 0 && startNode.Order != nextFixedExpected)
                return null;

            visited[(startNode.Row, startNode.Col)] = true;
            solutionPath.Add(startNode);
            nextFixedExpected++;

            if (HamiltonianBacktrack(startNode))
                return solutionPath;
            else
                return null;
        }

        private bool HamiltonianBacktrack(ZipNode current)
        {
            if (solutionPath.Count == totalAvailable)
                return true;

            foreach (ZipNode neighbor in current.Neighbors)
            {
                if (!visited[(neighbor.Row, neighbor.Col)])
                {
                    if (neighbor.Order != 0 && neighbor.Order != nextFixedExpected)
                        continue;

                    visited[(neighbor.Row, neighbor.Col)] = true;
                    solutionPath.Add(neighbor);
                    bool fixedIncremented = false;
                    if (neighbor.Order != 0)
                    {
                        nextFixedExpected++;
                        fixedIncremented = true;
                    }

                    if (HamiltonianBacktrack(neighbor))
                        return true;

                    if (fixedIncremented)
                        nextFixedExpected--;
                    solutionPath.RemoveAt(solutionPath.Count - 1);
                    visited[(neighbor.Row, neighbor.Col)] = false;
                }
            }
            return false;
        }
    }
}
