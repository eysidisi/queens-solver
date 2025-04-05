using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using Xunit;
using QueensProblem.Service;
using QueensProblem.Service.ZipProblem;
using QueensProblem.Service.ZipProblem.ImageProcessing;
using QueensProblem.Service.QueensProblem.ImageProcessing;
using QueensProblem.Service.ZipSolver.ImageProcessing;

 namespace ZipProblem.Tests
{
    public class ZipImageProcessingServiceTests : IDisposable
    {
        private readonly DebugHelper _debugHelper;
        private readonly ZipBoardProcessor _zipBoardProcessor;
        private readonly BoardDetector _boardDetector;
        private readonly ZipImageProcessingService _service;
        private readonly string _testImagesPath;

        public ZipImageProcessingServiceTests()
        {
            // Ensure tessdata exists in test environment
            string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            if (!Directory.Exists(tessdataPath))
            {
                Directory.CreateDirectory(tessdataPath);
            }

            // Copy eng.traineddata if needed
            string engDataPath = Path.Combine(tessdataPath, "eng.traineddata");
            if (!File.Exists(engDataPath))
            {
                string resourceEngDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "tessdata", "eng.traineddata");
                if (File.Exists(resourceEngDataPath))
                {
                    File.Copy(resourceEngDataPath, engDataPath);
                }
                else
                {
                    throw new FileNotFoundException(
                        "eng.traineddata not found in Resources. Please download it from https://github.com/tesseract-ocr/tessdata_fast/raw/main/eng.traineddata " +
                        "and place it in the Resources/tessdata folder of your test project.");
                }
            }

            _debugHelper = new DebugHelper(true); // Enable debugging for tests
            _zipBoardProcessor = new ZipBoardProcessor(_debugHelper);
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
            _zipBoardProcessor.Dispose();
        }

        [Theory]
        [InlineData("zip_6x6_1.png")]
        [InlineData("zip_6x6_2.png")]
        [InlineData("zip_6x6_3.png")]
        [InlineData("zip_6x6_4.png")]
        [InlineData("zip_7x7_1.png")]
        public void ProcessAndSolveZipPuzzle_ShouldReturnValidSolution(string imageFilename)
        {
            // Arrange
            string testImagePath = Path.Combine(_testImagesPath, imageFilename);
            Assert.True(File.Exists(testImagePath), $"Test image {imageFilename} not found at {testImagePath}");

            using (var inputImage = new Bitmap(testImagePath))
            {
                // Act
                var (resultImage, solution, board) = _service.ProcessAndSolveZipPuzzle(inputImage);

                // Assert
                Assert.NotNull(resultImage);
                Assert.NotNull(solution);
                Assert.NotNull(board);
                
                // Solution should have at least 2 steps (start and end positions)
                Assert.True(solution.Count >= 2, $"Solution should have at least 2 steps, but has {solution.Count}");

                // Validate the solution path
                ValidateSolutionPath(solution, board);
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