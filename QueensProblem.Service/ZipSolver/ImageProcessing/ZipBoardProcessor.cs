using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using QueensProblem.Service.ZipProblem;
using Tesseract;

namespace QueensProblem.Service.ZipSolver.ImageProcessing
{
    public class ZipBoardProcessor : IDisposable
    {
        private readonly DebugHelper _debugHelper;
        private readonly TesseractEngine _tesseract;

        public ZipBoardProcessor(DebugHelper debugHelper)
        {
            _debugHelper = debugHelper;

            // Ensure tessdata exists
            EnsureTessDataExists();

            // Initialize Tesseract with English language data
            string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            _tesseract = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);

            // Configure Tesseract for digit recognition (set once here)
            _tesseract.SetVariable("tessedit_char_whitelist", "123456789");
            _tesseract.SetVariable("classify_bln_numeric_mode", "1");

        }

        public ZipBoard ProcessImage(Mat colorImage, int numberOfCells)
        {
            _debugHelper.SaveDebugImage(colorImage, "input_board");

            // Create and analyze the ZipBoard
            var zipBoard = AnalyzeProcessedBoard(colorImage, numberOfCells);
            // Add a senity check to ensure the board is valid. Numbers should be unique and in the range 1-9 and theyere can't be any missing numbers.
            if (!zipBoard.IsValid())
            {
                throw new Exception("Invalid board detected. Please ensure all numbers are unique and in the range 1-9 with no missing numbers.");
            }

            // Detect walls between cells and update connectivity
            DetectWallsAndSetupConnectivity(colorImage, zipBoard, numberOfCells);

            return zipBoard;
        }

        private ZipBoard AnalyzeProcessedBoard(Mat processedBoard, int numberOfCells)
        {
            var zipBoard = new ZipBoard(numberOfCells, numberOfCells);

            // Calculate cell dimensions
            int cellHeight = processedBoard.Height / numberOfCells;
            int cellWidth = processedBoard.Width / numberOfCells;

            // Process each cell
            for (int row = 0; row < numberOfCells; row++)
            {
                for (int col = 0; col < numberOfCells; col++)
                {
                    Rectangle cellRect = new Rectangle(
                        col * cellWidth,
                        row * cellHeight,
                        cellWidth,
                        cellHeight
                    );

                    using (Mat cellMat = new Mat(processedBoard, cellRect))
                    {
                        int cellNumber = DetectNumberInCell(cellMat, row, col);
                        zipBoard.SetNodeOrder(row, col, cellNumber);

                        // Save debug image of processed cell
                        _debugHelper.SaveDebugImage(cellMat, $"cell_{row}_{col}_number_{cellNumber}");
                    }
                }
            }

            // Set up default connectivity
            zipBoard.ResetAndSetupNeighbors((node1, node2) => true);

            return zipBoard;
        }

