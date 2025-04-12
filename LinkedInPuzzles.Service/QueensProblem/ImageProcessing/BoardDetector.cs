using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing;

namespace LinkedInPuzzles.Service.QueensProblem.ImageProcessing
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
            try
            {
                var dilated = PreprocessImage(colorImage);

                // 2. Find contours
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(
                    dilated,
                    contours,
                    null!,
                    RetrType.External,
                    ChainApproxMethod.ChainApproxSimple);

                // Debug: Draw each found contour on its own image and save it.
                for (int i = 0; i < contours.Size; i++)
                {
                    using (VectorOfPoint contour = contours[i])
                    {
                        // Clone the original image so each contour is drawn separately.
                        Mat singleContourImage = colorImage.Clone();
                        CvInvoke.Polylines(singleContourImage, contour, true, new MCvScalar(0, 255, 0), 2);
                        _debugHelper.SaveDebugImage(singleContourImage, $"contour_{i}");
                    }
                }

                // Store all candidate boards in this list
                List<(Point[] Corners, double Area)> candidateBoards = new();

                for (int i = 0; i < contours.Size; i++)
                {
                    VectorOfPoint contour = contours[i];
                    double area = CvInvoke.ContourArea(contour);

                    // Filter out very small areas
                    if (area < 1000)
                        continue;

                    // Use a smaller epsilon for more precise approximation
                    double peri = CvInvoke.ArcLength(contour, true);
                    VectorOfPoint approx = new VectorOfPoint();
                    CvInvoke.ApproxPolyDP(contour, approx, 0.01 * peri, true);

                    // If it has 4 corners, consider it a candidate
                    if (approx.Size == 4)
                    {
                        candidateBoards.Add((approx.ToArray(), area));
                    }
                }

                if (candidateBoards.Count == 0)
                {
                    throw new Exception("No quadrilateral board contours found.");
                }

                // 3. Evaluate each candidate by warping and checking grid consistency
                double bestScore = double.MinValue;
                Point[]? bestContour = null;
                int bestRows = 0, bestColumns = 0;
                Mat? bestWarp = null;

                foreach (var (corners, area) in candidateBoards)
                {
                    // Make aspect ratio check more lenient
                    Rectangle boundingRect = CvInvoke.BoundingRectangle(new VectorOfPoint(corners));
                    double aspectRatio = (double)boundingRect.Width / boundingRect.Height;
                    bool isSquare = aspectRatio > 0.8 && aspectRatio < 1.2; // More tolerant range
                    if (!isSquare)
                        continue;

                    // Add size check relative to image
                    double relativeArea = area / (colorImage.Width * colorImage.Height);
                    if (relativeArea < 0.1) // Board should be at least 10% of the image
                        continue;

                    // Warp this candidate
                    Point[] orderedPoints = OrderPoints(corners);
                    int boardSize = 450;
                    PointF[] destPoints =
                    [
                new PointF(0, 0),
                new PointF(boardSize - 1, 0),
                new PointF(boardSize - 1, boardSize - 1),
                new PointF(0, boardSize - 1)
                    ];

                    Mat transform = CvInvoke.GetPerspectiveTransform(
                        Array.ConvertAll(orderedPoints, p => new PointF(p.X, p.Y)),
                        destPoints);

                    Mat candidateWarp = new Mat();
                    CvInvoke.WarpPerspective(
                        colorImage,
                        candidateWarp,
                        transform,
                        new Size(boardSize, boardSize));

                    // Check grid dimensions on the warped candidate
                    var (rows, cols) = DetectGridDimensions(candidateWarp);

                    // Enforce minimum 4x4
                    if (rows < 4 || cols < 4)
                        continue;

                    // Define a scoring heuristic:
                    // - The more rows & columns we have, the higher the score.
                    // - Optionally, give a slight boost for area if multiple boards have the same rows/cols.
                    double candidateScore = rows * cols + area * 0.0001;

                    // Update best if this candidate is better
                    if (candidateScore > bestScore)
                    {
                        bestScore = candidateScore;
                        bestContour = corners;
                        bestRows = rows;
                        bestColumns = cols;
                        bestWarp = candidateWarp;
                    }
                }

                if (bestContour == null || bestWarp == null)
                {
                    throw new Exception("No valid board contour found after evaluating candidates.");
                }

                // 4. Debug: draw the chosen board contour on the original
                Mat contourDebug = colorImage.Clone();
                VectorOfPoint boardVector = new VectorOfPoint(bestContour);
                CvInvoke.Polylines(
                    contourDebug,
                    boardVector,
                    true,
                    new MCvScalar(0, 0, 255),
                    2);
                _debugHelper.SaveDebugImage(contourDebug, "final_board_contour");

                // 5. Return the best warp and its grid dimensions
                return (bestWarp.ToBitmap(), bestRows, bestColumns);
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

                        MCvScalar color = (r + c) % 2 == 0 ?
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

        public (Bitmap WarpedBoard, int Rows, int Columns) ExtractCurvedBoardAndAnalyze(Mat colorImage)
        {
            try
            {
                // 1. Preprocess the image
                Mat preprocessedImage = PreprocessImage(colorImage);

                // 2. Find and evaluate contours
                var (bestContour, bestBounds) = FindBestBoardContour(colorImage, preprocessedImage);

                if (bestContour == null)
                {
                    throw new Exception("No valid curved board contour found.");
                }

                // 3. Create warped board
                Mat warpedBoard = WarpBoardToSquare(colorImage, bestContour);

                // Save warped board with high quality
                SaveHighQualityImage(warpedBoard, "warped_board_hq");

                // 4. Analyze grid dimensions
                var (rows, cols) = DetectGridDimensions(warpedBoard);

                // 5. Debug visualization
                SaveDebugImages(colorImage, bestContour, bestBounds);

                return (warpedBoard.ToBitmap(), rows, cols);
            }
            catch (Exception ex)
            {
                _debugHelper.LogDebugMessage($"Curved board extraction failed: {ex.Message}");
                throw new Exception("Failed to extract curved board from image", ex);
            }
        }

        private Mat PreprocessImage(Mat colorImage)
        {
            _debugHelper.SaveDebugImage(colorImage, "original");

            // Convert to grayscale
            Mat gray = new Mat();
            CvInvoke.CvtColor(colorImage, gray, ColorConversion.Bgr2Gray);
            _debugHelper.SaveDebugImage(gray, "gray");

            // Blur
            Mat blurred = new Mat();
            CvInvoke.GaussianBlur(gray, blurred, new Size(5, 5), 0);
            _debugHelper.SaveDebugImage(blurred, "blurred");

            // Threshold
            Mat thresh = new Mat();
            CvInvoke.AdaptiveThreshold(
                blurred,
                thresh,
                255,
                AdaptiveThresholdType.MeanC,
                ThresholdType.BinaryInv,
                11,
                2);
            _debugHelper.SaveDebugImage(thresh, "thresh");

            // Morphological operations
            Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));

            Mat closed = new Mat();
            CvInvoke.MorphologyEx(thresh, closed, MorphOp.Close, kernel, new Point(-1, -1), 2, BorderType.Default, new MCvScalar(1));

            Mat dilated = new Mat();
            CvInvoke.Dilate(closed, dilated, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(1));
            _debugHelper.SaveDebugImage(dilated, "dilated");

            return dilated;
        }

        private (VectorOfPoint BestContour, Rectangle BestBounds) FindBestBoardContour(Mat colorImage, Mat preprocessedImage)
        {
            // Find contours
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(
                preprocessedImage,
                contours,
                null!,
                RetrType.External,
                ChainApproxMethod.ChainApproxSimple);

            // Debug contours
            Mat contourDebug = colorImage.Clone();
            for (int i = 0; i < contours.Size; i++)
            {
                CvInvoke.DrawContours(contourDebug, contours, i, new MCvScalar(0, 255, 0), 2);
            }
            _debugHelper.SaveDebugImage(contourDebug, "all_contours_curved");

            // Find best contour
            double maxArea = 0;
            VectorOfPoint bestContour = null;
            Rectangle bestBounds = Rectangle.Empty;

            for (int i = 0; i < contours.Size; i++)
            {
                using (VectorOfPoint contour = contours[i])
                {
                    if (!IsValidBoardContour(contour, colorImage, out Rectangle bounds))
                        continue;

                    double area = CvInvoke.ContourArea(contour);
                    if (area > maxArea)
                    {
                        maxArea = area;
                        bestContour = contour;
                        bestBounds = bounds;
                    }
                }
            }

            return (bestContour, bestBounds);
        }

        private bool IsValidBoardContour(VectorOfPoint contour, Mat colorImage, out Rectangle bounds)
        {
            bounds = Rectangle.Empty;
            double area = CvInvoke.ContourArea(contour);

            if (area < 1000)
                return false;

            // Get minimum area rectangle
            RotatedRect rotatedRect = CvInvoke.MinAreaRect(contour);
            PointF[] rectPoints = rotatedRect.GetVertices();
            bounds = CvInvoke.BoundingRectangle(Array.ConvertAll(rectPoints, p => new Point((int)p.X, (int)p.Y)));

            // Check aspect ratio
            double aspectRatio = (double)bounds.Width / bounds.Height;
            if (aspectRatio < 0.7 || aspectRatio > 1.3)
                return false;

            // Check relative area
            double relativeArea = area / (colorImage.Width * colorImage.Height);
            if (relativeArea < 0.1)
                return false;

            return true;
        }

        private Mat WarpBoardToSquare(Mat colorImage, VectorOfPoint bestContour)
        {
            // Create mask
            Mat mask = new Mat(colorImage.Size, DepthType.Cv8U, 1);
            mask.SetTo(new MCvScalar(0));
            CvInvoke.DrawContours(mask, new VectorOfVectorOfPoint(bestContour), 0, new MCvScalar(255), -1);
            _debugHelper.SaveDebugImage(mask, "board_mask");

            // Get corner points and create warped image
            int boardSize = 450;
            RotatedRect finalRect = CvInvoke.MinAreaRect(bestContour);
            PointF[] corners = finalRect.GetVertices();
            corners = OrderPointsF(corners);

            PointF[] destPoints =
            [
                new PointF(0, 0),
                new PointF(boardSize - 1, 0),
                new PointF(boardSize - 1, boardSize - 1),
                new PointF(0, boardSize - 1)
            ];

            Mat perspectiveMatrix = CvInvoke.GetPerspectiveTransform(corners, destPoints);
            Mat warpedBoard = new Mat();
            CvInvoke.WarpPerspective(colorImage, warpedBoard, perspectiveMatrix, new Size(boardSize, boardSize));

            // Debug: save the warped board
            _debugHelper.SaveDebugImage(warpedBoard, "warped_board");

            return warpedBoard;
        }

        private void SaveDebugImages(Mat colorImage, VectorOfPoint bestContour, Rectangle bestBounds)
        {
            Mat finalDebug = colorImage.Clone();
            CvInvoke.DrawContours(finalDebug, new VectorOfVectorOfPoint(bestContour), 0, new MCvScalar(0, 255, 0), 2);
            CvInvoke.Rectangle(finalDebug, bestBounds, new MCvScalar(0, 0, 255), 2);
            _debugHelper.SaveDebugImage(finalDebug, "final_curved_board");
        }

        private PointF[] OrderPointsF(PointF[] pts)
        {
            PointF[] rect = new PointF[4];
            // Sum of coordinates: smallest -> top-left, largest -> bottom-right
            var sorted = pts.OrderBy(p => p.X + p.Y).ToArray();
            rect[0] = sorted[0];
            rect[2] = sorted[sorted.Length - 1];

            // Difference of coordinates: smallest -> top-right, largest -> bottom-left
            var sortedDiff = pts.OrderBy(p => p.Y - p.X).ToArray();
            rect[1] = sortedDiff[0];
            rect[3] = sortedDiff[sortedDiff.Length - 1];

            return rect;
        }

        // Add a high quality image saving method
        private void SaveHighQualityImage(Mat image, string name)
        {
            if (!_debugHelper.IsDebugMode) return;

            string path = "debug_" + name + ".png";

            // Create image compression parameters for PNG with maximum quality
            var compressionParams = new List<KeyValuePair<ImwriteFlags, int>>
            {
                new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.PngCompression, 0), // 0 = no compression, maximum quality
                new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.PngStrategy, 0) // 0 = default strategy
            };

            CvInvoke.Imwrite(path, image, compressionParams.ToArray());
            _debugHelper.LogDebugMessage($"High quality image saved: {path}");

            // Also save in TIFF format for lossless quality
            string tiffPath = "debug_" + name + ".tiff";
            var tiffParams = new List<KeyValuePair<ImwriteFlags, int>>
            {
                new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.TiffCompression, 1) // 1 = no compression
            };

            CvInvoke.Imwrite(tiffPath, image, tiffParams.ToArray());
            _debugHelper.LogDebugMessage($"Lossless TIFF image saved: {tiffPath}");
        }
    }
}
