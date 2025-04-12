using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;

namespace LinkedInPuzzles.Service.ZipProblem.ImageProcessing
{
    /// <summary>
    /// Detects walls and sets up connectivity between cells in a Zip board
    /// </summary>
    public unsafe class ConnectivityDetector
    {
        private readonly DebugHelper _debugHelper;

        public ConnectivityDetector(DebugHelper debugHelper)
        {
            _debugHelper = debugHelper;
        }

        /// <summary>
        /// Detects walls between cells and updates connectivity in the ZipBoard
        /// </summary>
        public void DetectWallsAndSetupConnectivity(Mat image, ZipBoard zipBoard, int numberOfCells)
        {
            // Calculate cell dimensions
            int cellHeight = image.Height / numberOfCells;
            int cellWidth = image.Width / numberOfCells;

            // Calculate adaptive parameters based on image resolution
            int numSamplePoints = Math.Max(10, Math.Min(20, cellWidth / 10)); // Scale sample points with cell width
            int borderOffset = Math.Max(2, Math.Min(5, Math.Min(cellWidth, cellHeight) / 30)); // Scale border check range with cell size
            int wallDetectionThreshold = Math.Max(3, numSamplePoints / 3); // At least 1/3 of sample points should be dark
            int binaryThreshold = 50; // Default threshold

            // Preprocess the image to enhance wall detection
            Mat processed = PreprocessForWallDetection(image, binaryThreshold);

            // Create 2D arrays to keep track of walls
            bool[,] horizontalWalls = new bool[numberOfCells, numberOfCells]; // Walls between rows
            bool[,] verticalWalls = new bool[numberOfCells, numberOfCells];   // Walls between columns

            // Define a higher threshold for wall thickness
            int thickLineThreshold = Math.Max(3, Math.Min(cellWidth, cellHeight) / 40);

            // Detect walls
            DetectWalls(processed, numberOfCells, cellWidth, cellHeight,
                numSamplePoints, borderOffset, thickLineThreshold, wallDetectionThreshold,
                horizontalWalls, verticalWalls);

            // Set up connectivity based on detected walls
            zipBoard.ResetAndSetupNeighbors((node1, node2) =>
            {
                // Check if nodes are adjacent
                if (Math.Abs(node1.Row - node2.Row) + Math.Abs(node1.Col - node2.Col) != 1)
                    return false; // Not adjacent

                // Determine if there's a wall between these nodes
                if (node1.Row == node2.Row)
                {
                    // Horizontal neighbors (check vertical wall)
                    int leftCol = Math.Min(node1.Col, node2.Col);
                    int rightCol = Math.Max(node1.Col, node2.Col);
                    return !verticalWalls[node1.Row, rightCol]; // No wall between them
                }
                else
                {
                    // Vertical neighbors (check horizontal wall)
                    int topRow = Math.Min(node1.Row, node2.Row);
                    int bottomRow = Math.Max(node1.Row, node2.Row);
                    return !horizontalWalls[bottomRow, node1.Col]; // No wall between them
                }
            });

            // Generate visualization
            Mat wallVisualization = GenerateWallVisualization(image, zipBoard, numberOfCells,
                cellWidth, cellHeight, horizontalWalls, verticalWalls);
            _debugHelper.SaveDebugImage(wallVisualization, "detected_walls");
        }

        /// <summary>
        /// Preprocess the image to enhance wall detection
        /// </summary>
        private Mat PreprocessForWallDetection(Mat image, int binaryThreshold)
        {
            // Convert to grayscale for processing
            Mat gray = new Mat();
            if (image.NumberOfChannels > 1)
            {
                CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);
            }
            else
            {
                image.CopyTo(gray);
            }

            // Apply Gaussian blur to smooth out noise
            Mat blurred = new Mat();
            CvInvoke.GaussianBlur(gray, blurred, new Size(3, 3), 0);

            // Threshold to detect dark lines
            Mat binary = new Mat();
            CvInvoke.Threshold(blurred, binary, binaryThreshold, 255, ThresholdType.Binary);
            _debugHelper.SaveDebugImage(binary, "binary_original");

            // Create a copy for morphological operations
            Mat processed = new Mat();
            binary.CopyTo(processed);

            // Invert the image so walls are white and background is black
            CvInvoke.BitwiseNot(processed, processed);
            _debugHelper.SaveDebugImage(processed, "binary_inverted");

            // Invert back so walls are black again
            CvInvoke.BitwiseNot(processed, processed);
            _debugHelper.SaveDebugImage(processed, "final_processed");

            return processed;
        }

