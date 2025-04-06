namespace QueensProblem.Service.ZipProblem
{
    public class ZipSolver
    {
        private readonly ZipBoard board;

        private int totalAvailable;
        private Dictionary<(int, int), bool> visited;
        private List<ZipNode> solutionPath;
        private int nextFixedExpected;
        private int highestOrder; // Track the highest order number on the board
        private ZipNode highestOrderNode; // The node with the highest order

        public ZipSolver(ZipBoard board)
        {
            this.board = board;
        }

        public List<ZipNode> Solve()
        {
            totalAvailable = board.Rows * board.Cols; // Since all cells are available.

            // Find the highest order node
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
                System.Diagnostics.Debug.WriteLine("No starting node available.");
                return null;
            }

            if (startNode.Order != 0 && startNode.Order != nextFixedExpected)
                return null;

            visited[(startNode.Row, startNode.Col)] = true;
            solutionPath.Add(startNode);
            nextFixedExpected++;

            // Try to find a path
            bool success = false;
            
            // If we have a highest order node, we need a different strategy to ensure it's the last node
            if (highestOrderNode != null)
            {
                // Remove the highest order node from being visited unless it's the last node
                success = HamiltonianPathWithEndNode(startNode);
            }
            else
            {
                // Use original algorithm if there's no specified end node
                success = HamiltonianBacktrack(startNode);
            }
            
            return success ? solutionPath : null;
        }

        private bool HamiltonianPathWithEndNode(ZipNode current)
        {
            // If we've visited totalAvailable-1 nodes (all except the end node)
            if (solutionPath.Count == totalAvailable - 1)
            {
                // Check if our current position is adjacent to the highest order node
                if (current.Neighbors.Contains(highestOrderNode))
                {
                    // Add the end node and we're done
                    visited[(highestOrderNode.Row, highestOrderNode.Col)] = true;
                    solutionPath.Add(highestOrderNode);
                    return true;
                }
                return false; // No path to end node
            }

            // Skip visiting the highest order node until the end
            foreach (ZipNode neighbor in current.Neighbors)
            {
                // Skip the highest order node unless we're ready for it
                if (neighbor == highestOrderNode)
                    continue;
                
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

                    if (HamiltonianPathWithEndNode(neighbor))
                        return true;

                    if (fixedIncremented)
                        nextFixedExpected--;
                    solutionPath.RemoveAt(solutionPath.Count - 1);
                    visited[(neighbor.Row, neighbor.Col)] = false;
                }
            }
            return false;
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
