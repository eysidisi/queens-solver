using Emgu.CV;
using LinkedInPuzzles.Service.QueensProblem.ImageProcessing;
using LinkedInPuzzles.Service.ZipProblem;
using LinkedInPuzzles.Service.ZipProblem.ImageProcessing;
using System.Drawing;
using System.Drawing.Imaging;

namespace LinkedInPuzzles.Service.ZipSolver.ImageProcessing
{
    /// <summary>
    /// Service class for processing images of Zip puzzles with detailed debugging
    /// </summary>
    public class ZipImageProcessingService
    {
        private readonly DebugHelper _debugHelper;
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
        }

        /// <summary>
        /// Processes the input image to solve the Zip Problem
        /// </summary>
        /// <param name="inputImage">The captured image containing the Zip board</param>
        /// <returns>A tuple containing the result image, solution path, board, and detected board bounds</returns>
        public (Bitmap resultImage, List<ZipNode> solution, ZipBoard board, Rectangle boardBounds) ProcessAndSolveZipPuzzle(Bitmap inputImage)
        {

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

            // Get the board bounds in the original image
            Rectangle boardBounds = _boardDetector.GetBoardBoundsInOriginalImage(colorImage, boardInfo.WarpedBoard);

            // Process the board image to extract numbers and connectivity
            ZipBoard zipBoard = _zipBoardProcessor.ProcessImage(boardInfo.WarpedBoard, boardInfo.Rows);

            // Create a solver and solve the puzzle
            var zipSolver = new ZipProblem.ZipSolver(zipBoard);
            List<ZipNode> solution = zipSolver.Solve();

            if (solution == null || solution.Count == 0)
            {
                throw new InvalidOperationException("No solution found for this Zip puzzle configuration.");
            }


            // Visualize the solution on the image
            Bitmap resultImage = DrawSolution(boardInfo.WarpedBoard, zipBoard, solution);

            return (resultImage, solution, zipBoard, boardBounds);
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

            // Create a copy of the original image to draw on
            Bitmap resultImage = new Bitmap(originalImage);

            int cellWidth = originalImage.Width / board.Cols;
            int cellHeight = originalImage.Height / board.Rows;

            using (Graphics g = Graphics.FromImage(resultImage))
            {
                // Set up for smoother drawing
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw solution path with arrows
                using (Pen pathPen = new Pen(Color.Green, 3))
                {
                    // Configure arrow caps for the pen
                    pathPen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(5, 5, true);

                    // Draw solution path connecting center points of cells with arrows
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

                        // Draw arrow from current to next
                        g.DrawLine(pathPen, currentCenter, nextCenter);

                    }
                }

                // Highlight start and end points
                if (solution.Count > 0)
                {
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

                        // Add "START" label
                        using (Font labelFont = new Font("Arial", Math.Min(cellWidth, cellHeight) / 6, FontStyle.Bold))
                        using (Brush labelBrush = new SolidBrush(Color.White))
                        {
                            g.DrawString(
                                "START",
                                labelFont,
                                labelBrush,
                                startNode.Col * cellWidth + cellWidth / 4,
                                startNode.Row * cellHeight + cellHeight / 4
                            );
                        }

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

                        // Add "END" label
                        using (Font labelFont = new Font("Arial", Math.Min(cellWidth, cellHeight) / 6, FontStyle.Bold))
                        using (Brush labelBrush = new SolidBrush(Color.White))
                        {
                            g.DrawString(
                                "END",
                                labelFont,
                                labelBrush,
                                endNode.Col * cellWidth + cellWidth / 3,
                                endNode.Row * cellHeight + cellHeight / 3
                            );
                        }

                    }
                }
            }

            // Save intermediate visualization for debugging
            string debugImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "solution_visualization.png");
            resultImage.Save(debugImagePath, ImageFormat.Png);

            return resultImage;
        }
    }
}