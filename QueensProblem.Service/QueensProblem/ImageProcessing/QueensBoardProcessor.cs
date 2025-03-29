using System.Drawing;
using System.Linq;
using System.Drawing.Imaging;
using Emgu.CV;
using QueensProblem.Service.QueensProblem.Algorithm;

namespace QueensProblem.Service.QueensProblem.ImageProcessing
{
    /// <summary>
    /// Processes board images to extract cell information
    /// </summary>
    public class QueensBoardProcessor
    {
        private readonly ColorAnalyzer _colorAnalyzer;
        private readonly DebugHelper _debugHelper;
        private int _debugImageCounter = 0;

        public QueensBoardProcessor(ColorAnalyzer colorAnalyzer, DebugHelper debugHelper)
        {
            _colorAnalyzer = colorAnalyzer;
            _debugHelper = debugHelper;
        }

        private void SaveDebugImage(Bitmap image, string suffix)
        {
            if (!_debugHelper.IsDebugMode || image == null) return;

            string name = $"{_debugImageCounter++}_{suffix}";

            // Convert Bitmap to Mat for DebugHelper
            using (Mat mat = image.ToMat())
            {
                _debugHelper.SaveDebugImage(mat, name);
            }
        }

        /// <summary>
        /// Processes the board image to extract the colors of the cells
        /// </summary>
        /// <param name="boardImage">The image of the board</param>
        /// <param name="numberOfCells">The number of cells in the board</param>
        /// <returns>A 2D array of the colors of the cells in the board</returns>
        public string[,] ProcessBoardImage(Bitmap boardImage, int numberOfCells)
        {
            // Use floating point division to get exact cell dimensions
            double exactCellWidth = (double)boardImage.Width / numberOfCells;
            double exactCellHeight = (double)boardImage.Height / numberOfCells;
            string[,] colorBoard = new string[numberOfCells, numberOfCells];

            // Create debug image for cell centers if in debug mode
            Bitmap? debugImage = null;
            Graphics? debugGraphics = null;
            if (_debugHelper.IsDebugMode)
            {
                debugImage = new Bitmap(boardImage);
                debugGraphics = Graphics.FromImage(debugImage);
            }

            try
            {
                for (int y = 0; y < numberOfCells; y++)
                {
                    for (int x = 0; x < numberOfCells; x++)
                    {
                        // Calculate precise boundaries for each cell
                        int startX = (int)(x * exactCellWidth);
                        int startY = (int)(y * exactCellHeight);
                        int endX = (int)((x + 1) * exactCellWidth);
                        int endY = (int)((y + 1) * exactCellHeight);

                        // Calculate the middle pixel coordinates
                        int middleX = startX + (endX - startX) / 2;
                        int middleY = startY + (endY - startY) / 2;

                        // Get the color of the middle pixel
                        Color centerColor = boardImage.GetPixel(middleX, middleY);
                        colorBoard[y, x] = _colorAnalyzer.GetColorLabel(centerColor);

                        // Draw debug markers for cell centers
                        if (_debugHelper.IsDebugMode && debugGraphics != null)
                        {
                            using (Pen pen = new Pen(Color.Yellow, 2))
                            {
                                debugGraphics.DrawRectangle(pen, middleX - 2, middleY - 2, 4, 4);
                            }
                        }
                    }
                }

                if (_debugHelper.IsDebugMode && debugImage != null)
                {
                    SaveDebugImage(debugImage, "cell_centers");
                }

                return colorBoard;
            }
            finally
            {
                debugGraphics?.Dispose();
                debugImage?.Dispose();
            }
        }

        public void PrintColorBoard(string[,] board)
        {
            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    Console.Write(board[i, j] + "\t");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Draws queens on the board image at specified positions
        /// </summary>
        /// <param name="boardImage">The original board image</param>
        /// <param name="queens">List of queen positions, where each position is a tuple (row, column)</param>
        /// <returns>A new bitmap with queens drawn on it</returns>
        public Bitmap DrawQueens(Bitmap boardImage, IEnumerable<Queen> queens)
        {
            // Create a copy of the board image to draw on
            Bitmap resultImage = new Bitmap(boardImage);
            using (Graphics g = Graphics.FromImage(resultImage))
            {
                int numberOfCells = queens.Count(); // Assuming the board size equals number of queens
                double cellWidth = (double)boardImage.Width / numberOfCells;
                double cellHeight = (double)boardImage.Height / numberOfCells;

                // Create queen symbol using ♕ character
                using (Font queenFont = new Font("Arial Unicode MS", (float)(Math.Min(cellWidth, cellHeight) * 0.6)))
                using (Brush queenBrush = new SolidBrush(Color.Red))
                {
                    foreach (var queen in queens)
                    {
                        // Calculate center position for the queen
                        float x = (float)(queen.Col * cellWidth);
                        float y = (float)(queen.Row * cellHeight);

                        // Measure the size of the queen symbol to center it
                        string queenSymbol = "♕";
                        SizeF symbolSize = g.MeasureString(queenSymbol, queenFont);
                        float centerX = x + (float)(cellWidth - symbolSize.Width) / 2;
                        float centerY = y + (float)(cellHeight - symbolSize.Height) / 2;

                        // Draw the queen
                        g.DrawString(queenSymbol, queenFont, queenBrush, centerX, centerY);
                    }
                }

                if (_debugHelper.IsDebugMode)
                {
                    SaveDebugImage(resultImage, "queens_placed");
                }
            }

            return resultImage;
        }
    }
}