        private int DetectNumberInCell(Mat cellMat, int row, int col)
        {
            try
            {
                // Define different preprocessing parameter sets to try
                var parameterSets = new List<PreprocessingParameters>
                {
                    // Default parameters
                    new PreprocessingParameters {
                        ScaleFactor = 3,
                        CircularityThreshold = 0.7f,
                        MorphKernelSize = 3,
                        DilateIterations = 1
                    },
                    // Parameters optimized for 5 detection
                    new PreprocessingParameters {
                        ScaleFactor = 4,
                        CircularityThreshold = 0.65f,
                        MorphKernelSize = 2,
                        DilateIterations = 1,
                        IsForNumberFive = true
                    },
                    // Parameters with minimal dilation (helps with 5 vs 6 distinction)
                    new PreprocessingParameters {
                        ScaleFactor = 3,
                        CircularityThreshold = 0.7f,
                        MorphKernelSize = 3,
                        DilateIterations = 0
                    }
                };

                List<DetectionResult> results = new List<DetectionResult>();
                int callNum = 0;
                // Try each parameter set
                foreach (var parameters in parameterSets)
                {
                    callNum++;
                    using (Mat processedCell = PreprocessCellForOCR(cellMat, parameters, row, col, callNum))
                    {
                        if (processedCell.Width <= 1 || processedCell.Height <= 1)
                        {
                            // Empty cell (no circle detected)
                            continue;
                        }


                        // Convert Mat to Bitmap
                        using (Bitmap bmp = processedCell.ToBitmap())
                        {
                            // Convert Bitmap to Pix (Tesseract's image format)
                            using (var pix = Pix.LoadFromMemory(ImageToByte(bmp)))
                            {
                                // Use the overload that accepts PageSegMode.SingleChar for better single-digit detection
                                using (var page = _tesseract.Process(pix, PageSegMode.SingleChar))
                                {
                                    string text = page.GetText().Trim();
                                    float confidence = page.GetMeanConfidence();

                                    _debugHelper.LogDebugMessage($"OCR detected text: '{text}' with confidence: {confidence} (params: {parameters.GetHashCode()})");

                                    // Try to parse the detected text as a number
                                    if (int.TryParse(text, out int number))
                                    {
                                        // Adjust confidence based on parameters
                                        float adjustedConfidence = confidence;

                                        // Apply special weighting for 5 vs 6 distinction
                                        if (parameters.IsForNumberFive && number == 5)
                                        {
                                            adjustedConfidence *= 1.1f;  // Boost confidence if using 5-optimized params
                                        }
                                        else if (!parameters.IsForNumberFive && number == 6)
                                        {
                                            adjustedConfidence *= 1.05f;  // Slightly boost confidence for 6 with standard params
                                        }

                                        results.Add(new DetectionResult
                                        {
                                            Number = number,
                                            Confidence = adjustedConfidence,
                                            Parameters = parameters
                                        });
                                    }
                                    else
                                    {
                                        // If initial parse fails, clean the text further
                                        var cleanedText = new string(text.Where(c => char.IsDigit(c)).ToArray());
                                        if (!string.IsNullOrEmpty(cleanedText) && int.TryParse(cleanedText, out number))
                                        {
                                            results.Add(new DetectionResult
                                            {
                                                Number = number,
                                                Confidence = confidence * 0.9f,  // Slightly reduce confidence for cleaned text
                                                Parameters = parameters
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Process results
                if (results.Count > 0)
                {
                    // Sort by confidence (highest first)
                    results.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));

                    // Check for 5 vs 6 conflicts
                    bool has5 = results.Any(r => r.Number == 5);
                    bool has6 = results.Any(r => r.Number == 6);

                    if (has5 && has6)
                    {
                        _debugHelper.LogDebugMessage("Detected both 5 and 6, applying special handling");

                        var fiveResults = results.Where(r => r.Number == 5).ToList();
                        var sixResults = results.Where(r => r.Number == 6).ToList();

                        // Compare the highest confidence of each
                        float maxFiveConfidence = fiveResults.Max(r => r.Confidence);
                        float maxSixConfidence = sixResults.Max(r => r.Confidence);

                        // If confidence difference is significant, choose the higher one
                        if (Math.Abs(maxFiveConfidence - maxSixConfidence) > 0.1f)
                        {
                            return maxFiveConfidence > maxSixConfidence ? 5 : 6;
                        }
                        else
                        {
                            // If confidence is similar, prefer the result from the parameters optimized for 5s
                            bool fiveDetectedWithOptimizedParams = fiveResults.Any(r => r.Parameters.IsForNumberFive);
                            if (fiveDetectedWithOptimizedParams)
                            {
                                _debugHelper.LogDebugMessage("Choosing 5 based on specialized parameters");
                                return 5;
                            }

                            // Otherwise return the most confident result (already sorted)
                            return results[0].Number;
                        }
                    }

                    // Return the most confident detection
                    return results[0].Number;
                }
            }
            catch (Exception ex)
            {
                _debugHelper.LogDebugMessage($"Error detecting number: {ex.Message}");
            }

            return 0; // Return 0 if no number is detected or on error
        }

        private byte[] ImageToByte(Bitmap img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private Mat PreprocessCellForOCR(Mat cellMat, PreprocessingParameters parameters, int row, int col, int callNum)
        {
            // Create a working copy
            Mat processed = new Mat();
            cellMat.CopyTo(processed);

            // Resize for better OCR performance (scaling up can help)
            CvInvoke.Resize(processed, processed, new Size(cellMat.Width * parameters.ScaleFactor, cellMat.Height * parameters.ScaleFactor));

            // Convert to grayscale if the image is in color
            if (processed.NumberOfChannels > 1)
            {
                CvInvoke.CvtColor(processed, processed, ColorConversion.Bgr2Gray);
            }

            // Apply threshold to convert to a binary image
            Mat binary = new Mat();
            CvInvoke.Threshold(processed, binary, 0, 255, ThresholdType.Otsu | ThresholdType.Binary);

            // If more than 50% is white, invert (so that the digit is isolated)
            MCvScalar sum = CvInvoke.Sum(binary);
            double whitePixelRatio = sum.V0 / (255.0 * binary.Width * binary.Height);
            if (whitePixelRatio > 0.5)
            {
                CvInvoke.BitwiseNot(binary, binary);
            }

            // Clean up noise with morphological operations
            Mat element = CvInvoke.GetStructuringElement(
                parameters.IsForNumberFive ? ElementShape.Cross : ElementShape.Rectangle,
                new Size(parameters.MorphKernelSize, parameters.MorphKernelSize),
                new Point(-1, -1));

            // Opening to remove noise
            CvInvoke.MorphologyEx(binary, binary, MorphOp.Open, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

            // Check if there's a circle in the image (as numbers are always in circles)
            bool circleDetected = false;
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                // Find contours
                CvInvoke.FindContours(binary, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                // Analyze contours to find circles
                if (contours.Size > 0)
                {
                    // Create a debug visualization image to show contours
                    Mat contourViz = new Mat(binary.Size, DepthType.Cv8U, 3);
                    CvInvoke.CvtColor(binary, contourViz, ColorConversion.Gray2Bgr);

                    // Instead of just taking the largest contour, find the most circle-like contour
                    int bestCircleIndex = -1;
                    double bestCircleScore = 0;
                    int largestContourIndex = -1;
                    double largestContourArea = 0;

                    _debugHelper.LogDebugMessage($"Found {contours.Size} contours in cell");

                    // Analyze all contours to find the most circle-like one with sufficient area
                    for (int i = 0; i < contours.Size; i++)
                    {
                        double area = CvInvoke.ContourArea(contours[i]);
                        double perimeter = CvInvoke.ArcLength(contours[i], true);

                        // Calculate circularity: 4*π*area/perimeter²
                        // A perfect circle has circularity = 1
                        double circularity = (4 * Math.PI * area) / (perimeter * perimeter);

                        // Calculate aspect ratio of the bounding rect
                        Rectangle boundingRect = CvInvoke.BoundingRectangle(contours[i]);
                        double aspectRatio = boundingRect.Width > 0 ?
                            (double)boundingRect.Height / boundingRect.Width : 0;

                        // Draw this contour on the visualization
                        MCvScalar contourColor;
                        int contourThickness = 1;

                        // Track the largest contour
                        if (area > largestContourArea)
                        {
                            largestContourArea = area;
                            largestContourIndex = i;
                        }

                        // Only consider contours with reasonable area
                        if (area > 100 && area < (binary.Width * binary.Height * 0.8))
                        {
                            // Compute a circle score that considers:
                            // 1. How circular it is (circularity)
                            // 2. How square/round the bounding rect is (aspect ratio)
                            double aspectRatioScore = 1.0 - Math.Abs(1.0 - aspectRatio);
                            double circleScore = circularity * 0.7 + aspectRatioScore * 0.3;

                            // Log all potential circle candidates
                            _debugHelper.LogDebugMessage($"Contour {i}: Area={area:F1}, Circularity={circularity:F3}, " +
                                $"AspectRatio={aspectRatio:F2}, CircleScore={circleScore:F3}");

                            // If it's a good circle candidate, check if it's the best one so far
                            if (circularity > parameters.CircularityThreshold && circleScore > bestCircleScore)
                            {
                                bestCircleScore = circleScore;
                                bestCircleIndex = i;
                                contourColor = new MCvScalar(0, 255, 0); // Green for the best circle
                                contourThickness = 2;
                            }
                            else
                            {
                                contourColor = new MCvScalar(255, 0, 0); // Red for non-circle contours
                            }
                        }
                        else
                        {
                            contourColor = new MCvScalar(0, 0, 255); // Blue for too small/large contours
                        }

                        // Draw the contour and its bounding rect on the visualization
                        CvInvoke.DrawContours(contourViz, contours, i, contourColor, contourThickness);
                        Rectangle rect = CvInvoke.BoundingRectangle(contours[i]);
                        CvInvoke.Rectangle(contourViz, rect, contourColor, 1);
                    }

                    // Save the contour visualization
                    _debugHelper.SaveDebugImage(contourViz, $"cell_{row}_{col}_countours");
                    
                    // If we found a good circle, use it; otherwise fall back to the largest contour
                    int selectedContourIndex = bestCircleIndex != -1 ? bestCircleIndex : largestContourIndex;
                    
                    // Set flag based on whether we found a valid circle
                    circleDetected = bestCircleIndex != -1;
                    
                    if (selectedContourIndex != -1)
                    {
                        // Create a mask from the selected contour
                        Mat mask = new Mat(binary.Size, DepthType.Cv8U, 1);
                        mask.SetTo(new MCvScalar(0));
                        CvInvoke.DrawContours(mask, contours, selectedContourIndex, new MCvScalar(255), -1);
                        CvInvoke.BitwiseAnd(binary, mask, binary);
                        
                        // Crop to the bounding rectangle of the selected contour
                        Rectangle boundingRect = CvInvoke.BoundingRectangle(contours[selectedContourIndex]);
                        
                        _debugHelper.LogDebugMessage($"Selected contour index: {selectedContourIndex}, " +
                            $"Is best circle: {selectedContourIndex == bestCircleIndex}, " +
                            $"Bounding rect: {boundingRect}");
                        
                        if (boundingRect.Width > 5 && boundingRect.Height > 5 &&
                            boundingRect.X >= 0 && boundingRect.Y >= 0 &&
                            boundingRect.X + boundingRect.Width <= binary.Width &&
                            boundingRect.Y + boundingRect.Height <= binary.Height)
                        {
                            // Add padding around the detected number
                            int padding = parameters.IsForNumberFive ? 8 : 5;  // More padding for 5s
                            Rectangle paddedRect = new Rectangle(
                                Math.Max(0, boundingRect.X - padding),
                                Math.Max(0, boundingRect.Y - padding),
                                Math.Min(binary.Width - boundingRect.X, boundingRect.Width + padding * 2),
                                Math.Min(binary.Height - boundingRect.Y, boundingRect.Height + padding * 2)
                            );
                            binary = new Mat(binary, paddedRect);
                            
                            // Also save the cropped region
                            _debugHelper.SaveDebugImage(binary, $"cell_cropped_{row}_{col}");
                        }
                    }
                }
            }

            _debugHelper.SaveDebugImage(binary, $"cell_{row}_{col}_after_circle_{callNum}");

            // If no circle is detected, return a blank image (indicating no number)
            if (!circleDetected)
            {
                Mat blank = new Mat(processed.Size, DepthType.Cv8U, 1);
                blank.SetTo(new MCvScalar(0)); // Black background
                return blank;
            }

            // Enhance contrast
            CvInvoke.EqualizeHist(binary, binary);

            // Ensure white digits on a dark background
            MCvScalar binarySum = CvInvoke.Sum(binary);
            double binaryWhiteRatio = binarySum.V0 / (255.0 * binary.Width * binary.Height);
            if (binaryWhiteRatio < 0.3)
            {
                CvInvoke.BitwiseNot(binary, binary);
            }

            // For parameters optimized for 5, apply a slight thinning to emphasize the distinctive shape
            if (parameters.IsForNumberFive)
            {
                Mat thinElement = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(2, 2), new Point(-1, -1));
                CvInvoke.MorphologyEx(binary, binary, MorphOp.Erode, thinElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
            }

            // Apply dilation if required by the parameters
            if (parameters.DilateIterations > 0)
            {
                CvInvoke.MorphologyEx(binary, binary, MorphOp.Dilate, element, new Point(-1, -1),
                    parameters.DilateIterations, BorderType.Default, new MCvScalar());
            }

            return binary;
        }

        private void EnsureTessDataExists()
        {
            string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            string engDataPath = Path.Combine(tessdataPath, "eng.traineddata");

            if (!Directory.Exists(tessdataPath))
            {
                Directory.CreateDirectory(tessdataPath);
            }

            if (!File.Exists(engDataPath))
            {
                throw new FileNotFoundException(
                    "Tesseract English language data file not found. Please ensure 'eng.traineddata' " +
                    "is present in the tessdata directory, or install the Tesseract.Data.English NuGet package.");
            }
        }

        public unsafe void DetectWallsAndSetupConnectivity(Mat image, ZipBoard zipBoard, int numberOfCells)
        {
            // Calculate cell dimensions
            int cellHeight = image.Height / numberOfCells;
            int cellWidth = image.Width / numberOfCells;

            // Calculate adaptive parameters based on image resolution
            int numSamplePoints = Math.Max(10, Math.Min(20, cellWidth / 10)); // Scale sample points with cell width
            int borderOffset = Math.Max(2, Math.Min(5, Math.Min(cellWidth, cellHeight) / 30)); // Scale border check range with cell size
            int wallDetectionThreshold = Math.Max(3, numSamplePoints / 3); // At least 1/3 of sample points should be dark
            int binaryThreshold = 50; // Default threshold

            _debugHelper.LogDebugMessage($"Wall detection parameters: samplePoints={numSamplePoints}, borderOffset={borderOffset}, threshold={wallDetectionThreshold}");

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

            // Auto-adjust binary threshold based on image histogram if needed
            if (image.Width > 1000 || image.Height > 1000) // For high-resolution images
            {
                // Compute image histogram
                Mat hist = new Mat();
                float[] ranges = { 0, 256 };
                using (VectorOfMat images = new VectorOfMat(gray))
                {
                    CvInvoke.CalcHist(images, new int[] { 0 }, null, hist, new int[] { 64 }, ranges, false);
                }

                // Find histogram peaks to determine a better threshold
                double minVal = 0, maxVal = 0;
                Point minLoc = new Point(), maxLoc = new Point();
                CvInvoke.MinMaxLoc(hist, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                // Adjust threshold based on histogram peak locations
                double peakPos = maxLoc.Y * 4; // scale back from 64 bins to 256 range
                binaryThreshold = (int)Math.Min(Math.Max(peakPos / 2, 30), 80); // Keep between 30-80

                _debugHelper.LogDebugMessage($"Auto-adjusted binary threshold to {binaryThreshold} for high-resolution image");
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

            // Calculate kernel size based on cell dimensions - large enough to close thin grid lines
            int closingKernelSize = Math.Max(5, Math.Min(cellWidth, cellHeight) / 20);
            _debugHelper.LogDebugMessage($"Using closing kernel size: {closingKernelSize}");

            // Create a structuring element for closing operation
            Mat closingElement = CvInvoke.GetStructuringElement(
                ElementShape.Ellipse, // Elliptical element works better for connecting regions
                new Size(closingKernelSize, closingKernelSize),
                new Point(-1, -1));

            // Apply a strong closing operation to connect white regions (dilate then erode)
            Mat closed = new Mat();
            CvInvoke.MorphologyEx(processed, closed, MorphOp.Close, closingElement, new Point(-1, -1), 2, BorderType.Default, new MCvScalar());
            _debugHelper.SaveDebugImage(closed, "after_closing");

            // Now erode to restore the thick walls somewhat
            Mat smallElement = CvInvoke.GetStructuringElement(
                ElementShape.Rectangle,
                new Size(3, 3),
                new Point(-1, -1));
            CvInvoke.MorphologyEx(closed, processed, MorphOp.Erode, smallElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
            _debugHelper.SaveDebugImage(processed, "after_erode");

            // Invert back so walls are black again
            CvInvoke.BitwiseNot(processed, processed);
            _debugHelper.SaveDebugImage(processed, "final_processed");

            // Create 2D arrays to keep track of walls
            bool[,] horizontalWalls = new bool[numberOfCells, numberOfCells]; // Walls between rows
            bool[,] verticalWalls = new bool[numberOfCells, numberOfCells];   // Walls between columns

            // Define a higher threshold for wall thickness
            int thickLineThreshold = Math.Max(3, Math.Min(cellWidth, cellHeight) / 40);
            _debugHelper.LogDebugMessage($"Thick line threshold: {thickLineThreshold} pixels");

            // Detect horizontal walls
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

            // Generate and save a debug visualization of the detected walls
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

            // Draw detected walls
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

            _debugHelper.SaveDebugImage(wallVisualization, "detected_walls");
        }

        public void Dispose()
        {
            _tesseract?.Dispose();
        }
    }

    // Class to hold preprocessing parameters
    public class PreprocessingParameters
    {
        public int ScaleFactor { get; set; } = 3;
        public float CircularityThreshold { get; set; } = 0.6f;
        public int MorphKernelSize { get; set; } = 3;
        public int DilateIterations { get; set; } = 1;
        public bool IsForNumberFive { get; set; } = false;
    }

    // Class to hold detection results
    public class DetectionResult
    {
        public int Number { get; set; }
        public float Confidence { get; set; }
        public PreprocessingParameters Parameters { get; set; }
    }
}

