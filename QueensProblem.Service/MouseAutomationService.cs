using QueensProblem.Service.Algorithm;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QueensProblem.Service
{
    /// <summary>
    /// Service for automating mouse actions to solve the Queens Problem
    /// </summary>
    public class MouseAutomationService
    {
        #region Win32 API Imports

        // Windows API for mouse events
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        // For absolute positioning
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        #endregion

        #region Constants

        // Constants for mouse events
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        #endregion

        #region Configuration

        // Configurable settings
        private readonly int mouseDownDelay;
        private readonly int clickDelay;
        private readonly int movementDelay;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the MouseAutomationService with default settings
        /// </summary>
        public MouseAutomationService() : this(5, 5, 5)
        {
        }

        /// <summary>
        /// Creates a new instance of the MouseAutomationService with custom settings
        /// </summary>
        /// <param name="mouseDownDelay">Delay in ms between mouse down and up events</param>
        /// <param name="clickDelay">Delay in ms between each queen placement</param>
        /// <param name="movementDelay">Delay in ms after cursor movement before clicking</param>
        public MouseAutomationService(int mouseDownDelay, int clickDelay, int movementDelay)
        {
            this.mouseDownDelay = mouseDownDelay;
            this.clickDelay = clickDelay;
            this.movementDelay = movementDelay;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Simulates mouse clicks at the queen positions on the original screen region
        /// </summary>
        /// <param name="queens">Array of queen positions</param>
        /// <param name="screenRegion">The original screen region containing the board</param>
        /// <param name="delayBetweenClicks">Optional override for delay between clicks</param>
        /// <param name="boardBounds">Optional rectangle specifying the actual board bounds within the screenshot</param>
        /// <returns>Task representing the async operation</returns>
        public async Task ClickQueenPositions(Queen[] queens, Rectangle screenRegion, int? delayBetweenClicks = null, Rectangle? boardBounds = null)
        {
            ValidateParameters(queens, screenRegion);
            
            int actualDelay = delayBetweenClicks ?? clickDelay;
            int boardSize = queens.Length;
            
            // If no board bounds provided, attempt to detect them or use full region
            Rectangle actualBoardBounds = boardBounds ?? DetectBoardBounds(screenRegion, boardSize);
            
            // Calculate cell dimensions based on the actual board boundaries
            int cellWidth = actualBoardBounds.Width / boardSize;
            int cellHeight = actualBoardBounds.Height / boardSize;
            
            // Calculate the absolute position of the board within the screen
            int boardStartX = screenRegion.Left + actualBoardBounds.Left;
            int boardStartY = screenRegion.Top + actualBoardBounds.Top;
            
            System.Diagnostics.Debug.WriteLine($"Board detected at: X={boardStartX}, Y={boardStartY}, Width={actualBoardBounds.Width}, Height={actualBoardBounds.Height}");
            System.Diagnostics.Debug.WriteLine($"Cell dimensions: Width={cellWidth}, Height={cellHeight}");
            
            for (int i = 0; i < queens.Length; i++)
            {
                var queen = queens[i];
                
                // Calculate screen coordinates for the center of the cell
                int screenX = boardStartX + (queen.Col * cellWidth) + (cellWidth / 2);
                int screenY = boardStartY + (queen.Row * cellHeight) + (cellHeight / 2);
                
                System.Diagnostics.Debug.WriteLine($"Clicking Queen at: {queen.Row},{queen.Col} -> Screen position: {screenX},{screenY}");
                
                // Move cursor to position
                MoveCursorTo(screenX, screenY);
                
                // Small delay to ensure cursor is positioned correctly
                await Task.Delay(movementDelay);
                
                // Perform mouse click
                PerformMouseDoubleClick();
                
                // Progress information
                OnProgressChanged(i + 1, queens.Length, new Point(screenX, screenY));
                
                // Delay between clicks (except after the last click)
                if (i < queens.Length - 1)
                {
                    await Task.Delay(actualDelay);
                }
            }
        }
        
        #endregion

        #region Private Methods

        /// <summary>
        /// Attempts to detect the board boundaries within the given screen region
        /// </summary>
        /// <param name="screenRegion">The entire captured region</param>
        /// <param name="boardSize">The size of the board (number of cells per side)</param>
        /// <returns>A Rectangle representing the detected board bounds within the screenshot</returns>
        private Rectangle DetectBoardBounds(Rectangle screenRegion, int boardSize)
        {
            // If we can't access the screenshot data directly here, we'll use a heuristic approach

            // Default margin estimation (assuming 10% of the region might be padding)
            int estimatedMargin = Math.Min(screenRegion.Width, screenRegion.Height) / 20;
            
            // Create a reduced rectangle with estimated margins
            return new Rectangle(
                estimatedMargin,                        // X offset from left of screenshot
                estimatedMargin,                        // Y offset from top of screenshot
                screenRegion.Width - (estimatedMargin * 2),  // Width of actual board
                screenRegion.Height - (estimatedMargin * 2)  // Height of actual board
            );
        }
        
        /// <summary>
        /// Validates input parameters for the automation operation
        /// </summary>
        private void ValidateParameters(Queen[] queens, Rectangle screenRegion)
        {
            if (queens == null || queens.Length == 0)
            {
                throw new ArgumentException("Queens array cannot be null or empty", nameof(queens));
            }
            
            if (screenRegion.IsEmpty)
            {
                throw new ArgumentException("Screen region cannot be empty", nameof(screenRegion));
            }
            
            if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
            {
                throw new ArgumentException("Screen region must have positive width and height", nameof(screenRegion));
            }
            
            // Verify that queen positions are within the board boundaries
            int boardSize = queens.Length;
            foreach (var queen in queens)
            {
                if (queen.Row < 0 || queen.Row >= boardSize || queen.Col < 0 || queen.Col >= boardSize)
                {
                    throw new ArgumentException($"Queen position ({queen.Row}, {queen.Col}) is outside the board boundaries", nameof(queens));
                }
            }
        }
        
        /// <summary>
        /// Moves the cursor to the specified screen coordinates
        /// </summary>
        private void MoveCursorTo(int x, int y)
        {
            // Use Windows API for more reliable positioning
            SetCursorPos(x, y);
        }

        /// <summary>
        /// Performs a mouse double-click at the current cursor position
        /// </summary>
        private void PerformMouseDoubleClick()
        {
            // Simulate a mouse double-click using Windows API
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(mouseDownDelay); // Short delay between down and up events
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            Thread.Sleep(mouseDownDelay); // Short delay between down and up events
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(mouseDownDelay); // Short delay between down and up events
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        /// <summary>
        /// Raises the progress changed event
        /// </summary>
        private void OnProgressChanged(int current, int total, Point location)
        {
            // Could be expanded to include a proper event system if needed
            System.Diagnostics.Debug.WriteLine($"Mouse automation progress: {current}/{total} at position {location}");
        }
        
        #endregion
    }
}
