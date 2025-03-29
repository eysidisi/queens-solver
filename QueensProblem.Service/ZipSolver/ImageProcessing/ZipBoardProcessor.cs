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
        static int count = 0;

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
            zipBoard.SetupNeighbors((node1, node2) => true);

            return zipBoard;
        }

        private int DetectNumberInCell(Mat cellMat, int row, int col)
        {
            try
            {
                // Preprocess the cell image for better OCR performance
                using (Mat processedCell = PreprocessCellForOCR(cellMat))
                {
                    _debugHelper.SaveDebugImage(processedCell, $"processed_cell_{row}_{col}");

                    // Convert Mat to Bitmap
                    using (Bitmap bmp = processedCell.ToBitmap())
                    {
                        // Convert Bitmap to Pix (Tesseract's image format)
                        using (var pix = Pix.LoadFromMemory(ImageToByte(bmp)))
                        {
                            // Optionally, you can still set some OCR variables here if needed.
                            // Here we use the overload that accepts PageSegMode.SingleChar for better single-digit detection.
                            using (var page = _tesseract.Process(pix, PageSegMode.SingleChar))
                            {
                                string text = page.GetText().Trim();
                                float confidence = page.GetMeanConfidence();

                                _debugHelper.LogDebugMessage($"OCR detected text: '{text}' with confidence: {confidence}");

                                // Try to parse the detected text as a number
                                if (int.TryParse(text, out int number))
                                {
                                    return number;
                                }

                                // If initial parse fails, clean the text further
                                var cleanedText = new string(text.Where(c => char.IsDigit(c)).ToArray());
                                if (!string.IsNullOrEmpty(cleanedText) && int.TryParse(cleanedText, out number))
                                {
                                    return number;
                                }
                            }
                        }
                    }
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

        private Mat PreprocessCellForOCR(Mat cellMat)
        {
            // Create a working copy
            Mat processed = new Mat();
            cellMat.CopyTo(processed);

            // Resize for better OCR performance (scaling up can help)
            CvInvoke.Resize(processed, processed, new Size(cellMat.Width * 3, cellMat.Height * 3));

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
            Mat element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
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
                    for (int i = 0; i < contours.Size; i++)
                    {
                        // Get contour area and perimeter
                        double area = CvInvoke.ContourArea(contours[i]);
                        double perimeter = CvInvoke.ArcLength(contours[i], true);

                        // Check if it's large enough to consider
                        if (area > 100)  // Adjust threshold as needed
                        {
                            // Calculate circularity: 4*π*area/perimeter²
                            // A perfect circle has circularity = 1
                            double circularity = (4 * Math.PI * area) / (perimeter * perimeter);

                            // If circularity is close to 1, it's likely a circle
                            if (circularity > 0.7)  // Adjust threshold as needed
                            {
                                circleDetected = true;
                                _debugHelper.LogDebugMessage($"Circle detected with circularity: {circularity}");
                                break;
                            }
                        }
                    }
                }
            }

            // If no circle is detected, return a blank image (indicating no number)
            if (!circleDetected)
            {
                _debugHelper.LogDebugMessage("No circle detected in cell, returning blank image");
                Mat blank = new Mat(processed.Size, DepthType.Cv8U, 1);
                blank.SetTo(new MCvScalar(0)); // Black background
                return blank;
            }

            // Continue with the rest of the processing
            CvInvoke.MorphologyEx(binary, binary, MorphOp.Dilate, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

            // Find contours again to focus on the region containing the number
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(binary, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                if (contours.Size > 0)
                {
                    // Locate the largest contour (assumed to be the circle with the number)
                    int largestContourIndex = 0;
                    double largestContourArea = 0;

                    for (int i = 0; i < contours.Size; i++)
                    {
                        double area = CvInvoke.ContourArea(contours[i]);
                        if (area > largestContourArea)
                        {
                            largestContourArea = area;
                            largestContourIndex = i;
                        }
                    }

                    // Create a mask from the largest contour
                    Mat mask = new Mat(binary.Size, DepthType.Cv8U, 1);
                    mask.SetTo(new MCvScalar(0));
                    CvInvoke.DrawContours(mask, contours, largestContourIndex, new MCvScalar(255), -1);
                    CvInvoke.BitwiseAnd(binary, mask, binary);

                    // Crop to the bounding rectangle of the largest contour
                    Rectangle boundingRect = CvInvoke.BoundingRectangle(contours[largestContourIndex]);

                    if (boundingRect.Width > 5 && boundingRect.Height > 5 &&
                        boundingRect.X >= 0 && boundingRect.Y >= 0 &&
                        boundingRect.X + boundingRect.Width <= binary.Width &&
                        boundingRect.Y + boundingRect.Height <= binary.Height)
                    {
                        // Add a small padding around the detected number
                        int padding = 5;
                        Rectangle paddedRect = new Rectangle(
                            Math.Max(0, boundingRect.X - padding),
                            Math.Max(0, boundingRect.Y - padding),
                            Math.Min(binary.Width - boundingRect.X, boundingRect.Width + padding * 2),
                            Math.Min(binary.Height - boundingRect.Y, boundingRect.Height + padding * 2)
                        );
                        binary = new Mat(binary, paddedRect);
                    }
                }
            }

            // Enhance contrast
            CvInvoke.EqualizeHist(binary, binary);

            // Ensure white digits on a dark background; if less than 30% is white, invert the image
            MCvScalar binarySum = CvInvoke.Sum(binary);
            double binaryWhiteRatio = binarySum.V0 / (255.0 * binary.Width * binary.Height);
            if (binaryWhiteRatio < 0.3)
            {
                CvInvoke.BitwiseNot(binary, binary);
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

        public void Dispose()
        {
            _tesseract?.Dispose();
        }
    }
}
