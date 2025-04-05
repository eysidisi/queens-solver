using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Drawing;

namespace QueensProblem.Service.ZipProblem.ImageProcessing
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
        /// Checks if the image contains a circle
        /// </summary>
        public bool ContainsCircle(Mat image)
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

                // Contour-based detection
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(binary, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                    for (int i = 0; i < contours.Size; i++)
                    {
                        double area = CvInvoke.ContourArea(contours[i]);
                        double perimeter = CvInvoke.ArcLength(contours[i], true);
                        double circularity = 0;

                        if (perimeter > 0)
                        {
                            circularity = (4 * Math.PI * area) / (perimeter * perimeter);
                        }

                        double minArea = binary.Width * binary.Height * 0.3; // 30% of image
                        double maxArea = binary.Width * binary.Height * 0.9; // 90% of image

                        if (area > minArea &&
                            area < maxArea &&
                            circularity > 0.5)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Extracts the content inside the detected circle
        /// </summary>
        public Mat ExtractCircleContent(Mat cell, bool saveDebugImages = false, string debugPrefix = "")
        {
            using (Mat gray = new Mat())
            using (Mat binary = new Mat())
            {
                // Convert to grayscale if needed
                if (cell.NumberOfChannels > 1)
                {
                    CvInvoke.CvtColor(cell, gray, ColorConversion.Bgr2Gray);
                }
                else
                {
                    cell.CopyTo(gray);
                }

                // Blur to reduce noise
                CvInvoke.GaussianBlur(gray, gray, new Size(5, 5), 1);

                // Use adaptive thresholding for better segmentation
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

                        double minArea = binary.Width * binary.Height * 0.3;
                        double maxArea = binary.Width * binary.Height * 0.9;

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
                            cell.CopyTo(result, mask);

                            Rectangle boundingRect = CvInvoke.BoundingRectangle(contours[bestCircleIndex]);

                            if (boundingRect.Width > 5 && boundingRect.Height > 5 &&
                                boundingRect.X >= 0 && boundingRect.Y >= 0 &&
                                boundingRect.X + boundingRect.Width <= result.Width &&
                                boundingRect.Y + boundingRect.Height <= result.Height)
                            {
                                // Apply small padding inside the circle (focus on the digit)
                                int padding = Math.Min(boundingRect.Width, boundingRect.Height) / 10;
                                Rectangle paddedRect = new Rectangle(
                                    boundingRect.X + padding,
                                    boundingRect.Y + padding,
                                    boundingRect.Width - padding * 2,
                                    boundingRect.Height - padding * 2
                                );

                                if (paddedRect.Width > 0 && paddedRect.Height > 0)
                                {
                                    Mat cropped = new Mat(result, paddedRect);
                                    if (saveDebugImages && _debugHelper != null)
                                    {
                                        _debugHelper.SaveDebugImage(cropped, $"{debugPrefix}_circle_content");
                                    }
                                    return cropped;
                                }
                                
                                Mat boundedResult = new Mat(result, boundingRect);
                                if (saveDebugImages && _debugHelper != null)
                                {
                                    _debugHelper.SaveDebugImage(boundedResult, $"{debugPrefix}_circle_content_bounded");
                                }
                                return boundedResult;
                            }
                            
                            if (saveDebugImages && _debugHelper != null)
                            {
                                _debugHelper.SaveDebugImage(result, $"{debugPrefix}_circle_content_full");
                            }
                            return result;
                        }
                    }
                }

                // If no circle is found, return the original image
                return cell.Clone();
            }
        }
    }
} 