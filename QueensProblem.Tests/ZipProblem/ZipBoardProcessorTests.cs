using System;
using System.IO;
using System.Collections.Generic;
using Emgu.CV;
using QueensProblem.Service;
using QueensProblem.Service.ZipProblem;
using QueensProblem.Service.ZipSolver.ImageProcessing;
using Xunit;

namespace ZipProblem.Tests
{
    public class ZipBoardProcessorTests : IDisposable
    {
        private readonly DebugHelper _debugHelper;
        private readonly ZipBoardProcessor _processor;
        private readonly string _testImagesPath;

        public ZipBoardProcessorTests()
        {
            // Ensure tessdata exists in test environment
            string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            if (!Directory.Exists(tessdataPath))
            {
                Directory.CreateDirectory(tessdataPath);
            }

            // Copy eng.traineddata from resources to test environment
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

            _debugHelper = new DebugHelper(true); // Enable debug mode for tests
            _processor = new ZipBoardProcessor(_debugHelper);

            // Set up test images path
            _testImagesPath = Path.Combine(AppContext.BaseDirectory, "ZipProblem/ZipTestImages");
            if (!Directory.Exists(_testImagesPath))
            {
                Directory.CreateDirectory(_testImagesPath);
            }
        }

        public void Dispose()
        {
            _processor.Dispose();
        }

        // Define wall position class for test data
        public class WallPosition
        {
            public int Row { get; set; }
            public int Col { get; set; }
            public bool IsHorizontal { get; set; }

            public WallPosition(int row, int col, bool isHorizontal)
            {
                Row = row;
                Col = col;
                IsHorizontal = isHorizontal;
            }
        }

        // Define number position class for test data
        public class NumberPosition
        {
            public int Number { get; set; }
            public int Row { get; set; }
            public int Col { get; set; }

            public NumberPosition(int number, int row, int col)
            {
                Number = number;
                Row = row;
                Col = col;
            }
        }

