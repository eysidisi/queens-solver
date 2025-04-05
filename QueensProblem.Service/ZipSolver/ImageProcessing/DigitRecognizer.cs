using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Tesseract;

namespace QueensProblem.Service.ZipSolver.ImageProcessing
{
    /// <summary>
    /// Recognizes digits in images using OCR 
    /// </summary>
    public class DigitRecognizer : IDisposable
    {
        private readonly DebugHelper _debugHelper;
        private readonly TesseractEngine _tesseract;
        private readonly ImagePreprocessor _preprocessor;

        public DigitRecognizer(DebugHelper debugHelper, string tessdataPath = null)
        {
            _debugHelper = debugHelper;
            _preprocessor = new ImagePreprocessor(debugHelper);

            // Initialize tesseract
            if (string.IsNullOrEmpty(tessdataPath))
            {
                tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            }

            EnsureTessDataExists(tessdataPath);
            
            _tesseract = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
            
            // Configure Tesseract for digit recognition
            _tesseract.SetVariable("tessedit_char_whitelist", "123456789");
            _tesseract.SetVariable("classify_bln_numeric_mode", "1");
        }

        public int RecognizeDigit(Mat image, int row, int col)
        {
            try
            {
                if (image.Width <= 1 || image.Height <= 1)
                {
                    return 0; // Invalid image
                }

                // Define preprocessing parameters
                var parameters = new PreprocessingParameters
                {
                    MorphKernelSize = 2,
                    DilateIterations = 1
                };

                // Preprocess the image for OCR
                using (Mat processedImage = _preprocessor.PreprocessForOCR(image, parameters))
                {
                    _debugHelper.SaveDebugImage(processedImage, $"cell_{row}_{col}_processed");
                    
                    List<DetectionResult> results = new List<DetectionResult>();
                    
                    // Convert to bitmap for Tesseract
                    using (Bitmap bmp = processedImage.ToBitmap())
                    using (var pix = Pix.LoadFromMemory(ImageToByte(bmp)))
                    using (var page = _tesseract.Process(pix, PageSegMode.SingleChar))
                    {
                        string text = page.GetText().Trim();
                        float confidence = page.GetMeanConfidence();

                        _debugHelper.LogDebugMessage(
                            $"OCR detected text: '{text}' with confidence: {confidence:F3}");

                        // Attempt to parse the detected text as a number
                        if (int.TryParse(text, out int number))
                        {
                            results.Add(new DetectionResult
                            {
                                Number = number,
                                Confidence = confidence,
                                Parameters = parameters
                            });
                        }
                        else
                        {
                            // Clean the text and try parsing again
                            string cleanedText = new string(text.Where(c => char.IsDigit(c)).ToArray());
                            if (!string.IsNullOrEmpty(cleanedText) &&
                                int.TryParse(cleanedText, out number))
                            {
                                results.Add(new DetectionResult
                                {
                                    Number = number,
                                    Confidence = confidence * 0.9f,
                                    Parameters = parameters
                                });
                            }
                        }
                    }

                    // Process and return the most confident result
                    if (results.Count > 0)
                    {
                        results.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
                        return results[0].Number;
                    }
                }
            }
            catch (Exception ex)
            {
                _debugHelper.LogDebugMessage($"Error detecting number: {ex.Message}");
            }

            return 0; // No digit detected or error
        }

        private byte[] ImageToByte(Bitmap img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

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