using QueensProblem.Service.QueensProblem.Algorithm;
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

        // Win32 API to get current cursor position
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

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
        
        /// <summary>
        /// Simulates mouse drag operations between ZipNode positions
        /// </summary>
        /// <param name="nodes">List of ZipNode positions to connect by dragging</param>
        /// <param name="screenRegion">The original screen region containing the board</param>
        /// <param name="delayBetweenDrags">Optional override for delay between drag operations</param>
        /// <param name="boardBounds">Optional rectangle specifying the actual board bounds within the screenshot</param>
        /// <returns>Task representing the async operation</returns>
        public async Task DragBetweenZipNodes(List<ZipProblem.ZipNode> nodes, Rectangle screenRegion, int? delayBetweenDrags = null, Rectangle? boardBounds = null)
        {
            if (nodes == null || nodes.Count == 0)
            {
                throw new ArgumentException("Nodes list cannot be null or empty", nameof(nodes));
            }
            
            if (screenRegion.IsEmpty)
            {
                throw new ArgumentException("Screen region cannot be empty", nameof(screenRegion));
            }
            
            if (screenRegion.Width <= 0 || screenRegion.Height <= 0)
            {
                throw new ArgumentException("Screen region must have positive width and height", nameof(screenRegion));
            }
            
            int actualDelay = delayBetweenDrags ?? clickDelay;
            
            // Determine board size from the maximum row/column in the nodes
            int maxRow = 0;
            int maxCol = 0;
            foreach (var node in nodes)
            {
                maxRow = Math.Max(maxRow, node.Row);
                maxCol = Math.Max(maxCol, node.Col);
            }
            int boardSize = Math.Max(maxRow, maxCol) + 1;
            
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
            
            if (nodes.Count < 2)
            {
                // If only one node, just click it
                var node = nodes[0];
                int screenX = boardStartX + (node.Col * cellWidth) + (cellWidth / 2);
                int screenY = boardStartY + (node.Row * cellHeight) + (cellHeight / 2);
                
                MoveCursorTo(screenX, screenY);
                await Task.Delay(movementDelay);
                PerformMouseClick();
                
                OnProgressChanged(1, 1, new Point(screenX, screenY));
                return;
            }
            
            // Process the first node - click only, no drag
            var firstNode = nodes[0];
            int firstScreenX = boardStartX + (firstNode.Col * cellWidth) + (cellWidth / 2);
            int firstScreenY = boardStartY + (firstNode.Row * cellHeight) + (cellHeight / 2);
            
            System.Diagnostics.Debug.WriteLine($"Clicking first node at: {firstNode.Row},{firstNode.Col} -> Screen position: {firstScreenX},{firstScreenY}");
            
            // Move cursor to first position and click
            MoveCursorTo(firstScreenX, firstScreenY);
            await Task.Delay(movementDelay);
            PerformMouseClick();
            
            // Progress information
            OnProgressChanged(1, nodes.Count, new Point(firstScreenX, firstScreenY));
            
            // Delay before starting drag operations
            await Task.Delay(actualDelay);
            
            // For each subsequent node, perform a drag operation
            for (int i = 1; i < nodes.Count; i++)
            {
                var node = nodes[i];
                
                // Calculate screen coordinates for the center of the cell
                int screenX = boardStartX + (node.Col * cellWidth) + (cellWidth / 2);
                int screenY = boardStartY + (node.Row * cellHeight) + (cellHeight / 2);
                
                System.Diagnostics.Debug.WriteLine($"Dragging to node at: {node.Row},{node.Col} -> Screen position: {screenX},{screenY}");
                
                // Perform drag operation from previous position to current node
                await PerformMouseDrag(screenX, screenY);
                
                // Progress information
                OnProgressChanged(i + 1, nodes.Count, new Point(screenX, screenY));
                
                // Delay between drag operations (except after the last drag)
                if (i < nodes.Count - 1)
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
        /// Performs a single mouse click at the current cursor position
        /// </summary>
        private void PerformMouseClick()
        {
            // Simulate a mouse click using Windows API
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(mouseDownDelay); // Short delay between down and up events
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        /// <summary>
        /// Performs a mouse drag operation from the current cursor position to the specified target coordinates
        /// </summary>
        /// <param name="targetX">Target X coordinate</param>
        /// <param name="targetY">Target Y coordinate</param>
        /// <returns>Task representing the async operation</returns>
        private async Task PerformMouseDrag(int targetX, int targetY)
        {
            // Get current cursor position
            POINT currentPoint;
            if (!GetCursorPos(out currentPoint))
            {
                throw new InvalidOperationException("Failed to get current cursor position");
            }
            
            int startX = currentPoint.X;
            int startY = currentPoint.Y;
            
            // Calculate distance between points
            double distance = Math.Sqrt(Math.Pow(targetX - startX, 2) + Math.Pow(targetY - startY, 2));
            
            // Determine number of steps based on distance (1 step per 10 pixels with a minimum of 5 steps and maximum of 30)
            int steps = Math.Max(5, Math.Min(30, (int)(distance / 10)));
            
            // Press mouse down at current position
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            
            // Small delay to ensure mouse down is registered
            await Task.Delay(mouseDownDelay);
            
            // Small random offset for human-like jitter
            Random random = new Random();
            
            // Move through intermediate points with slight randomization for realism
            for (int i = 1; i <= steps; i++)
            {
                // Calculate position along the path with slight randomization
                int interpolatedX = startX + (int)((targetX - startX) * (i / (double)steps));
                int interpolatedY = startY + (int)((targetY - startY) * (i / (double)steps));
                
                // Add slight jitter for more realistic movement (±2 pixels)
                if (i > 1 && i < steps) // Don't add jitter to the first or last positions
                {
                    interpolatedX += random.Next(-2, 3); // Random value between -2 and 2
                    interpolatedY += random.Next(-2, 3); // Random value between -2 and 2
                }
                
                // Move to the interpolated position
                MoveCursorTo(interpolatedX, interpolatedY);
                
                // Shorter delay between intermediate movements
                int stepDelay = i == steps ? movementDelay : Math.Max(2, movementDelay / 4);
                await Task.Delay(stepDelay);
            }
            
            // Ensure we end exactly at the target position
            MoveCursorTo(targetX, targetY);
            
            // Small delay before releasing the button
            await Task.Delay(mouseDownDelay);
            
            // Release mouse button at target position
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
        
        /// <summary>
        /// Helper structure for cursor position
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
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
