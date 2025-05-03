using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing;

namespace LinkedInPuzzles.Service.ZipProblem.ImageProcessing
{
    /// <summary>
    /// Detects and extracts content from circles in images
    /// </summary>
    public class CircleDetector
    {
        private readonly DebugHelper _debugHelper;

        public CircleDetector(DebugHelper debugHelper)
        {
            _debugHelper = debugHelper;
        }

        /// <summary>
        /// Attempts to find a circle in the image and extract its content if found
        /// </summary>
        /// <param name="image">The input image to process</param>
        /// <param name="saveDebugImages">Whether to save debug images</param>
        /// <param name="debugPrefix">Prefix for debug image filenames</param>
        /// <returns>A tuple containing: (bool circleFound, Mat extractedContent)</returns>
        public (bool circleFound, Mat extractedContent) FindAndExtractCircle(Mat image)
        {
            using (Mat gray = new Mat())
            using (Mat binary = new Mat())
            {
                // Convert to grayscale
                if (image.NumberOfChannels > 1)
                {
                    CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);
                }
                else
                {
                    image.CopyTo(gray);
                }

                // Blur to reduce noise
                CvInvoke.GaussianBlur(gray, gray, new Size(5, 5), 1);

                // Use adaptive thresholding for better results on complex backgrounds
                CvInvoke.AdaptiveThreshold(gray, binary, 255, AdaptiveThresholdType.MeanC, ThresholdType.BinaryInv, 11, 2);

                // Find contours
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(binary, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                    int bestCircleIndex = -1;
                    double bestCircularity = 0;

                    for (int i = 0; i < contours.Size; i++)
                    {
                        double area = CvInvoke.ContourArea(contours[i]);
                        double perimeter = CvInvoke.ArcLength(contours[i], true);
                        double circularity = 0;

                        if (perimeter > 0)
                        {
                            circularity = (4 * Math.PI * area) / (perimeter * perimeter);
                        }

                        double minArea = binary.Width * binary.Height * 0.1; // 20% of image
                        double maxArea = binary.Width * binary.Height * 0.8; // 90% of image

                        if (area > minArea &&
                            area < maxArea &&
                            circularity > 0.5 &&
                            circularity > bestCircularity)
                        {
                            bestCircleIndex = i;
                            bestCircularity = circularity;
                        }
                    }

                    if (bestCircleIndex >= 0)
                    {
                        using (Mat mask = new Mat(binary.Size, DepthType.Cv8U, 1))
                        {
                            mask.SetTo(new MCvScalar(0));
                            CvInvoke.DrawContours(mask, contours, bestCircleIndex, new MCvScalar(255), -1);

                            Mat result = new Mat();
                            image.CopyTo(result, mask);

                            Rectangle boundingRect = CvInvoke.BoundingRectangle(contours[bestCircleIndex]);

                            if (boundingRect.Width > 5 && boundingRect.Height > 5 &&
                                boundingRect.X >= 0 && boundingRect.Y >= 0 &&
                                boundingRect.X + boundingRect.Width <= result.Width &&
                                boundingRect.Y + boundingRect.Height <= result.Height)
                            {
                                // Make the rectangle square by using the maximum dimension
                                int maxDimension = Math.Max(boundingRect.Width, boundingRect.Height);
                                int minDimension = Math.Min(boundingRect.Width, boundingRect.Height);
                                int centerX = boundingRect.X + boundingRect.Width / 2;
                                int centerY = boundingRect.Y + boundingRect.Height / 2;
                                
                                // Create a square rectangle centered on the original rectangle
                                Rectangle squareRect = new Rectangle(
                                    centerX - maxDimension / 2,
                                    centerY - maxDimension / 2,
                                    maxDimension,
                                    maxDimension
                                );

                                // Ensure the square rectangle stays within image bounds
                                squareRect.X = Math.Max(0, Math.Min(squareRect.X, result.Width - maxDimension));
                                squareRect.Y = Math.Max(0, Math.Min(squareRect.Y, result.Height - maxDimension));

                                // Apply small padding inside the circle (focus on the digit)
                                int padding = minDimension / 6;
                                Rectangle paddedRect = new Rectangle(
                                    squareRect.X + padding,
                                    squareRect.Y + padding,
                                    squareRect.Width - padding * 2,
                                    squareRect.Height - padding * 2
                                );

                                if (paddedRect.Width > 0 && paddedRect.Height > 0)
                                {
                                    Mat cropped = new Mat(result, paddedRect);
                                    if (_debugHelper != null)
                                    {
                                        _debugHelper.SaveDebugImage(cropped, $"circle_content");
                                    }
                                    return (true, cropped);
                                }

                                Mat boundedResult = new Mat(result, squareRect);
                                return (true, boundedResult);
                            }

                            return (true, result);
                        }
                    }
                }

                // No circle found
                return (false, null);
            }
        }

    }
}