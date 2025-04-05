using Emgu.CV;
using QueensProblem.Service.QueensProblem.Algorithm;
using System.Drawing;
using System.Drawing.Imaging;

namespace QueensProblem.Service.QueensProblem.ImageProcessing
{
    public class QueensImageProcessingService
    {
        private readonly bool debugEnabled;
        
        public QueensImageProcessingService(bool debugEnabled = false)
        {
            this.debugEnabled = debugEnabled;
        }
        
        /// <summary>
        /// Processes the input image to solve the Queens Problem
        /// </summary>
        /// <param name="inputImage">The captured image containing the board</param>
        /// <param name="skipWarping">Whether to skip the perspective warping step (useful for screenshots)</param>
        /// <returns>A tuple containing the result image, queens solution, and board boundaries</returns>
        public (Bitmap resultImage, Queen[] queens, Rectangle boardBounds) ProcessAndSolveQueensProblem(Bitmap inputImage, bool skipWarping = false)
        {
            try
            {
                // Save the input image for reference
                string capturedImagePath = "captured_input.png";
                inputImage.Save(capturedImagePath, ImageFormat.Png);

                // Convert Bitmap to Mat
                Mat colorImage = inputImage.ToMat();
                
                // Initialize components
                var debugHelper = new DebugHelper(debugEnabled);
                var colorAnalyzer = new ColorAnalyzer();
                var boardDetector = new BoardDetector(debugHelper);
                var boardProcessor = new QueensBoardProcessor(colorAnalyzer, debugHelper);
                var queenSolver = new QueensSolver();

                // Extract the board from the image, optionally skipping warping
                var (boardImage, rows, columns) = boardDetector.ExtractBoardAndAnalyze(colorImage);
                
                // Store the actual board boundaries within the original image
                Rectangle boardBounds = boardDetector.GetBoardBoundsInOriginalImage(colorImage, boardImage);

                if (debugEnabled)
                {
                    DrawBoardBoundsForDebugging(inputImage, boardBounds);
                }

                // Process the extracted board into a grid of colors
                string[,] board = boardProcessor.ProcessBoardImage(boardImage, rows);
                
                // Solve the Queens Problem
                var queens = queenSolver.Solve(board);
                
                if (queens == null)
                {
                    throw new Exception("No solution found for this board configuration.");
                }
                
                // Draw the queens on the board image
                Bitmap resultImage = boardProcessor.DrawQueens(boardImage, queens.ToList());
                
                // Save the result image
                string outputPath = "queens_solution.png";
                resultImage.Save(outputPath, ImageFormat.Png);
                
                // Draw board bounds for debugging if enabled
                
                return (resultImage, queens, boardBounds);
            }
            catch (Exception ex)
            {
                // Rethrow the exception to be handled by the caller
                throw new Exception($"Error processing image: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Draws the detected board boundaries on the original image for debugging
        /// </summary>
        /// <param name="image">The original image</param>
        /// <param name="bounds">The detected board boundaries</param>
        private void DrawBoardBoundsForDebugging(Bitmap image, Rectangle bounds)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                using (Pen pen = new Pen(Color.Red, 3))
                {
                    g.DrawRectangle(pen, bounds);
                }
            }
            
            // Save the debug image
            image.Save("debug_board_bounds.png", ImageFormat.Png);
        }
    }
} 