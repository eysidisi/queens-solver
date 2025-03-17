using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace QueensProblem.Service.ImageProcessing
{
    /// <summary>
    /// Handles board detection and extraction from images
    /// </summary>
    public class BoardDetector
    {
        private readonly DebugHelper _debugHelper;

        public BoardDetector(DebugHelper debugHelper)
        {
            _debugHelper = debugHelper;
        }

        public (Bitmap WarpedBoard, int Rows, int Columns) ExtractBoardAndAnalyze(Mat colorImage)
        {
            // Add more robust error handling for screen captures
            try
            {
                // Load the image.
                _debugHelper.SaveDebugImage(colorImage, "original");

                // Convert to grayscale.
                Mat gray = new Mat();
                CvInvoke.CvtColor(colorImage, gray, ColorConversion.Bgr2Gray);
                _debugHelper.SaveDebugImage(gray, "gray");

                // Blur to reduce noise.
                Mat blurred = new Mat();
                CvInvoke.GaussianBlur(gray, blurred, new Size(5, 5), 0);
                _debugHelper.SaveDebugImage(blurred, "blurred");

                // Adaptive threshold to create a binary image.
                Mat thresh = new Mat();
                CvInvoke.AdaptiveThreshold(blurred, thresh, 255, AdaptiveThresholdType.MeanC, ThresholdType.BinaryInv, 11, 2);
                _debugHelper.SaveDebugImage(thresh, "thresh");

                // Find contours.
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(thresh, contours, null!, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                double maxArea = 0;
                Point[]? boardContour = null;
                // Loop through contours to find the largest quadrilateral.
                for (int i = 0; i < contours.Size; i++)
                {
                    VectorOfPoint contour = contours[i];
                    double area = CvInvoke.ContourArea(contour);
                    if (area > 1000) // filter out small areas.
                    {
                        double peri = CvInvoke.ArcLength(contour, true);
                        VectorOfPoint approx = new VectorOfPoint();
                        CvInvoke.ApproxPolyDP(contour, approx, 0.02 * peri, true);
                        if (approx.Size == 4 && area > maxArea)
                        {
                            maxArea = area;
                            boardContour = approx.ToArray();
                        }
                    }
                }

                if (boardContour == null)
                    throw new Exception("Board contour not found.");

                // Draw the detected board contour for debugging.
                Mat contourDebug = colorImage.Clone();
                VectorOfPoint boardVector = new VectorOfPoint(boardContour!);
                CvInvoke.Polylines(contourDebug, boardVector, true, new MCvScalar(0, 0, 255), 2);
                _debugHelper.SaveDebugImage(contourDebug, "boardContour");

                Mat boardMat;
                int boardSize = 450; // Standard size for the board

                // Original warping approach
                // Order the contour points (top-left, top-right, bottom-right, bottom-left).
                Point[] orderedPoints = OrderPoints(boardContour);

                // Define destination points for a square board.
                PointF[] destPoints = new PointF[]
                {
                        new PointF(0, 0),
                        new PointF(boardSize - 1, 0),
                        new PointF(boardSize - 1, boardSize - 1),
                        new PointF(0, boardSize - 1)
                };

                // Compute perspective transform and warp the image.
                Mat transform = CvInvoke.GetPerspectiveTransform(Array.ConvertAll(orderedPoints, p => new PointF(p.X, p.Y)), destPoints);
                boardMat = new Mat();
                CvInvoke.WarpPerspective(colorImage, boardMat, transform, new Size(boardSize, boardSize));
                _debugHelper.SaveDebugImage(boardMat, "warped");

                // Detect grid dimensions
                var (rows, columns) = DetectGridDimensions(boardMat);

                return (boardMat.ToBitmap(), rows, columns);
            }
            catch (Exception ex)
            {
                _debugHelper.LogDebugMessage($"Board extraction failed: {ex.Message}");
                throw new Exception("Failed to extract board from image", ex);
            }
        }

        private Point[] OrderPoints(Point[] pts)
        {
            Point[] rect = new Point[4];
            // Sum of coordinates: smallest -> top-left, largest -> bottom-right.
            var sorted = pts.OrderBy(p => p.X + p.Y).ToArray();
            rect[0] = sorted[0];
            rect[2] = sorted[sorted.Length - 1];

            // Difference of coordinates: smallest -> top-right, largest -> bottom-left.
            var sortedDiff = pts.OrderBy(p => p.Y - p.X).ToArray();
            rect[1] = sortedDiff[0];
            rect[3] = sortedDiff[sortedDiff.Length - 1];

            return rect;
        }

        public (int Rows, int Columns) DetectGridDimensions(Mat warpedBoard)
        {
            // Convert to grayscale if not already
            Mat gray = new Mat();
            if (warpedBoard.NumberOfChannels > 1)
            {
                CvInvoke.CvtColor(warpedBoard, gray, ColorConversion.Bgr2Gray);
            }
            else
            {
                gray = warpedBoard.Clone();
            }

            // Apply adaptive threshold
            Mat binary = new Mat();
            CvInvoke.AdaptiveThreshold(gray, binary, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 11, 2);
            _debugHelper.SaveDebugImage(binary, "binary_grid");

            // Detect edges
            Mat edges = new Mat();
            CvInvoke.Canny(binary, edges, 50, 150);
            _debugHelper.SaveDebugImage(edges, "edges_grid");

            // Detect lines using Hough Transform
            LineSegment2D[] lines = CvInvoke.HoughLinesP(
                edges,
                1, // rho
                Math.PI / 180, // theta
                50, // threshold
                30, // minLineLength
                10 // maxLineGap
            );

            // Separate horizontal and vertical lines
            var horizontalLines = new List<int>();
            var verticalLines = new List<int>();

            foreach (var line in lines)
            {
                // Calculate line angle
                double angle = Math.Abs(Math.Atan2(line.P2.Y - line.P1.Y, line.P2.X - line.P1.X) * 180 / Math.PI);

                // Horizontal lines (angle close to 0 or 180 degrees)
                if (angle < 20 || angle > 160)
                {
                    int y = (line.P1.Y + line.P2.Y) / 2;
                    horizontalLines.Add(y);
                }
                // Vertical lines (angle close to 90 degrees)
                else if (angle > 70 && angle < 110)
                {
                    int x = (line.P1.X + line.P2.X) / 2;
                    verticalLines.Add(x);
                }
            }

            // Draw detected lines for debugging
            Mat lineDebug = warpedBoard.Clone();
            foreach (var line in lines)
            {
                CvInvoke.Line(lineDebug, line.P1, line.P2, new MCvScalar(0, 0, 255), 2);
            }
            _debugHelper.SaveDebugImage(lineDebug, "detected_grid_lines");

            // Group nearby lines (they might be detected multiple times)
            horizontalLines = GroupNearbyPositions(horizontalLines, 20).OrderBy(x => x).ToList();
            verticalLines = GroupNearbyPositions(verticalLines, 20).OrderBy(x => x).ToList();

            // Calculate the number of internal cells by excluding border lines
            int rows, columns;

            if (horizontalLines.Count >= 2)
            {
                // Subtract 2 to exclude the outer border lines
                rows = horizontalLines.Count - 1;
            }
            else
            {
                rows = horizontalLines.Count;
            }

            if (verticalLines.Count >= 2)
            {
                // Subtract 2 to exclude the outer border lines
                columns = verticalLines.Count - 1;
            }
            else
            {
                columns = verticalLines.Count;
            }

            // Create debug image showing internal cells
            if (rows > 0 && columns > 0)
            {
                Mat cellDebug = warpedBoard.Clone();
                // Draw cells with alternating colors for debugging
                int imageSize = warpedBoard.Width;
                int cellHeight = imageSize / rows;
                int cellWidth = imageSize / columns;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        Rectangle rect = new Rectangle(
                            c * cellWidth,
                            r * cellHeight,
                            cellWidth,
                            cellHeight);

                        MCvScalar color = ((r + c) % 2 == 0) ?
                            new MCvScalar(0, 255, 0, 128) : // Green for even cells
                            new MCvScalar(0, 0, 255, 128);  // Red for odd cells

                        CvInvoke.Rectangle(cellDebug, rect, color, -1);
                    }
                }
                CvInvoke.AddWeighted(cellDebug, 0.3, warpedBoard, 0.7, 0, cellDebug);
                _debugHelper.SaveDebugImage(cellDebug, "detected_cells");
            }

            return (rows, columns);
        }

        public Rectangle GetBoardBoundsInOriginalImage(Mat originalImage, Bitmap boardImage)
        {
            // We will rerun part of the board detection process to find the actual board boundaries
            // Convert to grayscale
            Mat gray = new Mat();
            CvInvoke.CvtColor(originalImage, gray, ColorConversion.Bgr2Gray);

            // Apply blur to reduce noise
            Mat blurred = new Mat();
            CvInvoke.GaussianBlur(gray, blurred, new Size(5, 5), 0);

            // Apply adaptive threshold to create binary image
            Mat thresh = new Mat();
            CvInvoke.AdaptiveThreshold(blurred, thresh, 255, AdaptiveThresholdType.MeanC, ThresholdType.BinaryInv, 11, 2);

            // Find contours in the binary image
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(thresh, contours, null!, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            // Look for the largest quadrilateral contour (likely the board)
            double maxArea = 0;
            Point[]? boardContour = null;

            for (int i = 0; i < contours.Size; i++)
            {
                VectorOfPoint contour = contours[i];
                double area = CvInvoke.ContourArea(contour);

                if (area > 1000) // Filter out small areas
                {
                    double peri = CvInvoke.ArcLength(contour, true);
                    VectorOfPoint approx = new VectorOfPoint();
                    CvInvoke.ApproxPolyDP(contour, approx, 0.02 * peri, true);

                    // The board should be a quadrilateral (4 corners)
                    if (approx.Size == 4 && area > maxArea)
                    {
                        maxArea = area;
                        boardContour = approx.ToArray();
                    }
                }
            }

            if (boardContour == null)
            {
                // If we couldn't find the board contour, fall back to a default approach
                int boardSize = Math.Min(originalImage.Width, originalImage.Height) * 3 / 4;
                int offsetX = (originalImage.Width - boardSize) / 2;
                int offsetY = (originalImage.Height - boardSize) / 2;

                return new Rectangle(offsetX, offsetY, boardSize, boardSize);
            }

            // Get the bounding rectangle of the detected board contour
            Rectangle bounds = CvInvoke.BoundingRectangle(new VectorOfPoint(boardContour));

            // Draw the board contour for debugging
            if (_debugHelper.IsDebugMode)
            {
                Mat debugImage = originalImage.Clone();
                VectorOfPoint contourVector = new VectorOfPoint(boardContour);
                CvInvoke.DrawContours(debugImage, new VectorOfVectorOfPoint(contourVector), 0, new MCvScalar(0, 255, 0), 3);
                CvInvoke.Rectangle(debugImage, bounds, new MCvScalar(0, 0, 255), 2);
                _debugHelper.SaveDebugImage(debugImage, "detected_board_bounds");
            }

            return bounds;
        }

        private List<int> GroupNearbyPositions(List<int> positions, int threshold)
        {
            if (!positions.Any()) return new List<int>();

            // Sort positions to improve grouping
            var sortedPositions = positions.OrderBy(p => p).ToList();
            var groups = new List<List<int>> { new List<int> { sortedPositions[0] } };

            foreach (var pos in sortedPositions.Skip(1))
            {
                bool addedToExistingGroup = false;

                // Try to add to an existing group
                foreach (var group in groups)
                {
                    var avgPos = group.Average();
                    if (Math.Abs(pos - avgPos) <= threshold)
                    {
                        group.Add(pos);
                        addedToExistingGroup = true;
                        break;
                    }
                }

                // Create a new group if not added to any existing group
                if (!addedToExistingGroup)
                {
                    groups.Add(new List<int> { pos });
                }
            }

            // Return the average position of each group
            return groups.Select(g => (int)g.Average()).OrderBy(p => p).ToList();
        }

    }
}
