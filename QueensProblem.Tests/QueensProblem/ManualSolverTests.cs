using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Xunit;
using QueensProblem.Service.QueensProblem.Algorithm;
using QueensProblem.Service.QueensProblem.ImageProcessing;

namespace QueensProblem.Tests.Queens
{
    public class ManualSolverTests
    {
        // Output directory for solved images
        private const string OutputDirectory = "TestOutput";
        private const string TestImagesDirectory = "TestImages";
        
        // Supported image extensions
        private static readonly string[] SupportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };
        
        // Dependencies
        private readonly DebugHelper _debugHelper;
        private readonly ColorAnalyzer _colorAnalyzer;
        private readonly BoardDetector _boardDetector;
        private readonly QueensBoardProcessor _boardProcessor;
        private readonly QueensSolver _queensSolver;
        
        public ManualSolverTests()
        {
            // Initialize with debug mode enabled to generate debug images
            _debugHelper = new DebugHelper(true);
            _colorAnalyzer = new ColorAnalyzer();
            _boardDetector = new BoardDetector(_debugHelper);
            _boardProcessor = new QueensBoardProcessor(_colorAnalyzer, _debugHelper);
            _queensSolver = new QueensSolver();
            
            // Ensure output directory exists
            Directory.CreateDirectory(OutputDirectory);
        }
        
        [Fact]
        public void Solve_All_Test_Images()
        {
            // Find all test images
            var testImages = FindAllTestImages();
            
            Console.WriteLine($"Found {testImages.Count} test images to process.");
            
            // Process each test image
            foreach (var imagePath in testImages)
            {
                Console.WriteLine($"\n=== Processing {Path.GetFileName(imagePath)} ===\n");
                
                try
                {
                    string outputFileName = $"{Path.GetFileNameWithoutExtension(imagePath)}_solution{Path.GetExtension(imagePath)}";
                    SolveAndDisplayBoard(imagePath, outputFileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {Path.GetFileName(imagePath)}: {ex.Message}");
                }
            }
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
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TestImagesDirectory);
            
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
        
        /// <summary>
        /// Helper method to solve a board image and display/save the solution
        /// </summary>
        private void SolveAndDisplayBoard(string imagePath, string outputFileName)
        {            
            Assert.True(File.Exists(imagePath), $"Test image not found: {imagePath}");
            
            Console.WriteLine($"Processing image: {imagePath}");
            
            // Load and process the image
            using var colorImage = CvInvoke.Imread(imagePath, ImreadModes.Color);
            var (boardImage, rows, columns) = _boardDetector.ExtractBoardAndAnalyze(colorImage);
            
            Console.WriteLine($"Detected board dimensions: {rows}x{columns}");
            
            // Process the board to get the color board
            string[,] colorBoard = _boardProcessor.ProcessBoardImage(boardImage, rows);
            
            // Print the detected color board
            Console.WriteLine("Detected color board:");
            _boardProcessor.PrintColorBoard(colorBoard);
            
            // Solve the queens problem
            Console.WriteLine("Solving Queens Problem...");
            Queen[] queens = _queensSolver.Solve(colorBoard);
            
            // Check if a solution was found
            if (queens == null)
            {
                Assert.Fail("Failed to find a solution for the Queens Problem");
                return; // Never reached, but helps the compiler understand the flow
            }
            
            // Display the positions of the queens
            Console.WriteLine("Solution found! Queen positions:");
            for (int i = 0; i < queens.Length; i++)
            {
                Console.WriteLine($"Queen {i+1}: Row {queens[i].Row}, Column {queens[i].Col}");
            }
            
            // Draw the queens on the board
            Bitmap solutionImage = _boardProcessor.DrawQueens(boardImage, queens);
            
            // Save the solution image
            string outputPath = Path.Combine(OutputDirectory, outputFileName);
            string absoluteOutputPath = Path.GetFullPath(outputPath);
            solutionImage.Save(absoluteOutputPath);
            Console.WriteLine($"Solution image saved to: {absoluteOutputPath}");
            
            // Open the solution image in the default image viewer
            OpenImageInDefaultViewer(absoluteOutputPath);
            
            // Clean up
            solutionImage.Dispose();
            boardImage.Dispose();
        }
        
        /// <summary>
        /// Opens an image file using the system's default image viewer
        /// </summary>
        private void OpenImageInDefaultViewer(string imagePath)
        {
            try
            {
                // Create a new process info
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = imagePath,
                    UseShellExecute = true
                };
                
                // Start the process
                Console.WriteLine($"Opening image: {imagePath}");
                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not open the image automatically: {ex.Message}");
                Console.WriteLine($"Please open the file manually: {imagePath}");
            }
        }
    }
} 