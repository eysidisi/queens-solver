using Emgu.CV.CvEnum;
using Emgu.CV;
using System.Text.RegularExpressions;
using System.Drawing;
using QueensProblem.Service.QueensProblem.ImageProcessing;
using QueensProblem.Service;

namespace QueensProblem.Tests.Queens
{
    /// <summary>
    /// Tests for the BoardProcessor class that handles chess board image processing
    /// </summary>
    public class BoardProcessorTests
    {
        // Constants for directories and supported file types
        private const string TestImagesDirectory = "QueensProblem/TestImages";
        private static readonly string[] SupportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };
        
        // Dependencies needed for testing
        private readonly DebugHelper _debugHelper;
        private readonly ColorAnalyzer _colorAnalyzer;
        private readonly BoardDetector _boardDetector;
        private readonly QueensBoardProcessor _boardProcessor;
        
        /// <summary>
        /// Constructor initializes common dependencies for all tests
        /// </summary>
        public BoardProcessorTests()
        {
            // Arrange shared dependencies (debug disabled to avoid test output files)
            _debugHelper = new DebugHelper(false);
            _colorAnalyzer = new ColorAnalyzer();
            _boardDetector = new BoardDetector(_debugHelper);
            _boardProcessor = new QueensBoardProcessor(_colorAnalyzer, _debugHelper);
        }

        [Fact]
        public void Process_Queens8x8_ReturnsCorrectColorPattern()
        {
            // Expected color pattern for 8x8 board
            string[,] expectedPattern = new string[,]
            {
                { "color1", "color1", "color2", "color2", "color2", "color3", "color3", "color3" },
                { "color1", "color1", "color2", "color2", "color2", "color3", "color3", "color3" },
                { "color4", "color1", "color2", "color2", "color2", "color3", "color3", "color3" },
                { "color4", "color1", "color5", "color5", "color5", "color5", "color3", "color3" },
                { "color4", "color1", "color5", "color5", "color5", "color5", "color6", "color6" },
                { "color4", "color5", "color5", "color7", "color7", "color6", "color6", "color6" },
                { "color4", "color8", "color7", "color7", "color7", "color6", "color6", "color6" },
                { "color8", "color8", "color8", "color7", "color7", "color6", "color6", "color6" }
            };
            
            // Find the queens_8x8.png test image
            string targetFile = "queens_8x8.png";
            string imagePath = FindTestImage(targetFile);
            
            Assert.NotNull(imagePath);
            Console.WriteLine($"Testing color processing of {targetFile}");
            
            // Process the image
            ProcessAndVerifyImage(imagePath, colorImage => _boardDetector.ExtractBoardAndAnalyze(colorImage), expectedPattern);
        }
        
        [Fact]
        public void Process_Queens11x11_ReturnsCorrectColorPattern()
        {
            // Expected color pattern for 11x11 board
            string[,] expectedPattern = new string[,]
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
            
            // Find the queens_11x11.png test image
            string targetFile = "queens_11x11.png";
            string imagePath = FindTestImage(targetFile);
            
            Assert.NotNull(imagePath);
            Console.WriteLine($"Testing color processing of {targetFile}");
            
            // Process the image
            ProcessAndVerifyImage(imagePath, colorImage => _boardDetector.ExtractBoardAndAnalyze(colorImage), expectedPattern);
        }
        
        // Helper method to find a specific test image
        private string FindTestImage(string targetFilename)
        {
            string testImagesPath = FindTestImagesDirectory();
            if (testImagesPath == null)
            {
                Console.WriteLine("Warning: TestImages directory not found.");
                return null;
            }
            
            foreach (var extension in SupportedExtensions)
            {
                string path = Path.Combine(testImagesPath, targetFilename);
                if (File.Exists(path))
                {
                    return path;
                }
                
                // Try with the extension if it's not already in the filename
                if (!targetFilename.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    path = Path.Combine(testImagesPath, targetFilename + extension);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Helper method to process an image and verify its color pattern
        /// </summary>
        private void ProcessAndVerifyImage(string imagePath, Func<Mat, (Bitmap, int, int)> extractBoard, string[,] expectedPattern)
        {
            // Arrange
            Mat colorImage = CvInvoke.Imread(imagePath, ImreadModes.Color);
            Bitmap boardImage = null;
            int rows = 0;
            
            try
            {
                // Extract the board from the image
                (boardImage, rows, _) = extractBoard(colorImage);
                
                // Act
                var result = _boardProcessor.ProcessBoardImage(boardImage, rows);
                
                // Assert
                AssertSameColorPattern(expectedPattern, result);
                Console.WriteLine($"Successfully verified color pattern for {Path.GetFileName(imagePath)}");
            }
            finally
            {
                // Clean up
                colorImage?.Dispose();
                boardImage?.Dispose();
            }
        }

        /// <summary>
        /// Helper method to verify that two color boards have the same pattern,
        /// regardless of the actual color names used (only the pattern matters)
        /// </summary>
        private void AssertSameColorPattern(string[,] expected, string[,] actual)
        {
            // First verify dimensions match
            Assert.Equal(expected.GetLength(0), actual.GetLength(0));
            Assert.Equal(expected.GetLength(1), actual.GetLength(1));

            var rows = expected.GetLength(0);
            var cols = expected.GetLength(1);

            // Create mappings for both directions to ensure bijective relationship
            var expectedToActualColors = new Dictionary<string, string>();
            var actualToExpectedColors = new Dictionary<string, string>();

            // Check each position and build/verify color mappings
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var expectedColor = expected[i, j];
                    var actualColor = actual[i, j];

                    if (expectedToActualColors.TryGetValue(expectedColor, out string? mappedActual))
                    {
                        // Verify this position matches the existing mapping
                        if (mappedActual != actualColor)
                        {
                            Assert.Fail($"Inconsistent color mapping at position [{i},{j}]: " +
                                $"Expected color pattern '{expectedColor}' was previously mapped to " +
                                $"'{mappedActual}' but found '{actualColor}'");
                        }
                    }
                    else if (actualToExpectedColors.TryGetValue(actualColor, out string? mappedExpected))
                    {
                        // If actual color is already mapped to a different expected color, that's an error
                        if (mappedExpected != expectedColor)
                        {
                            Assert.Fail($"Color mapping conflict at position [{i},{j}]: " +
                                $"Actual color '{actualColor}' was previously mapped to " +
                                $"'{mappedExpected}' but found '{expectedColor}'");
                        }
                    }
                    else
                    {
                        // Create new mapping
                        expectedToActualColors[expectedColor] = actualColor;
                        actualToExpectedColors[actualColor] = expectedColor;
                    }
                }
            }
        }
        
        private string FindTestImagesDirectory()
        {
            // First try in the current directory
            string dir = Path.Combine(Directory.GetCurrentDirectory(), TestImagesDirectory);
            
            if (Directory.Exists(dir))
                return dir;
            
            // Try looking relative to the current directory
            string projectDir = Directory.GetCurrentDirectory();
            while (projectDir != null && !Directory.Exists(Path.Combine(projectDir, TestImagesDirectory)))
            {
                var parentDir = Directory.GetParent(projectDir);
                if (parentDir == null) break;
                projectDir = parentDir.FullName;
            }
            
            if (projectDir != null)
            {
                dir = Path.Combine(projectDir, TestImagesDirectory);
                if (Directory.Exists(dir))
                    return dir;
            }
            
            return null;
        }
    }
}
