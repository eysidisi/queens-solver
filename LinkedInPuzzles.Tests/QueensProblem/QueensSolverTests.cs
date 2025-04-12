using LinkedInPuzzles.Service.QueensProblem.Algorithm;

namespace LinkedInPuzzles.Tests.Queens
{
    /// <summary>
    /// Tests for the QueensSolver class to verify it can solve the Queens Problem
    /// with various color constraints and board sizes
    /// </summary>
    public class QueensSolverTests
    {
        [Fact]
        public void Solve_4x4BoardWithColorConstraints_ReturnsSolution()
        {
            // Arrange
            string[,] colorBoard = new string[,]
            {
                { "Red", "Green", "Blue", "Yellow" },
                { "Red", "Green", "Blue", "Yellow" },
                { "Red", "Green", "Blue", "Yellow" },
                { "Red", "Green", "Blue", "Yellow" }
            };

            var solver = new QueensSolver();

            // Act
            var solution = solver.Solve(colorBoard);

            // Assert
            Assert.NotNull(solution);
            Assert.Equal(4, solution.Length);

            // Verify solution rules - queens don't threaten each other
            Helpers.VerifyQueensPlacement(solution);

            // Verify solution constraints - one queen per color
            Helpers.VerifyExactlyOneQueenPerColor(solution, colorBoard);
        }

        [Fact]
        public void Solve_9x9BoardWithColorConstraints_ReturnsSolution()
        {
            // Arrange
            string[,] colorBoard = new string[,]
            {
                { "Blue",   "Blue",   "Blue",   "Blue",   "Orange",  "Orange",  "Orange",  "Pink",    "Pink"   },
                { "Blue",   "Green",  "Blue",   "Purple", "Purple",  "Purple",  "Orange",  "Orange",  "Pink"   },
                { "Blue",   "Green",  "Purple", "Purple", "Red",     "Purple",  "Purple",  "Orange",  "Pink"   },
                { "Blue",   "Green",  "Purple", "Purple", "Red",     "Purple",  "Purple",  "Orange",  "Pink"   },
                { "Green",  "Green",  "Green",  "Purple", "Purple",  "Purple",  "Yellow",  "Yellow",  "Pink"   },
                { "Green",  "Green",  "White",  "White",  "Purple",  "Gray",    "Gray",    "Yellow",  "Yellow" },
                { "Green",  "Green",  "White",  "Purple", "Purple",  "Purple",  "Gray",    "Gray",    "Yellow" },
                { "Green",  "Green",  "White",  "White",  "Purple",  "White",   "White",   "White",   "Yellow" },
                { "Green",  "White",  "White",  "White",  "White",   "White",   "White",   "White",   "Yellow" }
            };

            var solver = new QueensSolver();

            // Act
            var solution = solver.Solve(colorBoard);

            // Assert
            Assert.NotNull(solution);
            Assert.Equal(9, solution.Length);
            Helpers.VerifyQueensPlacement(solution);
            Helpers.VerifyExactlyOneQueenPerColor(solution, colorBoard);
        }

        [Fact]
        public void Solve_11x11BoardWithColorConstraints_ReturnsSolution()
        {
            // Arrange
            var colorBoard = new string[,]
            {
                { "color1", "color1", "color1", "color2", "color2", "color2", "color2", "color3", "color3", "color3", "color2" },
                { "color2", "color1", "color4", "color4", "color4", "color2", "color2", "color2", "color3", "color2", "color2" },
                { "color2", "color1", "color5", "color4", "color6", "color6", "color6", "color2", "color3", "color2", "color2" },
                { "color2", "color2", "color5", "color4", "color2", "color6", "color7", "color2", "color2", "color2", "color2" },
                { "color2", "color5", "color5", "color5", "color2", "color6", "color7", "color7", "color7", "color8", "color2" },
                { "color2", "color2", "color2", "color2", "color2", "color2", "color7", "color8", "color8", "color8", "color2" },
                { "color2", "color2", "color2", "color2", "color2", "color2", "color2", "color2", "color2", "color8", "color2" },
                { "color2", "color2", "color2", "color2", "color9", "color2", "color9", "color2", "color2", "color2", "color2" },
                { "color2", "color2", "color2", "color10", "color9", "color9", "color9", "color2", "color2", "color11", "color2" },
                { "color2", "color2", "color2", "color10", "color9", "color2", "color9", "color2", "color2", "color11", "color2" },
                { "color2", "color2", "color10", "color10", "color10", "color2", "color2", "color2", "color11", "color11", "color11" }
            };

            var solver = new QueensSolver();

            // Act
            var solution = solver.Solve(colorBoard);

            // Assert
            Assert.NotNull(solution);
            Assert.Equal(11, solution.Length);
            Helpers.VerifyQueensPlacement(solution);
            Helpers.VerifyExactlyOneQueenPerColor(solution, colorBoard);
        }

        [Fact]
        public void Solve_InvalidBoard_ReturnsNull()
        {
            // Arrange - Create a board with color constraints that make a solution impossible
            // This board has only one color, so it's impossible to place 3 non-threatening queens
            string[,] unsolvableBoard = new string[,]
            {
                { "Red", "Red", "Red" },
                { "Red", "Red", "Red" },
                { "Red", "Red", "Red" }
            };

            var solver = new QueensSolver();

            // Act
            var solution = solver.Solve(unsolvableBoard);

            // Assert
            Assert.Null(solution);
        }
    }
}