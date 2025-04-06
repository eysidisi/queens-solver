using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using QueensProblem.Service.ZipProblem;

namespace ZipProblem.Tests
{
    public class ZipSolverTests
    {
        private void VerifySolutionOrder(List<ZipNode> solution, Dictionary<int, ZipNode> orderMap)
        {
            var orderedNodes = solution.Where(node => node.Order != 0).ToList();
            var expectedOrder = orderMap.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToList();
            Assert.Equal(expectedOrder, orderedNodes);
        }


        [Fact]
        public void TestZipSolver_Medium()
        {
            int rows = 4, cols = 4;
            ZipBoard board = new ZipBoard(rows, cols);

            board.SetNodeOrder(0, 0, 1);
            board.SetNodeOrder(1, 3, 2);
            board.SetNodeOrder(3, 0, 3);

            HashSet<(int, int, int, int)> blockedConnections = new HashSet<(int, int, int, int)>
            {
                (1, 1, 1, 2)
            };

            board.ResetAndSetupNeighbors((a, b) => !blockedConnections.Contains((a.Row, a.Col, b.Row, b.Col)) &&
                                           !blockedConnections.Contains((b.Row, b.Col, a.Row, a.Col)));

            ZipSolver solver = new ZipSolver(board);
            List<ZipNode> solution = solver.Solve();

            Assert.NotNull(solution);
            Assert.Equal(rows * cols, solution.Count);
            VerifySolutionOrder(solution, board.OrderMap);
        }

        // This test case represents zip_6x6_1.png file
        [Fact]
        public void TestZipSolver_ImageBoard()
        {
            int rows = 6, cols = 6;
            ZipBoard board = new ZipBoard(rows, cols);

            // Set numbered nodes according to the image:
            board.SetNodeOrder(4, 0, 1);
            board.SetNodeOrder(1, 2, 2);
            board.SetNodeOrder(2, 5, 3);
            board.SetNodeOrder(4, 3, 4);
            board.SetNodeOrder(1, 5, 5);
            board.SetNodeOrder(3, 0, 6);

            // Blocked connections matching the black bars in the image:
            //   Between row 4, col 0 and row 4, col 1
            //   Between row 1, col 3 and row 1, col 4
            HashSet<(int, int, int, int)> blockedConnections = new HashSet<(int, int, int, int)>
                {
                    (3, 0, 4, 0),
                    (1, 5, 2, 5)
                };

            board.ResetAndSetupNeighbors((a, b) =>
                 !blockedConnections.Contains((a.Row, a.Col, b.Row, b.Col)) &&
                 !blockedConnections.Contains((b.Row, b.Col, a.Row, a.Col)));

            ZipSolver solver = new ZipSolver(board);
            List<ZipNode> solution = solver.Solve();

            Assert.NotNull(solution);
            Assert.Equal(rows * cols, solution.Count);
            VerifySolutionOrder(solution, board.OrderMap);
        }
    }
}
