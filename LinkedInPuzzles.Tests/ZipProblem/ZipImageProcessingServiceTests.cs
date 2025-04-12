using LinkedInPuzzles.Service;
using LinkedInPuzzles.Service.QueensProblem.ImageProcessing;
using LinkedInPuzzles.Service.ZipProblem;
using LinkedInPuzzles.Service.ZipProblem.ImageProcessing;
using LinkedInPuzzles.Service.ZipSolver.ImageProcessing;
using System.Drawing;

namespace LinkedInPuzzles.Tests.ZipProblem
{
    public class ZipImageProcessingServiceTests : IDisposable
    {
        private readonly DebugHelper _debugHelper;
        private readonly ZipBoardProcessor _zipBoardProcessor;
        private readonly BoardDetector _boardDetector;
        private readonly ZipImageProcessingService _service;
        private readonly string _testImagesPath;
        private readonly DigitRecognizer _digitRecognizer;
        private readonly CircleDetector _circleDetector;
        private readonly ConnectivityDetector _connectivityDetector;

        public ZipImageProcessingServiceTests()
        {
            // Look for tessdata in various locations
            string[] possibleTessdataPaths = new[]
            {
                // Project structure path
                Path.Combine(
                    Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName,
                    "LinkedInPuzzles.Service", "ZipProblem", "Resources", "tessdata"),
                // Output directory path
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
                // Resources subdirectory
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "tessdata")
            };

            string tessdataPath = null;
            foreach (var path in possibleTessdataPaths)
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "eng.traineddata")))
                {
                    tessdataPath = path;
                    break;
                }
            }

            // If not found, use the default location and try to create/copy
            if (tessdataPath == null)
            {
                tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                if (!Directory.Exists(tessdataPath))
                {
                    Directory.CreateDirectory(tessdataPath);
                }

                // Try to find and copy eng.traineddata from project structure
                string projectTessdataPath = Path.Combine(
                    Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName,
                    "LinkedInPuzzles.Service", "ZipProblem", "Resources", "tessdata", "eng.traineddata");

                string engDataPath = Path.Combine(tessdataPath, "eng.traineddata");
                if (File.Exists(projectTessdataPath) && !File.Exists(engDataPath))
                {
                    File.Copy(projectTessdataPath, engDataPath);
                    Console.WriteLine($"Copied eng.traineddata from {projectTessdataPath} to {engDataPath}");
                }
                else if (!File.Exists(engDataPath))
                {
                    throw new FileNotFoundException(
                        "eng.traineddata not found. Please ensure it exists at: " +
                        projectTessdataPath);
                }
            }

            Console.WriteLine($"Using tessdata path: {tessdataPath}");

            _debugHelper = new DebugHelper(true); // Enable debugging for tests

            // Initialize required components
            _digitRecognizer = new DigitRecognizer(_debugHelper, tessdataPath);
            _circleDetector = new CircleDetector(_debugHelper);
            _connectivityDetector = new ConnectivityDetector(_debugHelper);

            // Initialize ZipBoardProcessor with all required parameters
            _zipBoardProcessor = new ZipBoardProcessor(
                _debugHelper,
                _digitRecognizer,
                _circleDetector,
                _connectivityDetector);

            _boardDetector = new BoardDetector(_debugHelper);
            _service = new ZipImageProcessingService(_debugHelper, _zipBoardProcessor, _boardDetector);

            // Set up test images path
            _testImagesPath = Path.Combine(AppContext.BaseDirectory, "ZipProblem", "ZipTestImages");

            // Verify the test images directory exists
            if (!Directory.Exists(_testImagesPath))
            {
                throw new DirectoryNotFoundException($"Test images directory not found at {_testImagesPath}");
            }
        }

        public void Dispose()
        {
            // Clean up resources
            _digitRecognizer?.Dispose();
        }

        [Theory]
        [InlineData("zip_6x6_1.png")]
        [InlineData("zip_6x6_2.png")]
        [InlineData("zip_6x6_3.png")]
        [InlineData("zip_6x6_4.png")]
        [InlineData("zip_7x7_1.png")]
        [InlineData("zip_7x7_2.png")]
        public void ProcessAndSolveZipPuzzle(string imageFilename)
        {
            // Arrange
            string testImagePath = Path.Combine(_testImagesPath, imageFilename);
            Assert.True(File.Exists(testImagePath), $"Test image {imageFilename} not found at {testImagePath}");

            using (var inputImage = new Bitmap(testImagePath))
            {
                // Act
                var (resultImage, solution, board, _) = _service.ProcessAndSolveZipPuzzle(inputImage);

                // Assert
                Assert.NotNull(resultImage);
                Assert.NotNull(solution);
                Assert.NotNull(board);

                // Solution should have at least 2 steps (start and end positions)
                Assert.True(solution.Count >= 2, $"Solution should have at least 2 steps, but has {solution.Count}");

                // Validate the solution path
                ValidateSolutionPath(solution, board);

                // Save the result image for visual inspection
                string resultDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");
                Directory.CreateDirectory(resultDir);
                string resultPath = Path.Combine(resultDir, $"result_{imageFilename}");
                resultImage.Save(resultPath);

                _debugHelper.LogDebugMessage($"Result image saved to: {resultPath}");

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = resultPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    _debugHelper.LogDebugMessage($"Unable to display result image: {ex.Message}");
                }
                }
        }

        private void ValidateSolutionPath(List<ZipNode> solution, ZipBoard board)
        {
            // Validate that consecutive nodes in the solution are connected
            for (int i = 0; i < solution.Count - 1; i++)
            {
                ZipNode current = solution[i];
                ZipNode next = solution[i + 1];

                // Verify that current and next are neighboring cells
                Assert.Contains(next, current.Neighbors);
            }

            // Verify that the solution covers all numbered cells in ascending order
            var numberedCells = new List<ZipNode>();
            foreach (var kvp in board.OrderMap)
            {
                numberedCells.Add(kvp.Value);
            }

            // Sort by order number
            numberedCells.Sort((a, b) => a.Order.CompareTo(b.Order));

            // Solution must include all numbered cells
            foreach (var cell in numberedCells)
            {
                Assert.Contains(cell, solution);
            }

            // Check that numbered cells appear in the correct order in the solution
            for (int i = 0; i < numberedCells.Count - 1; i++)
            {
                int firstIndex = solution.IndexOf(numberedCells[i]);
                int secondIndex = solution.IndexOf(numberedCells[i + 1]);

                Assert.True(firstIndex < secondIndex,
                    $"Cell with order {numberedCells[i].Order} should appear before cell with order {numberedCells[i + 1].Order} in the solution");
            }
        }
    }
}