using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;
using System.Xml.Linq;
using Tesseract;

namespace LinkedInPuzzles.Service.ZipProblem.ImageProcessing
{
    /// <summary>
    /// Recognizes digits in images using OCR
    /// </summary>
    public class DigitRecognizer : IDisposable
    {
        private readonly DebugHelper _debugHelper;
        private readonly TesseractEngine _tesseract;

        public DigitRecognizer(DebugHelper debugHelper, string tessdataPath)
        {
            _debugHelper = debugHelper;

            EnsureTessDataExists(tessdataPath);

            // Initialize Tesseract
            // Include '0' in the whitelist so that multi-digit numbers (e.g., "10") can be recognized.
            _tesseract = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
            _tesseract.SetVariable("tessedit_char_whitelist", "0123456789");
            _tesseract.SetVariable("classify_bln_numeric_mode", "1");
            _tesseract.SetVariable("text_is_digit_only", "1");
            _tesseract.SetVariable("tessedit_write_images", "true");
        }

        /// <summary>
        /// Recognizes multi-digit numbers (e.g., "10", "11") from the given image.
        /// Disallows single-digit '0' as a valid output.
        /// </summary>
        /// <param name="image">The input image (ROI with the digit(s))</param>
        /// <returns>An integer representing the recognized number, or 0 if none / invalid.</returns>
        public int RecognizeDigit(Mat image)
        {
            try
            {
                // Basic image dimension check
                if (image.Width <= 1 || image.Height <= 1)
                {
                    return 0; // Invalid image
                }

                // Define preprocessing parameters
                var parameters = new PreprocessingParameters
                {
                    MorphKernelSize = 2,
                    DilateIterations = 2
                };

                // Preprocess the image for OCR
                using (Mat processedImage = PreprocessForOCR(image, parameters))
                {
                    _debugHelper.SaveDebugImage(processedImage, $"cell_processed_{Random.Shared.Next()}");

                    // Convert to bitmap for Tesseract
                    using (Bitmap bmp = processedImage.ToBitmap())
                    using (var pix = Pix.LoadFromMemory(ImageToByte(bmp)))
                    {
                        // Use SingleBlock to allow multi-digit detection.
                        using (var page = _tesseract.Process(pix, PageSegMode.SingleLine))
                        {
                            string rawText = page.GetText().Trim();
                            float confidence = page.GetMeanConfidence();

                            _debugHelper.LogDebugMessage(
                                $"OCR detected text: '{rawText}' with confidence: {confidence:F3}");


                            // Clean out non-digit characters
                            string cleanedText = new string(rawText.Where(char.IsDigit).ToArray());

                            if (string.IsNullOrEmpty(cleanedText) || IsAllZeros(cleanedText))
                            {
                                throw new Exception("Detected text is empty or all zeros.");
                            }

                            // Attempt to parse the cleaned text as an integer
                            if (int.TryParse(cleanedText, out int recognizedNumber))
                            {
                                _debugHelper.LogDebugMessage(
                                    $"Parsed cleaned text: '{cleanedText}' => {recognizedNumber}");
                                return recognizedNumber;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _debugHelper.LogDebugMessage($"Error detecting number: {ex.Message}");
            }

            // No valid number detected
            return 0;
        }

        /// <summary>
        /// Converts a Bitmap to a byte array in PNG format for Tesseract consumption.
        /// </summary>
        private byte[] ImageToByte(Bitmap img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Basic check to see if a string is composed solely of '0' characters
        /// </summary>
        private bool IsAllZeros(string text)
        {
            // True if every character is '0'
            return text.All(ch => ch == '0');
        }

        /// <summary>
        /// Preprocesses the given image (ROI) for Tesseract OCR: resize, threshold, morphological operations.
        /// </summary>
        private Mat PreprocessForOCR(Mat image, PreprocessingParameters parameters)
        {
            // Clone the input to avoid modifying the original.
            Mat processed = image.Clone();

            // 1. Resize the image to a fixed target height (e.g., 32 pixels)
            int targetSize = 32; // This target height works well with screenshots.
            double scale = (double)targetSize / processed.Height;
            Size newSize = new Size((int)(processed.Width * scale), targetSize);
            CvInvoke.Resize(processed, processed, newSize, 0, 0, Inter.Cubic);

            // 2. Convert to grayscale if necessary.
            if (processed.NumberOfChannels > 1)
            {
                CvInvoke.CvtColor(processed, processed, ColorConversion.Bgr2Gray);
            }

            // 3. Since the input is clean, avoid heavy blurring. A light blur (if any) using a 3x3 kernel is enough.
            //CvInvoke.GaussianBlur(processed, processed, new Size(3, 3), 0);

            // 4. Apply Otsu's thresholding to get a clean binary image.
            Mat binary = new Mat();
            CvInvoke.Threshold(processed, binary, 0, 255, ThresholdType.Binary | ThresholdType.Otsu);

            // 5. Ensure that the image shows dark digits on a white background.
            double sumOfPixels = CvInvoke.Sum(binary).V0;
            double totalPixelValue = 255.0 * binary.Width * binary.Height;
            double whitePixelRatio = sumOfPixels / totalPixelValue;
            if (whitePixelRatio < 0.5)
            {
                CvInvoke.BitwiseNot(binary, binary);
            }

            // 6. Apply a very light morphological closing to clean minor gaps inside digit strokes.
            // Use only closing and do not apply dilation to avoid merging adjacent "1" digits.
            int kernelSize = parameters.MorphKernelSize > 1 ? parameters.MorphKernelSize : 3;
            using (Mat element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(kernelSize, kernelSize), new Point(-1, -1)))
            {
                // Use closing to fix small holes in individual digits.
                CvInvoke.MorphologyEx(binary, binary, MorphOp.Close, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                //if (parameters.DilateIterations > 0)
                //{
                //    CvInvoke.MorphologyEx(binary, binary, MorphOp.Dilate, element, new Point(-1, -1),
                //        parameters.DilateIterations, BorderType.Default, new MCvScalar());
                //}
            }

            return binary;
        }

        /// <summary>
        /// Ensures that Tesseract data files are present.
        /// </summary>
        private void EnsureTessDataExists(string tessdataPath)
        {
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
