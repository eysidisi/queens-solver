using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using QueensProblem.Service.ZipProblem;
using QueensProblem.Service.ZipProblem.ImageProcessing;
using QueensProblem.Service.QueensProblem.ImageProcessing;

namespace QueensProblem.Service.ZipSolver.ImageProcessing
{
    /// <summary>
    /// Service class for processing images of Zip puzzles with detailed debugging
    /// </summary>
    public class ZipImageProcessingService
    {
        private readonly DebugHelper _debugHelper;
        private readonly string _debugOutputPath;
        private readonly ZipBoardProcessor _zipBoardProcessor;
        private readonly BoardDetector _boardDetector;
        
        public ZipImageProcessingService(
            DebugHelper debugHelper, 
            ZipBoardProcessor zipBoardProcessor, 
            BoardDetector boardDetector)
        {
            _debugHelper = debugHelper ?? throw new ArgumentNullException(nameof(debugHelper));
            _zipBoardProcessor = zipBoardProcessor ?? throw new ArgumentNullException(nameof(zipBoardProcessor));
            _boardDetector = boardDetector ?? throw new ArgumentNullException(nameof(boardDetector));
            
            _debugOutputPath = AppDomain.CurrentDomain.BaseDirectory;
            _debugHelper.LogDebugMessage("ZipImageProcessingService initialized");
            _debugHelper.LogDebugMessage($"Debug output path: {_debugOutputPath}");
        }
        
        /// <summary>
        /// Processes the input image to solve the Zip Problem
        /// </summary>
        /// <param name="inputImage">The captured image containing the Zip board</param>
        /// <returns>A tuple containing the result image, solution path, and board</returns>
        public (Bitmap resultImage, List<ZipNode> solution, ZipBoard board) ProcessAndSolveZipPuzzle(Bitmap inputImage)
        {
            
            // Save the input image for reference
            string capturedImagePath = Path.Combine(_debugOutputPath, "zip_input.png");

            // Validate input image
            if (inputImage == null || inputImage.Width < 100 || inputImage.Height < 100)
            {
                throw new ArgumentException("Input image is too small or invalid");
            }

            // Convert Bitmap to Mat
            Mat colorImage = inputImage.ToMat();
            _debugHelper.SaveDebugImage(colorImage, "input_color_mat");
            
            // Detect the board dimensions
            var boardInfo = _boardDetector.ExtractCurvedBoardAndAnalyze(colorImage);
            
            // Process the board image to extract numbers and connectivity
            ZipBoard zipBoard = _zipBoardProcessor.ProcessImage(boardInfo.WarpedBoard, boardInfo.Rows);
            
            // Create a solver and solve the puzzle
            var zipSolver = new ZipProblem.ZipSolver(zipBoard);
            List<ZipNode> solution = zipSolver.Solve();
            
            if (solution == null || solution.Count == 0)
            {
                _debugHelper.LogDebugMessage("No solution found for this Zip puzzle configuration");
                throw new InvalidOperationException("No solution found for this Zip puzzle configuration.");
            }
            
            _debugHelper.LogDebugMessage($"Solution found with {solution.Count} steps");
            
            // Debug output for solution path
            _debugHelper.LogDebugMessage("Solution path:");
            for (int i = 0; i < solution.Count; i++)
            {
                var node = solution[i];
                _debugHelper.LogDebugMessage($"Step {i+1}: ({node.Row}, {node.Col})");
            }
            
            // Visualize the solution on the image
            _debugHelper.LogDebugMessage("Visualizing solution");
            Bitmap resultImage = DrawSolution(boardInfo.WarpedBoard, zipBoard, solution);
            
            // Save the result image
            string outputPath = Path.Combine(_debugOutputPath, "zip_solution.png");
            resultImage.Save(outputPath, ImageFormat.Png);
            _debugHelper.LogDebugMessage($"Solution image saved to: {outputPath}");
            
            return (resultImage, solution, zipBoard);
        }
        