        /// <summary>
        /// Looks up expected numbers and walls for a given file name.
        /// </summary>
        private (List<NumberPosition> expectedNumbers, List<WallPosition> expectedWalls) GetExpectedData(string imageFilename)
        {
            switch (imageFilename)
            {
                case "zip_6x6_1_cropped.png":
                    return (
                        new List<NumberPosition>
                        {
                            new NumberPosition(1, 4, 0),
                            new NumberPosition(2, 1, 2),
                            new NumberPosition(3, 2, 5),
                            new NumberPosition(4, 4, 3),
                            new NumberPosition(5, 1, 5),
                            new NumberPosition(6, 3, 0)
                        },
                        new List<WallPosition>
                        {
                            new WallPosition(4, 0, true),
                            new WallPosition(2, 5, true)
                        }
                    );

                case "zip_6x6_2_cropped.png":
                    return (
                        new List<NumberPosition>
                        {
                            new NumberPosition(1, 3, 4),
                            new NumberPosition(2, 1, 5),
                            new NumberPosition(3, 2, 4),
                            new NumberPosition(4, 4, 3),
                            new NumberPosition(5, 2, 1),
                            new NumberPosition(6, 1, 2),
                            new NumberPosition(7, 4, 0),
                            new NumberPosition(8, 3, 1)
                        },
                        new List<WallPosition>
                        {
                            // Vertical walls
                            new WallPosition(1, 1, false),
                            new WallPosition(2, 1, false),
                            new WallPosition(3, 2, false),
                            new WallPosition(4, 2, false),
                            new WallPosition(1, 4, false),
                            new WallPosition(2, 4, false),
                            new WallPosition(3, 5, false),
                            new WallPosition(4, 5, false),
                            // Horizontal walls
                            new WallPosition(3, 1, true),
                            new WallPosition(3, 4, true)
                        }
                    );

                case "zip_6x6_3_cropped.png":
                    return (
                        new List<NumberPosition>
                        {
                            new NumberPosition(1, 5, 5),
                            new NumberPosition(2, 3, 2),
                        },
                        new List<WallPosition>
                        {
                            // Vertical walls
                            new WallPosition(1, 1, false),
                            new WallPosition(2, 1, false),
                            new WallPosition(3, 1, false),
                            new WallPosition(4, 1, false),
                            new WallPosition(2, 2, false),
                            new WallPosition(3, 2, false),
                            new WallPosition(3, 3, false),
                            new WallPosition(2, 4, false),
                            new WallPosition(3, 4, false),
                            new WallPosition(1, 5, false),
                            new WallPosition(2, 5, false),
                            new WallPosition(3, 5, false),
                            new WallPosition(4, 5, false),
                            // Horizontal walls
                            new WallPosition(1, 1, true),
                            new WallPosition(1, 2, true),
                            new WallPosition(1, 3, true),
                            new WallPosition(1, 4, true),
                            new WallPosition(2, 2, true),
                            new WallPosition(2, 3, true),
                            new WallPosition(4, 2, true),
                            new WallPosition(5, 1, true),
                            new WallPosition(5, 2, true),
                        }
                    );

                case "zip_6x6_4_cropped.png":
                    return (
                        new List<NumberPosition>
                        {
                            new NumberPosition(1, 1, 1),
                            new NumberPosition(2, 2, 2),
                            new NumberPosition(3, 0, 5),
                            new NumberPosition(4, 5, 0),
                            new NumberPosition(5, 4, 1),
                            new NumberPosition(6, 3, 3),
                            new NumberPosition(7, 4, 4),
                            new NumberPosition(8, 1, 4),
                        },
                        new List<WallPosition>
                        {
                            new WallPosition(1, 1, true),
                            new WallPosition(1, 4, true),

                            new WallPosition(5, 1, true),
                            new WallPosition(5, 2, true),
                            new WallPosition(5, 3, true),
                            new WallPosition(5, 4, true),

                            new WallPosition(1, 1, false),
                            new WallPosition(1, 2, false),

                            new WallPosition(1, 4, false),
                            new WallPosition(1, 5, false),

                            new WallPosition(4, 1, false),
                            new WallPosition(4, 5, false),
                        }
                    );

                default:
                    throw new ArgumentException($"No expected test data defined for file: {imageFilename}");
            }
        }

        [Theory]
        [InlineData("zip_6x6_1_cropped.png", 6)]
        [InlineData("zip_6x6_2_cropped.png", 6)]
        [InlineData("zip_6x6_3_cropped.png", 6)]
        [InlineData("zip_6x6_4_cropped.png", 6)]
        public void ProcessImage_DetectsCorrectNumbersAndWalls_Inline(string imageFilename, int gridSize)
        {
            // Arrange
            string testImagePath = Path.Combine(_testImagesPath, imageFilename);
            Assert.True(File.Exists(testImagePath), $"Test image {imageFilename} not found");

            // Get expected results based on file name
            var (expectedNumbers, expectedWalls) = GetExpectedData(imageFilename);

            using (var image = CvInvoke.Imread(testImagePath))
            {
                // Act
                var zipBoard = _processor.ProcessImage(image, gridSize);

                // Assert number positions
                Assert.Equal(expectedNumbers.Count, zipBoard.OrderMap.Count);
                foreach (var expectedNumber in expectedNumbers)
                {
                    Assert.Equal(expectedNumber.Number, zipBoard.GetNode(expectedNumber.Row, expectedNumber.Col).Order);
                }

                // Assert wall positions
                var walls = zipBoard.GetWalls();
                Assert.Equal(expectedWalls.Count, walls.Count);
                foreach (var expectedWall in expectedWalls)
                {
                    Assert.Contains(walls, w =>
                        w.Row == expectedWall.Row &&
                        w.Col == expectedWall.Col &&
                        w.IsHorizontal == expectedWall.IsHorizontal);
                }
            }
        }
    }
}
