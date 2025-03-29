using Xunit;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Drawing;
using System.IO;
using Emgu.CV.Structure;
using QueensProblem.Service.QueensProblem.ImageProcessing;
using QueensProblem.Service;

namespace QueensProblem.Tests.Queens
{
    public class BoardDetectorTests : IDisposable
    {
        private readonly DebugHelper _debugHelper;
        private readonly BoardDetector _boardDetector;
        private readonly List<Mat> _disposableResources;

        public BoardDetectorTests()
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

        [Fact]
        public void DetectGridDimensions_WithNoise_ShouldReturnCorrectDimensions()
        {
            // Arrange
            using var board = CreateTestBoard(8, 8);
            
            // Add some random noise
            var random = new Random(42);
            for (int i = 0; i < 100; i++)
            {
                int x = random.Next(0, board.Width);
                int y = random.Next(0, board.Height);
                CvInvoke.Circle(board, new Point(x, y), 2, new MCvScalar(0, 0, 0), -1);
            }

            // Act
            var (rows, columns) = _boardDetector.DetectGridDimensions(board);

            // Assert
            Assert.Equal(8, rows);
            Assert.Equal(8, columns);
        }

        [Fact]
        public void GroupNearbyPositions_ShouldGroupCorrectly()
        {
            // Need to access the private method via reflection
            var methodInfo = typeof(BoardDetector).GetMethod("GroupNearbyPositions", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(methodInfo); // Make sure the method was found
            
            // Test case 1: Positions that should be grouped
            var positions1 = new List<int> { 10, 15, 12, 60, 65, 100, 102 };
            var threshold = 10;
            
            var result1 = methodInfo!.Invoke(_boardDetector, new object[] { positions1, threshold }) as List<int>;
            
            // Should result in 3 groups: around 12, around 62, and around 101
            Assert.NotNull(result1);
            Assert.Equal(3, result1!.Count);
            Assert.Contains(result1, x => Math.Abs(x - 12) < 5);
            Assert.Contains(result1, x => Math.Abs(x - 62) < 5);
            Assert.Contains(result1, x => Math.Abs(x - 101) < 5);
            
            // Test case 2: Out of order positions
            var positions2 = new List<int> { 100, 10, 60, 12, 65, 15, 102 };
            
            var result2 = methodInfo.Invoke(_boardDetector, new object[] { positions2, threshold }) as List<int>;
            
            // Should still result in the same 3 groups
            Assert.NotNull(result2);
            Assert.Equal(3, result2!.Count);
            Assert.Contains(result2, x => Math.Abs(x - 12) < 5);
            Assert.Contains(result2, x => Math.Abs(x - 62) < 5);
            Assert.Contains(result2, x => Math.Abs(x - 101) < 5);
            
            // Test case 3: Positions that are all within threshold
            var positions3 = new List<int> { 50, 55, 52, 53, 57 };
            var threshold2 = 15;
            
            var result3 = methodInfo.Invoke(_boardDetector, new object[] { positions3, threshold2 }) as List<int>;
            
            // Should result in a single group
            Assert.NotNull(result3);
            Assert.Single(result3);
            Assert.Contains(result3!, x => Math.Abs(x - 53) < 5);
        }

        [Theory]
        [InlineData("queens_8x8.png", 8, 8)]
        [InlineData("queens_11x11.png", 11, 11)]
        [InlineData("queens_9x9.jpg", 9, 9)]
        public void ExtractBoardAndAnalyze_WithRealImages_ShouldReturnCorrectDimensions(string imageName, int expectedRows, int expectedColumns)
        {
            // Arrange
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QueensProblem/TestImages", imageName);
            
            // Make sure the test image exists
            if (!File.Exists(imagePath))
            {
                // Try looking in a different location relative to the test project
                string projectDir = Directory.GetCurrentDirectory();
                while (projectDir != null && !Directory.Exists(Path.Combine(projectDir, "TestImages")))
                {
                    var parentDir = Directory.GetParent(projectDir);
                    if (parentDir == null) break;
                    projectDir = parentDir.FullName;
                }
                
                if (projectDir != null)
                {
                    imagePath = Path.Combine(projectDir, "TestImages", imageName);
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