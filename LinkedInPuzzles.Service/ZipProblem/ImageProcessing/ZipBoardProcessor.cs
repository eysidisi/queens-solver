using Emgu.CV;
using System.Drawing;

namespace LinkedInPuzzles.Service.ZipProblem.ImageProcessing
{
    public class ZipBoardProcessor
    {
        private readonly DebugHelper _debugHelper;
        private readonly ConnectivityDetector _connectivityDetector;
        private readonly CircleDetector _circleDetector;
        private readonly DigitRecognizer _digitRecognizer;

        public ZipBoardProcessor(
            DebugHelper debugHelper,
            DigitRecognizer digitRecognizer,
            CircleDetector circleDetector,
            ConnectivityDetector connectivityDetector)
        {
            _debugHelper = debugHelper;
            _digitRecognizer = digitRecognizer;
            _circleDetector = circleDetector;
            _connectivityDetector = connectivityDetector;
        }


        public ZipBoard ProcessImage(Bitmap boardImage, int numberOfCells)
        {
            // Convert Bitmap to Mat for internal processing
            Mat colorImage = boardImage.ToMat();
            _debugHelper.SaveDebugImage(colorImage, "input_board");

            // Create and analyze the ZipBoard
            var zipBoard = AnalyzeProcessedBoard(colorImage, numberOfCells);
            // Add a senity check to ensure the board is valid. Numbers should be unique and in the range 1-9 and theyere can't be any missing numbers.
            if (!zipBoard.IsValid())
            {
                throw new Exception("Invalid board detected. Please ensure all numbers are unique and in the range 1-9 with no missing numbers.");
            }

            // Detect walls between cells and update connectivity
            _connectivityDetector.DetectWallsAndSetupConnectivity(colorImage, zipBoard, numberOfCells);

            return zipBoard;
        }

        public ZipBoard ProcessImage(Mat colorImage, int numberOfCells)
        {
            _debugHelper.SaveDebugImage(colorImage, "input_board");

            // Create and analyze the ZipBoard
            var zipBoard = AnalyzeProcessedBoard(colorImage, numberOfCells);
            // Add a senity check to ensure the board is valid. Numbers should be unique and in the range 1-9 and theyere can't be any missing numbers.
            if (!zipBoard.IsValid())
            {
                throw new Exception("Invalid board detected. Please ensure all numbers are unique and in the range 1-9 with no missing numbers.");
            }

            // Detect walls between cells and update connectivity
            _connectivityDetector.DetectWallsAndSetupConnectivity(colorImage, zipBoard, numberOfCells);

            return zipBoard;
        }

        private ZipBoard AnalyzeProcessedBoard(Mat processedBoard, int numberOfCells)
        {
            var zipBoard = new ZipBoard(numberOfCells, numberOfCells);

            // Calculate cell dimensions
            int cellHeight = processedBoard.Height / numberOfCells;
            int cellWidth = processedBoard.Width / numberOfCells;

            int widthPadding = (int)(cellWidth * 0.1);
            int heightPadding = (int)(cellHeight * 0.1);

            // Process each cell
            for (int row = 0; row < numberOfCells; row++)
            {
                for (int col = 0; col < numberOfCells; col++)
                {
                    // Apply padding inward (making cell smaller)
                    int x = col * cellWidth + widthPadding;
                    int y = row * cellHeight + heightPadding;
                    int width = Math.Max(1, cellWidth - widthPadding * 2);   // Ensure at least 1 pixel width
                    int height = Math.Max(1, cellHeight - heightPadding * 2); // Ensure at least 1 pixel height

                    Rectangle cellRect = new Rectangle(x, y, width, height);

                    using (Mat cellMat = new Mat(processedBoard, cellRect))
                    {
                        int cellNumber = DetectNumberInCell(cellMat, row, col);
                        zipBoard.SetNodeOrder(row, col, cellNumber);
                    }
                }
            }

            // Set up default connectivity
            zipBoard.ResetAndSetupNeighbors((node1, node2) => true);

            return zipBoard;
        }

        private int DetectNumberInCell(Mat cellMat, int row, int col)
        {
            try
            {
                (var circleExist, var extractedContent) = _circleDetector.FindAndExtractCircle(cellMat);
                if (!circleExist)
                {
                    return 0;
                }
                return _digitRecognizer.RecognizeDigit(extractedContent, row, col);

            }
            catch (Exception ex)
            {
                _debugHelper.LogDebugMessage($"Error detecting number: {ex.Message}");
            }

            return 0; // Return 0 if no number is detected or on error
        }

    }
}