        /// <summary>
        /// Draws the solution path on the board image
        /// </summary>
        /// <param name="originalImage">The original image</param>
        /// <param name="board">The processed board</param>
        /// <param name="solution">The solution path</param>
        /// <returns>A new image with the solution drawn on it</returns>
        private Bitmap DrawSolution(Bitmap originalImage, ZipBoard board, List<ZipNode> solution)
        {
            _debugHelper.LogDebugMessage("Drawing solution path");
            
            // Create a copy of the original image to draw on
            Bitmap resultImage = new Bitmap(originalImage);
            
            int cellWidth = originalImage.Width / board.Cols;
            int cellHeight = originalImage.Height / board.Rows;
            
            _debugHelper.LogDebugMessage($"Cell dimensions: {cellWidth}x{cellHeight} pixels");
            
            using (Graphics g = Graphics.FromImage(resultImage))
            {
                // Draw solution path
                using (Pen pathPen = new Pen(Color.Green, 3))
                {
                    _debugHelper.LogDebugMessage($"Drawing {solution.Count-1} path segments");
                    
                    // Draw solution path connecting center points of cells
                    for (int i = 0; i < solution.Count - 1; i++)
                    {
                        ZipNode current = solution[i];
                        ZipNode next = solution[i + 1];
                        
                        // Calculate center points
                        Point currentCenter = new Point(
                            current.Col * cellWidth + cellWidth / 2,
                            current.Row * cellHeight + cellHeight / 2
                        );
                        
                        Point nextCenter = new Point(
                            next.Col * cellWidth + cellWidth / 2,
                            next.Row * cellHeight + cellHeight / 2
                        );
                        
                        // Draw line between centers
                        g.DrawLine(pathPen, currentCenter, nextCenter);
                        
                        _debugHelper.LogDebugMessage($"Drew line from ({current.Row},{current.Col}) to ({next.Row},{next.Col})");
                    }
                }
                
                // Highlight start and end points
                if (solution.Count > 0)
                {
                    _debugHelper.LogDebugMessage("Highlighting start and end points");
                    
                    using (Brush startBrush = new SolidBrush(Color.Blue))
                    {
                        ZipNode startNode = solution[0];
                        g.FillEllipse(
                            startBrush,
                            startNode.Col * cellWidth + cellWidth / 3,
                            startNode.Row * cellHeight + cellHeight / 3,
                            cellWidth / 3,
                            cellHeight / 3
                        );
                        _debugHelper.LogDebugMessage($"Start point marked at ({startNode.Row},{startNode.Col})");
                    }
                    
                    using (Brush endBrush = new SolidBrush(Color.Red))
                    {
                        ZipNode endNode = solution[solution.Count - 1];
                        g.FillEllipse(
                            endBrush,
                            endNode.Col * cellWidth + cellWidth / 3,
                            endNode.Row * cellHeight + cellHeight / 3,
                            cellWidth / 3,
                            cellHeight / 3
                        );
                        _debugHelper.LogDebugMessage($"End point marked at ({endNode.Row},{endNode.Col})");
                    }
                }
                
                // Number the steps in the solution
                _debugHelper.LogDebugMessage("Adding step numbers to solution path");
                using (Font font = new Font("Arial", Math.Min(cellWidth, cellHeight) / 4))
                using (Brush textBrush = new SolidBrush(Color.Blue))
                {
                    for (int i = 0; i < solution.Count; i++)
                    {
                        ZipNode node = solution[i];
                        g.DrawString(
                            (i + 1).ToString(),
                            font,
                            textBrush,
                            node.Col * cellWidth + cellWidth / 2,
                            node.Row * cellHeight + cellHeight / 2
                        );
                    }
                }
            }
            
            // Save intermediate visualization for debugging
            string debugImagePath = Path.Combine(_debugOutputPath, "solution_visualization.png");
            resultImage.Save(debugImagePath, ImageFormat.Png);
            _debugHelper.LogDebugMessage($"Solution visualization saved to: {debugImagePath}");
            
            return resultImage;
        }
    }
} 