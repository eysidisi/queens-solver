using LinkedInPuzzles.Service.QueensProblem.Algorithm;

namespace LinkedInPuzzles.Tests.Queens
{
    /// <summary>
    /// Helper methods for verifying queen placement solutions in tests
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// Verifies that each color on the board has exactly one queen placed on it
        /// </summary>
        /// <param name="queens">The array of placed queens</param>
        /// <param name="colorBoard">The color constraint board</param>
        public static void VerifyExactlyOneQueenPerColor(Queen[] queens, string[,] colorBoard)
        {
            // Get all unique colors on the board
            var allColors = new HashSet<string>();
            int rows = colorBoard.GetLength(0);
            int cols = colorBoard.GetLength(1);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    allColors.Add(colorBoard[row, col]);
                }
            }

            // Create a dictionary to count queens per color
            var queensPerColor = new Dictionary<string, int>();
            foreach (var color in allColors)
            {
                queensPerColor[color] = 0;
            }

            // Count queens for each color
            foreach (var queen in queens)
            {
                string color = colorBoard[queen.Row, queen.Col];
                queensPerColor[color]++;
            }

            // Verify each color has exactly one queen
            foreach (var color in allColors)
            {
                int count = queensPerColor[color];
                Assert.Equal(1, count);
            }
        }

        /// <summary>
        /// Verifies that no queen threatens another queen according to chess rules
        /// with the special constraint that queens can't touch diagonally
        /// </summary>
        /// <param name="queens">The array of placed queens</param>
        public static void VerifyQueensPlacement(Queen[] queens)
        {
            int n = queens.Length;

            // Check each pair of queens
            for (int i = 0; i < n; i++)
            {
                Queen queen1 = queens[i];

                for (int j = i + 1; j < n; j++)
                {
                    Queen queen2 = queens[j];

                    // Check if queens are in same row
                    Assert.NotEqual(queen1.Row, queen2.Row);

                    // Check if queens are in same column
                    Assert.NotEqual(queen1.Col, queen2.Col);

                    // Check diagonal threats
                    int rowDiff = Math.Abs(queen1.Row - queen2.Row);
                    int colDiff = Math.Abs(queen1.Col - queen2.Col);

                    // Special case: queens can't touch diagonally in this problem
                    Assert.False(rowDiff == 1 && colDiff == 1,
                        $"Queens at ({queen1.Row}, {queen1.Col}) and ({queen2.Row}, {queen2.Col}) are touching diagonally");
                }
            }
        }
    }
}