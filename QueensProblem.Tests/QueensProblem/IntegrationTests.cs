using Emgu.CV;
using QueensProblem.Service;
using QueensProblem.Service.QueensProblem.Algorithm;
using QueensProblem.Service.QueensProblem.ImageProcessing;

namespace QueensProblem.Tests.Queens
{
    /// <summary>
    /// Integration tests that verify the full end-to-end flow of the Queens Problem solution
    /// from image processing to solving the constraint-based problem
    /// </summary>
    public class IntegrationTests
    {
        private readonly DebugHelper _debugHelper;
        private readonly BoardDetector _boardDetector;
        private readonly ColorAnalyzer _colorAnalyzer;
        private readonly QueensBoardProcessor _boardProcessor;
        private const string TestImagesDirectory = "QueensProblem/TestImages";
        private static readonly string[] SupportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };

        /// <summary>
        /// Sets up the test environment with necessary components
        /// </summary>
        public IntegrationTests()
        {
            // Initialize components with debug mode disabled for tests
            _debugHelper = new DebugHelper(false);
            _boardDetector = new BoardDetector(_debugHelper);
            _colorAnalyzer = new ColorAnalyzer();
            _boardProcessor = new QueensBoardProcessor(_colorAnalyzer, _debugHelper);
        }

        [Fact]
        public void ProcessAllImagesAndSolveQueensProblem_ValidBoards_ReturnsSolutions()
        {
            // Find all test images
            var testImages = FindAllTestImages();
            Assert.NotEmpty(testImages);

            foreach (var imagePath in testImages)
            {
                Console.WriteLine($"Testing image: {Path.GetFileName(imagePath)}");
                
                // Extract expected size from filename (assuming format like queens_8x8.png)
                string filename = Path.GetFileNameWithoutExtension(imagePath);
                int expectedSize = ExtractBoardSizeFromFilename(filename);
                
                // Use Assert.True with a descriptive message for better test output
                Assert.True(expectedSize > 0, 
                    $"Could not determine board size from filename: {filename}. " +
                    "Filenames should include dimensions like 'queens_8x8.png'.");
                
                // Verify the file exists
                Assert.True(File.Exists(imagePath), $"Test image not found at: {imagePath}");
                
                var colorImage = CvInvoke.Imread(imagePath);
                Assert.NotNull(colorImage); // Verify the image was loaded

                try
                {
                    // Act - Step 1: Extract the chessboard from the image
                    var (boardBitmap, detectedRows, detectedColumns) = _boardDetector.ExtractBoardAndAnalyze(colorImage);
                    Assert.NotNull(boardBitmap);
                    
                    // Verify the detected board dimensions match expected size
                    Assert.Equal(expectedSize, detectedRows);
                    Assert.Equal(expectedSize, detectedColumns);

                    // Act - Step 2: Process the board to identify cell colors
                    var colorBoard = _boardProcessor.ProcessBoardImage(boardBitmap, detectedRows);
                    
                    // Verify board extraction worked
                    Assert.NotNull(colorBoard);
                    Assert.Equal(expectedSize, colorBoard.GetLength(0));
                    Assert.Equal(expectedSize, colorBoard.GetLength(1));

                    // Act - Step 3: Solve the Queens Problem based on the color constraints
                    var solver = new QueensSolver();
                    var solution = solver.Solve(colorBoard);

                    // Assert - Verify a valid solution was found
                    Assert.NotNull(solution);
                    Assert.Equal(expectedSize, solution.Length);

                    // Verify no queens threaten each other (rule validation)
                    Helpers.VerifyQueensPlacement(solution);

                    // Verify one queen per color (constraint validation)
                    Helpers.VerifyExactlyOneQueenPerColor(solution, colorBoard);

                    Console.WriteLine($"Successfully solved {Path.GetFileName(imagePath)} ({expectedSize}x{expectedSize})");
                }
                finally
                {
                    colorImage?.Dispose();
                }
            }
        }

        [Fact]
        public void ProcessInvalidImage_ThrowsException()
        {
            // Arrange - Create an empty 1x1 image which isn't a valid chessboard
            using var invalidImage = new Mat(1, 1, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            
            Assert.Throws<Exception>(() => _boardDetector.ExtractBoardAndAnalyze(invalidImage));  
        }

        /// <summary>
        /// Extracts the board size from a filename.
        /// Expected format is like "queens_8x8.png" or similar.
        /// </summary>
        private int ExtractBoardSizeFromFilename(string filename)
        {
            try
            {
                // First look for NxN pattern in filename
                var dimensionMatch = System.Text.RegularExpressions.Regex.Match(
                    filename, @"(\d+)x(\d+)");
                
                if (dimensionMatch.Success && 
                    dimensionMatch.Groups.Count >= 3 && 
                    dimensionMatch.Groups[1].Value == dimensionMatch.Groups[2].Value)
                {
                    return int.Parse(dimensionMatch.Groups[1].Value);
                }
                
                // As a fallback, look for any number that might represent the board size
                var numberMatch = System.Text.RegularExpressions.Regex.Match(
                    filename, @"(\d+)");
                
                if (numberMatch.Success)
                {
                    return int.Parse(numberMatch.Value);
                }
            }
            catch
            {
                // If parsing fails, return 0 to indicate failure
            }
            
            return 0;
        }

        /// <summary>
        /// Finds all image files in the TestImages directory
        /// </summary>
        private List<string> FindAllTestImages()
        {
            var result = new List<string>();
            
            // First, try to find the TestImages directory
            string testImagesPath = FindTestImagesDirectory();
            
            if (testImagesPath == null)
            {
                Console.WriteLine("Warning: TestImages directory not found. No test images will be processed.");
                return result;
            }
            
            // Find all files with supported extensions
            foreach (var extension in SupportedExtensions)
            {
                result.AddRange(Directory.GetFiles(testImagesPath, $"*{extension}", SearchOption.TopDirectoryOnly));
            }
            
            return result;
        }
        
        /// <summary>
        /// Locates the TestImages directory
        /// </summary>
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
