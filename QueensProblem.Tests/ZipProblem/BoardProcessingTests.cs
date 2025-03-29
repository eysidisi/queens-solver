using Xunit;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Drawing;
using System.IO;
using Emgu.CV.Structure;
using QueensProblem.Service.QueensProblem.ImageProcessing;

namespace ZipProblem.Tests
{
    public class BoardProcessingTests : IDisposable
    {
        private readonly DebugHelper _debugHelper;
        private readonly BoardDetector _boardDetector;
        private readonly List<Mat> _disposableResources;

        public BoardProcessingTests()
        {
            _debugHelper = new DebugHelper();
            _boardDetector = new BoardDetector(_debugHelper);
            _disposableResources = new List<Mat>();
        }

        public void Dispose()
        {
            foreach (var resource in _disposableResources)
            {
                resource?.Dispose();
            }
        }

        private Mat CreateTestBoard(int rows, int columns, int size = 450)
        {
            Mat board = new Mat(size, size, DepthType.Cv8U, 3);
            _disposableResources.Add(board);
            board.SetTo(new MCvScalar(255, 255, 255)); // White background

            // Draw horizontal lines
            int cellHeight = size / rows;
            for (int i = 0; i <= rows; i++)
            {
                CvInvoke.Line(board,
                    new Point(0, i * cellHeight),
                    new Point(size, i * cellHeight),
                    new MCvScalar(0, 0, 0), 2);
            }

            // Draw vertical lines
            int cellWidth = size / columns;
            for (int i = 0; i <= columns; i++)
            {
                CvInvoke.Line(board,
                    new Point(i * cellWidth, 0),
                    new Point(i * cellWidth, size),
                    new MCvScalar(0, 0, 0), 2);
            }

            return board;
        }

        [Theory]
        [InlineData(8, 8)]  // Standard chess board
        [InlineData(6, 6)]  // Smaller board
        [InlineData(10, 10)] // Larger board
        public void DetectGridDimensions_ShouldReturnCorrectDimensions(int expectedRows, int expectedColumns)
        {
            // Arrange
            using var testBoard = CreateTestBoard(expectedRows, expectedColumns);

            // Act
            var (rows, columns) = _boardDetector.DetectGridDimensions(testBoard);

            // Assert
            Assert.Equal(expectedRows, rows);
            Assert.Equal(expectedColumns, columns);
        }


        [Theory]
        [InlineData("zip_6x6_1.png", 6, 6)]
        public void ExtractBoardAndAnalyze_WithRealImages_ShouldReturnCorrectDimensions(string imageName, int expectedRows, int expectedColumns)
        {
            // Arrange
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZipProblem/ZipTestImages", imageName);

            // Make sure the test image exists
            if (!File.Exists(imagePath))
            {
                // Try looking in a different location relative to the test project
                string projectDir = Directory.GetCurrentDirectory();
                while (projectDir != null && !Directory.Exists(Path.Combine(projectDir, "ZipProblem/ZipTestImages")))
                {
                    var parentDir = Directory.GetParent(projectDir);
                    if (parentDir == null) break;
                    projectDir = parentDir.FullName;
                }

                if (projectDir != null)
                {
                    imagePath = Path.Combine(projectDir, "ZipProblem/ZipTestImages", imageName);
                }
            }

            Assert.True(File.Exists(imagePath), $"Test image not found: {imagePath}");

            // Load the image
            Mat image = CvInvoke.Imread(imagePath, ImreadModes.Color);
            _disposableResources.Add(image);

            // Act - Extract the board and analyze in one step
            var (warpedBoard, rows, columns) = _boardDetector.ExtractBoardAndAnalyze(image);

            // Assert
            Assert.NotNull(warpedBoard);
            Assert.Equal(expectedRows, rows);
            Assert.Equal(expectedColumns, columns);

            // Save the debug image for visual inspection
            if (imageName == "queens_8x8.png")
            {
                string debugDir = Path.Combine(Directory.GetCurrentDirectory(), "TestDebugImages");
                Directory.CreateDirectory(debugDir);
                warpedBoard.Save(Path.Combine(debugDir, $"ExtractBoardAndAnalyze_{imageName}.png"));
            }
        }
    }
}