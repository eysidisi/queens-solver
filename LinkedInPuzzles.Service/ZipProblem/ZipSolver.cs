
namespace LinkedInPuzzles.Service.ZipProblem
{
    public class ZipSolver
    {
        private readonly ZipBoard board;
        private int totalAvailable;
        private bool[,] visited;
        private List<ZipNode> solutionPath;
        private int nextFixedExpected;
        private int highestOrder;
        private ZipNode highestOrderNode;

        public ZipSolver(ZipBoard board)
        {
            this.board = board;
        }

        public List<ZipNode> Solve()
        {
            totalAvailable = board.Rows * board.Cols;
            highestOrder = 0;
            highestOrderNode = null;

            foreach (var orderPair in board.OrderMap)
            {
                if (orderPair.Key > highestOrder)
                {
                    highestOrder = orderPair.Key;
                    highestOrderNode = orderPair.Value;
                }
            }

            visited = new bool[board.Rows, board.Cols];
            solutionPath = new List<ZipNode>();
            nextFixedExpected = 1;

            // Select starting node
            ZipNode startNode = board.OrderMap.TryGetValue(1, out var fixedStart)
                                    ? fixedStart
                                    : board.GetNode(0, 0);

            if (startNode == null || (startNode.Order != 0 && startNode.Order != nextFixedExpected))
            {
                return null;
            }

            visited[startNode.Row, startNode.Col] = true;
            solutionPath.Add(startNode);
            nextFixedExpected++;

            // Use the merged recursive function.
            bool success = HamiltonianRecursive(startNode, highestOrderNode != null);
            return success ? solutionPath : null;
        }

        private bool HamiltonianRecursive(ZipNode current, bool enforceEndNode)
        {
            // Base case for enforcing an end node: leave one slot for highestOrderNode.
            if (enforceEndNode && solutionPath.Count == totalAvailable - 1)
            {
                if (current.Neighbors.Contains(highestOrderNode))
                {
                    visited[highestOrderNode.Row, highestOrderNode.Col] = true;
                    solutionPath.Add(highestOrderNode);
                    return true;
                }
                return false;
            }
            // General base case.
            if (!enforceEndNode && solutionPath.Count == totalAvailable)
            {
                return true;
            }

            // Apply a heuristic: optionally sort neighbors by unvisited count.
            var neighbors = current.Neighbors.OrderBy(n => CountUnvisitedNeighbors(n)).ToList();

            foreach (ZipNode neighbor in neighbors)
            {
                if (!visited[neighbor.Row, neighbor.Col])
                {
                    // Respect fixed order constraint.
                    if (neighbor.Order != 0 && neighbor.Order != nextFixedExpected)
                        continue;

                    visited[neighbor.Row, neighbor.Col] = true;
                    solutionPath.Add(neighbor);
                    bool incrementedFixed = false;
                    if (neighbor.Order != 0)
                    {
                        nextFixedExpected++;
                        incrementedFixed = true;
                    }

                    if (HamiltonianRecursive(neighbor, enforceEndNode))
                        return true;

                    // Backtrack.
                    if (incrementedFixed)
                        nextFixedExpected--;
                    solutionPath.RemoveAt(solutionPath.Count - 1);
                    visited[neighbor.Row, neighbor.Col] = false;
                }
            }
            return false;
        }

        private int CountUnvisitedNeighbors(ZipNode node)
        {
            int count = 0;
            foreach (var neighbor in node.Neighbors)
            {
                if (!visited[neighbor.Row, neighbor.Col])
                    count++;
            }
            return count;
        }
    }
}