        /// <summary>
        /// Detect horizontal and vertical walls
        /// </summary>
        private void DetectWalls(Mat processed, int numberOfCells, int cellWidth, int cellHeight,
            int numSamplePoints, int borderOffset, int thickLineThreshold, int wallDetectionThreshold,
            bool[,] horizontalWalls, bool[,] verticalWalls)
        {
            // Detect horizontal and vertical walls
            for (int row = 0; row < numberOfCells; row++)
            {
                for (int col = 0; col < numberOfCells; col++)
                {
                    // Check horizontal wall (between current cell and cell above)
                    if (row > 0)
                    {
                        // Sample points along the border line
                        int numDarkPixels = 0;
                        int maxThickness = 0; // Track max thickness of the line

                        for (int i = 0; i < numSamplePoints; i++)
                        {
                            int x = col * cellWidth + (cellWidth * i / numSamplePoints);
                            int y = row * cellHeight; // Top border of current cell

                            // Check for dark pixels and measure thickness
                            int thickness = 0;
                            int extendedOffset = Math.Max(5, borderOffset * 2); // Look further to measure thickness

                            for (int offset = -extendedOffset; offset <= extendedOffset; offset++)
                            {
                                int sampleY = y + offset;
                                if (sampleY >= 0 && sampleY < processed.Height)
                                {
                                    // Check if pixel is dark (part of a wall)
                                    byte* ptr = (byte*)processed.DataPointer;
                                    int idx = sampleY * processed.Step + x;
                                    if (ptr[idx] < 128)
                                    {
                                        thickness++;
                                    }
                                }
                            }

                            // If thickness exceeds threshold, count as dark pixel for wall detection
                            if (thickness > thickLineThreshold)
                            {
                                numDarkPixels++;
                            }

                            maxThickness = Math.Max(maxThickness, thickness);
                        }

                        // If we have enough dark pixels, it's likely a wall
                        horizontalWalls[row, col] = numDarkPixels >= wallDetectionThreshold;
                        _debugHelper.LogDebugMessage($"Horizontal wall between [{row - 1},{col}] and [{row},{col}]: {horizontalWalls[row, col]} ({numDarkPixels}/{numSamplePoints}, max thickness={maxThickness})");
                    }

                    // Check vertical wall (between current cell and cell to the left)
                    if (col > 0)
                    {
                        // Sample points along the border line
                        int numDarkPixels = 0;
                        int maxThickness = 0; // Track max thickness of the line

                        for (int i = 0; i < numSamplePoints; i++)
                        {
                            int y = row * cellHeight + (cellHeight * i / numSamplePoints);
                            int x = col * cellWidth; // Left border of current cell

                            // Check for dark pixels and measure thickness
                            int thickness = 0;
                            int extendedOffset = Math.Max(5, borderOffset * 2); // Look further to measure thickness

                            for (int offset = -extendedOffset; offset <= extendedOffset; offset++)
                            {
                                int sampleX = x + offset;
                                if (sampleX >= 0 && sampleX < processed.Width)
                                {
                                    // Check if pixel is dark (part of a wall)
                                    byte* ptr = (byte*)processed.DataPointer;
                                    int idx = y * processed.Step + sampleX;
                                    if (ptr[idx] < 128)
                                    {
                                        thickness++;
                                    }
                                }
                            }

                            // If thickness exceeds threshold, count as dark pixel for wall detection
                            if (thickness > thickLineThreshold)
                            {
                                numDarkPixels++;
                            }

                            maxThickness = Math.Max(maxThickness, thickness);
                        }

                        // If we have enough dark pixels, it's likely a wall
                        verticalWalls[row, col] = numDarkPixels >= wallDetectionThreshold;
                        _debugHelper.LogDebugMessage($"Vertical wall between [{row},{col - 1}] and [{row},{col}]: {verticalWalls[row, col]} ({numDarkPixels}/{numSamplePoints}, max thickness={maxThickness})");
                    }
                }
            }
        }

        /// <summary>
        /// Generate visualization of walls and cell numbers
        /// </summary>
        private Mat GenerateWallVisualization(Mat image, ZipBoard zipBoard, int numberOfCells,
            int cellWidth, int cellHeight, bool[,] horizontalWalls, bool[,] verticalWalls)
        {
            Mat wallVisualization = new Mat(image.Size, DepthType.Cv8U, 3);
            wallVisualization.SetTo(new MCvScalar(255, 255, 255)); // White background

            // Draw grid
            for (int i = 0; i <= numberOfCells; i++)
            {
                // Draw thin grid lines
                CvInvoke.Line(
                    wallVisualization,
                    new Point(0, i * cellHeight),
                    new Point(image.Width, i * cellHeight),
                    new MCvScalar(200, 200, 200), 1);

                CvInvoke.Line(
                    wallVisualization,
                    new Point(i * cellWidth, 0),
                    new Point(i * cellWidth, image.Height),
                    new MCvScalar(200, 200, 200), 1);
            }

            // Draw detected walls and cell numbers
            for (int row = 0; row < numberOfCells; row++)
            {
                for (int col = 0; col < numberOfCells; col++)
                {
                    // Draw thick lines for walls
                    if (row > 0 && horizontalWalls[row, col])
                    {
                        CvInvoke.Line(
                            wallVisualization,
                            new Point(col * cellWidth, row * cellHeight),
                            new Point((col + 1) * cellWidth, row * cellHeight),
                            new MCvScalar(0, 0, 0), 5);
                    }

                    if (col > 0 && verticalWalls[row, col])
                    {
                        CvInvoke.Line(
                            wallVisualization,
                            new Point(col * cellWidth, row * cellHeight),
                            new Point(col * cellWidth, (row + 1) * cellHeight),
                            new MCvScalar(0, 0, 0), 5);
                    }

                    // Draw cell numbers
                    int number = zipBoard.GetNode(row, col).Order;
                    if (number > 0)
                    {
                        CvInvoke.PutText(
                            wallVisualization,
                            number.ToString(),
                            new Point(col * cellWidth + cellWidth / 2 - 10, row * cellHeight + cellHeight / 2 + 10),
                            FontFace.HersheyDuplex,
                            1.0,
                            new MCvScalar(0, 0, 255), 2);
                    }
                }
            }

            return wallVisualization;
        }
    }
}