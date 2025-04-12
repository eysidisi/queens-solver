using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace LinkedInPuzzles.UI
{
    public class ScreenCaptureService
    {
        /// <summary>
        /// Information about the captured image resolution
        /// </summary>
        public class CaptureResolutionInfo
        {
            /// <summary>
            /// Width of the captured image in pixels
            /// </summary>
            public int Width { get; set; }

            /// <summary>
            /// Height of the captured image in pixels
            /// </summary>
            public int Height { get; set; }

            /// <summary>
            /// Horizontal resolution in dots per inch (DPI)
            /// </summary>
            public float HorizontalDpi { get; set; }

            /// <summary>
            /// Vertical resolution in dots per inch (DPI)
            /// </summary>
            public float VerticalDpi { get; set; }

            /// <summary>
            /// Physical width in inches (calculated from Width and HorizontalDpi)
            /// </summary>
            public float PhysicalWidthInches => Width / HorizontalDpi;

            /// <summary>
            /// Physical height in inches (calculated from Height and VerticalDpi)
            /// </summary>
            public float PhysicalHeightInches => Height / VerticalDpi;

            /// <summary>
            /// Total number of pixels in the image
            /// </summary>
            public int TotalPixels => Width * Height;

            /// <summary>
            /// Returns a formatted string with resolution information
            /// </summary>
            public override string ToString()
            {
                return $"{Width}x{Height} pixels ({Width / (float)Height:F2} ratio), " +
                       $"{HorizontalDpi:F0}x{VerticalDpi:F0} DPI, " +
                       $"{PhysicalWidthInches:F2}\"x{PhysicalHeightInches:F2}\"";
            }
        }

        /// <summary>
        /// Captures a specific region of the screen
        /// </summary>
        /// <param name="region">Rectangle defining the region to capture</param>
        /// <returns>Bitmap containing the captured screen region, or null if the region is invalid</returns>
        public Bitmap CaptureScreenRegion(Rectangle region)
        {
            if (region.Width <= 0 || region.Height <= 0)
                return null;

            // Create a bitmap to store the captured image
            Bitmap bitmap = new Bitmap(region.Width, region.Height);

            // Create a graphics object from the bitmap
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Capture the specified region of the screen
                g.CopyFromScreen(region.Left, region.Top, 0, 0, region.Size);
            }

            return bitmap;
        }

        /// <summary>
        /// Captures a specific region of the screen with a specific target resolution
        /// </summary>
        /// <param name="region">Rectangle defining the region to capture</param>
        /// <param name="targetWidth">Target width in pixels (0 to keep original)</param>
        /// <param name="targetHeight">Target height in pixels (0 to keep original)</param>
        /// <returns>Bitmap containing the captured screen region at the specified resolution</returns>
        public Bitmap CaptureScreenRegionWithResolution(Rectangle region, int targetWidth = 0, int targetHeight = 0)
        {
            // First capture at original resolution
            Bitmap original = CaptureScreenRegion(region);
            if (original == null)
                return null;

            // If no resize is needed, return the original
            if ((targetWidth <= 0 || targetWidth == region.Width) &&
                (targetHeight <= 0 || targetHeight == region.Height))
            {
                return original;
            }

            // Calculate target dimensions while maintaining aspect ratio if only one dimension is specified
            int width = targetWidth > 0 ? targetWidth : (int)(region.Width * (targetHeight / (float)region.Height));
            int height = targetHeight > 0 ? targetHeight : (int)(region.Height * (targetWidth / (float)region.Width));

            // Create a new bitmap with the target resolution
            Bitmap resized = new Bitmap(width, height);

            // Create graphics object for the new bitmap
            using (Graphics g = Graphics.FromImage(resized))
            {
                // Set high quality rendering
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // Draw the original image resized
                g.DrawImage(original, 0, 0, width, height);
            }

            // Dispose the original bitmap as we no longer need it
            original.Dispose();

            return resized;
        }

        /// <summary>
        /// Gets resolution information from a bitmap
        /// </summary>
        /// <param name="bitmap">The bitmap to analyze</param>
        /// <returns>CaptureResolutionInfo containing resolution details</returns>
        public CaptureResolutionInfo GetResolutionInfo(Bitmap bitmap)
        {
            if (bitmap == null)
                return null;

            return new CaptureResolutionInfo
            {
                Width = bitmap.Width,
                Height = bitmap.Height,
                HorizontalDpi = bitmap.HorizontalResolution,
                VerticalDpi = bitmap.VerticalResolution
            };
        }

        /// <summary>
        /// Saves the captured bitmap to a file
        /// </summary>
        /// <param name="bitmap">The bitmap to save</param>
        /// <param name="filePath">The file path to save to</param>
        /// <param name="format">The image format to use</param>
        public void SaveCapturedImage(Bitmap bitmap, string filePath, ImageFormat format = null)
        {
            if (bitmap == null || string.IsNullOrEmpty(filePath))
                return;

            // Default to PNG format if not specified
            format = format ?? ImageFormat.Png;

            // Save the bitmap to the specified file
            bitmap.Save(filePath, format);
        }

        /// <summary>
        /// Captures a specific region of the screen with high quality settings
        /// </summary>
        /// <param name="region">Rectangle defining the region to capture</param>
        /// <returns>Bitmap containing the high-quality captured screen region</returns>
        public Bitmap CaptureScreenRegionHighQuality(Rectangle region)
        {
            if (region.Width <= 0 || region.Height <= 0)
                return null;

            // Create a bitmap with high pixel depth (32 bits per pixel)
            Bitmap bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);

            // Set resolution to higher values (common high-quality print resolution is 300 DPI)
            bitmap.SetResolution(300, 300);

            // Create a graphics object with high quality settings
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Apply high quality rendering settings
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                // Capture the specified region of the screen
                g.CopyFromScreen(region.Left, region.Top, 0, 0, region.Size);
            }

            return bitmap;
        }

        /// <summary>
        /// Loads an image from file with high quality settings preserved
        /// </summary>
        /// <param name="filePath">Path to the image file</param>
        /// <returns>High-quality bitmap from the file</returns>
        public Bitmap LoadHighQualityImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            // Load the image with advanced options to preserve quality
            Bitmap original = null;

            try
            {
                // Create a high-quality bitmap by first loading the file without reducing color depth
                using (var tempImage = Image.FromFile(filePath))
                {
                    // Create a new bitmap with 32-bit ARGB format to preserve quality
                    original = new Bitmap(tempImage.Width, tempImage.Height, PixelFormat.Format32bppArgb);

                    // Preserve the original DPI
                    original.SetResolution(tempImage.HorizontalResolution, tempImage.VerticalResolution);

                    // Draw the original image with high quality settings
                    using (Graphics g = Graphics.FromImage(original))
                    {
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                        // Draw the image with full quality
                        g.DrawImage(tempImage, 0, 0, tempImage.Width, tempImage.Height);
                    }
                }

                return original;
            }
            catch (Exception)
            {
                // Fallback to standard loading if high-quality method fails
                if (original != null)
                    original.Dispose();

                return new Bitmap(filePath);
            }
        }
    }
}