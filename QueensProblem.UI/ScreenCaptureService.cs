using System.Drawing;
using System.Drawing.Imaging;

namespace QueensProblem.UI
{
    public class ScreenCaptureService
    {
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
    }
} 