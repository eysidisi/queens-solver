using Emgu.CV;
using System.Drawing;

namespace LinkedInPuzzles.Service
{
    /// <summary>
    /// Handles debug-related functionality
    /// </summary>
    public class DebugHelper
    {
        private readonly bool _debugEnabled;

        public bool IsDebugMode => _debugEnabled;

        public DebugHelper(bool debugEnabled = true)
        {
            _debugEnabled = debugEnabled;
        }

        public void SaveDebugImage(Mat image, string name)
        {
            if (!_debugEnabled) return;

            string path = "debug_" + name + ".png";
            CvInvoke.Imwrite(path, image);
            Console.WriteLine($"Debug image saved: {path}");
        }

        public void DrawCellLabels(Bitmap boardImage, string[,] board, int numberOfCells)
        {
            if (!_debugEnabled) return;

            int cellWidth = boardImage.Width / numberOfCells;
            int cellHeight = boardImage.Height / numberOfCells;
            Bitmap debugBitmap = new Bitmap(boardImage);
            using (Graphics g = Graphics.FromImage(debugBitmap))
            {
                // Draw grid lines for clarity.
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    for (int i = 0; i <= numberOfCells; i++)
                    {
                        int x = i * cellWidth;
                        g.DrawLine(pen, x, 0, x, boardImage.Height);
                    }
                    for (int i = 0; i <= numberOfCells; i++)
                    {
                        int y = i * cellHeight;
                        g.DrawLine(pen, 0, y, boardImage.Width, y);
                    }
                }

                // Draw the color number in the center of each cell.
                using (Font font = new Font("Arial", 16, FontStyle.Bold))
                using (Brush brush = Brushes.Blue)
                {
                    for (int y = 0; y < numberOfCells; y++)
                    {
                        for (int x = 0; x < numberOfCells; x++)
                        {
                            string label = board[y, x];
                            // Extract the number from "color1", "color2", etc.
                            string labelNumber = label.Replace("color", "");
                            int centerX = x * cellWidth + cellWidth / 2;
                            int centerY = y * cellHeight + cellHeight / 2;
                            SizeF textSize = g.MeasureString(labelNumber, font);
                            g.DrawString(labelNumber, font, brush, centerX - textSize.Width / 2, centerY - textSize.Height / 2);
                        }
                    }
                }
            }
            string debugFileName = "debug_cells.png";
            debugBitmap.Save(debugFileName);
            Console.WriteLine($"Debug cell label image saved: {debugFileName}");
        }

        public void LogDebugMessage(string message)
        {
            if (IsDebugMode)
            {
                Console.WriteLine($"[DEBUG] {DateTime.Now}: {message}");
                // You could also log to a file if needed
            }
        }

        public void SaveDebugImage(Bitmap bitmap, string name)
        {
            if (!_debugEnabled) return;

            using (var mat = bitmap.ToMat())
            {
                SaveDebugImage(mat, name);
            }
        }
    }
}
