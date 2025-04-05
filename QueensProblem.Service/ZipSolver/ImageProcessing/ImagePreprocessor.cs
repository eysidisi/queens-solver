using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Drawing;

namespace QueensProblem.Service.ZipSolver.ImageProcessing
{
    /// <summary>
    /// Handles image preprocessing operations for better OCR recognition
    /// </summary>
    public class ImagePreprocessor
    {
        private readonly DebugHelper _debugHelper;

        public ImagePreprocessor(DebugHelper debugHelper)
        {
            _debugHelper = debugHelper;
        }

        /// <summary>
        /// Preprocesses an image for optimal OCR recognition
        /// </summary>
        public Mat PreprocessForOCR(Mat image, PreprocessingParameters parameters)
        {
            // Create a working copy
            Mat processed = new Mat();
            image.CopyTo(processed);

            // 1. Resize the image so its height is approximately the target character height (32 pixels)
            int targetHeight = 32;
            double scale = (double)targetHeight / processed.Height;
            Size newSize = new Size((int)(processed.Width * scale), targetHeight);
            // Use a good interpolation method for enlarging/shrinking (bicubic)
            CvInvoke.Resize(processed, processed, newSize, 0, 0, Inter.Cubic);

            // 2. Convert to grayscale if needed
            if (processed.NumberOfChannels > 1)
            {
                CvInvoke.CvtColor(processed, processed, ColorConversion.Bgr2Gray);
            }

            // 3. Apply Otsu's thresholding to create a binary image
            Mat binary = new Mat();
            CvInvoke.Threshold(processed, binary, 0, 255, ThresholdType.Otsu | ThresholdType.Binary);

            // 4. Ensure the digit is dark on a white background (standard for OCR)
            MCvScalar sum = CvInvoke.Sum(binary);
            double whitePixelRatio = sum.V0 / (255.0 * binary.Width * binary.Height);
            if (whitePixelRatio < 0.5)
            {
                CvInvoke.BitwiseNot(binary, binary);
            }

            // 5. Remove noise using morphological operations
            int kernelSize = parameters.MorphKernelSize > 1 ? parameters.MorphKernelSize : 3;
            Mat element = CvInvoke.GetStructuringElement(
                ElementShape.Rectangle, 
                new Size(kernelSize, kernelSize), 
                new Point(-1, -1));

            // Apply closing to remove noise
            CvInvoke.MorphologyEx(binary, binary, MorphOp.Close, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

            // Apply dilation if specified in parameters
            if (parameters.DilateIterations > 0)
            {
                CvInvoke.MorphologyEx(binary, binary, MorphOp.Dilate, element, new Point(-1, -1),
                    parameters.DilateIterations, BorderType.Default, new MCvScalar());
            }

            return binary;
        }

        /// <summary>
        /// Enhances contrast in an image
        /// </summary>
        public Mat EnhanceContrast(Mat image)
        {
            Mat result = new Mat();
            image.CopyTo(result);
            
            if (result.NumberOfChannels > 1)
            {
                CvInvoke.CvtColor(result, result, ColorConversion.Bgr2Gray);
            }
            
            CvInvoke.EqualizeHist(result, result);
            return result;
        }
    }
    
    /// <summary>
    /// Parameters for image preprocessing operations
    /// </summary>
    public class PreprocessingParameters
    {
        /// <summary>
        /// Size of kernel for morphological operations
        /// </summary>
        public int MorphKernelSize { get; set; } = 3;
        
        /// <summary>
        /// Number of dilation iterations to apply
        /// </summary>
        public int DilateIterations { get; set; } = 1;
    }
} 