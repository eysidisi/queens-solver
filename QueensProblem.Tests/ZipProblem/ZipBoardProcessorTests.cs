using System;
using System.IO;
using Emgu.CV;
using QueensProblem.Service;
using QueensProblem.Service.ZipProblem;
using QueensProblem.Service.ZipSolver.ImageProcessing;

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

        [Fact]
        public void ProcessImage_WithValidBoard_ReturnsCorrectZipBoard()
        {
            // Arrange
            string testImagePath = Path.Combine(_testImagesPath, "zip_6x6_cropped.png");
            Assert.True(File.Exists(testImagePath), "Test image not found");

            using (var image = CvInvoke.Imread(testImagePath))
            {
                // Act
                var result = _processor.ProcessImage(image, 6);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(6, result.Rows);
                Assert.Equal(6, result.Cols);
            }
        }
    }
}