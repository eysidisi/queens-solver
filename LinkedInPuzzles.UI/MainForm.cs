using LinkedInPuzzles.Service;
using LinkedInPuzzles.Service.QueensProblem.ImageProcessing;
using LinkedInPuzzles.Service.ZipProblem.ImageProcessing;
using LinkedInPuzzles.Service.ZipSolver.ImageProcessing;

namespace LinkedInPuzzles.UI
{
    public class MainForm : Form
    {
        private Button captureButton;
        private Button browseButton;
        private PictureBox previewBox;
        private Button solveQueensButton;
        private Button solveZipButton;
        private Label statusLabel;
        private Rectangle originalScreenRegion;
        private Bitmap capturedImage;
        private bool debugEnabled = true;
        private readonly ScreenCaptureService screenCaptureService;
        private readonly QueensImageProcessingService queensImageProcessingService;
        private readonly ZipImageProcessingService zipImageProcessingService;
        private enum ProblemType { Queens, Zip }
        private CheckBox highQualityCheckbox;

        public MainForm()
        {
            screenCaptureService = new ScreenCaptureService();
            queensImageProcessingService = new QueensImageProcessingService(debugEnabled);

            // Initialize services for Zip problem
            var debugHelper = new DebugHelper(debugEnabled);

            // Initialize required components for ZipBoardProcessor
            string tessdataPath = Path.Combine(
                Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName,
                "LinkedInPuzzles.Service", "ZipProblem", "Resources", "tessdata");

            // Fallback to common locations if the path doesn't exist
            if (!Directory.Exists(tessdataPath) || !File.Exists(Path.Combine(tessdataPath, "eng.traineddata")))
            {
                string[] possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "tessdata"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
                    Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName, "Resources", "tessdata")
                };

                foreach (var path in possiblePaths)
                {
                    if (Directory.Exists(path) && File.Exists(Path.Combine(path, "eng.traineddata")))
                    {
                        tessdataPath = path;
                        break;
                    }
                }
            }

            // Log the path being used
            Console.WriteLine($"Using tessdata path: {tessdataPath}");
            if (!File.Exists(Path.Combine(tessdataPath, "eng.traineddata")))
            {
                MessageBox.Show(
                    $"Tessdata file not found at {tessdataPath}. Please ensure eng.traineddata is in the correct location.",
                    "Missing Tessdata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            var digitRecognizer = new DigitRecognizer(debugHelper, tessdataPath);
            var circleDetector = new CircleDetector(debugHelper);
            var connectivityDetector = new ConnectivityDetector(debugHelper);

            var zipBoardProcessor = new ZipBoardProcessor(
                debugHelper,
                digitRecognizer,
                circleDetector,
                connectivityDetector);

            var boardDetector = new BoardDetector(debugHelper);
            zipImageProcessingService = new ZipImageProcessingService(debugHelper, zipBoardProcessor, boardDetector);

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Set up the form
            this.Text = "Puzzle Solver";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Icon = SystemIcons.Application;

            // Create top panel for buttons
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10)
            };

            // Create capture button
            captureButton = new Button
            {
                Text = "Capture Screen Region",
                Width = 150,
                Height = 40,
                Location = new Point(20, 10),
                BackColor = Color.LightBlue
            };
            captureButton.Click += CaptureButton_Click;

            // Create browse button
            browseButton = new Button
            {
                Text = "Open Image File",
                Width = 150,
                Height = 40,
                Location = new Point(180, 10),
                BackColor = Color.LightGreen
            };
            browseButton.Click += BrowseButton_Click;

            // Create high quality checkbox
            highQualityCheckbox = new CheckBox
            {
                Text = "High Quality Capture",
                Location = new Point(340, 20),
                AutoSize = true,
                Checked = true
            };
            topPanel.Controls.Add(highQualityCheckbox);

            // Create preview panel
            Panel previewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Create preview box
            previewBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Create bottom panel for solve buttons
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                Padding = new Padding(10)
            };

            // Create Queens Problem solve button
            solveQueensButton = new Button
            {
                Text = "Solve Queens Problem",
                Width = 200,
                Height = 50,
                Location = new Point(this.ClientSize.Width / 4 - 100, 25),
                Enabled = false,
                BackColor = Color.Gold,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold)
            };
            solveQueensButton.Click += SolveQueensButton_Click;

            // Create Zip Problem solve button
            solveZipButton = new Button
            {
                Text = "Solve Zip Problem",
                Width = 200,
                Height = 50,
                Location = new Point(this.ClientSize.Width * 3 / 4 - 100, 25),
                Enabled = false,
                BackColor = Color.LightSalmon,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold)
            };
            solveZipButton.Click += SolveZipButton_Click;

            // Create status label
            statusLabel = new Label
            {
                Text = "Ready - Please capture a screen region or open an image file",
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.LightGray
            };

            // Add controls to the form
            topPanel.Controls.Add(captureButton);
            topPanel.Controls.Add(browseButton);
            previewPanel.Controls.Add(previewBox);
            bottomPanel.Controls.Add(solveQueensButton);
            bottomPanel.Controls.Add(solveZipButton);

            this.Controls.Add(previewPanel);
            this.Controls.Add(topPanel);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(statusLabel);

            // Adjust positions after the form is loaded
            this.Load += (sender, e) =>
            {
                // Re-center buttons based on actual width
                solveQueensButton.Location = new Point(this.ClientSize.Width / 4 - solveQueensButton.Width / 2, 25);
                solveZipButton.Location = new Point(this.ClientSize.Width * 3 / 4 - solveZipButton.Width / 2, 25);
            };

            // Adjust positions when form is resized
            this.Resize += (sender, e) =>
            {
                solveQueensButton.Location = new Point(this.ClientSize.Width / 4 - solveQueensButton.Width / 2, 25);
                solveZipButton.Location = new Point(this.ClientSize.Width * 3 / 4 - solveZipButton.Width / 2, 25);
            };
        }

        private void CaptureButton_Click(object sender, EventArgs e)
        {
            // First hide this form
            this.WindowState = FormWindowState.Minimized;
            Thread.Sleep(500); // Give time for the form to minimize

            try
            {
                // Create and show the screen selection form
                using (var selectionForm = new ScreenSelectionForm())
                {
                    // Wait for the screen selection form to close
                    selectionForm.ShowDialog();

                    // Check if the selection was completed
                    if (selectionForm.DialogResult == DialogResult.OK &&
                        selectionForm.SelectedRegion.Width > 0 &&
                        selectionForm.SelectedRegion.Height > 0)
                    {
                        // Store the original screen region for automated clicking later
                        originalScreenRegion = selectionForm.SelectedRegion;

                        // Capture the selected region with appropriate quality
                        if (highQualityCheckbox.Checked)
                        {
                            capturedImage = screenCaptureService.CaptureScreenRegionHighQuality(originalScreenRegion);
                        }
                        else
                        {
                            capturedImage = screenCaptureService.CaptureScreenRegion(originalScreenRegion);
                        }

                        if (capturedImage != null)
                        {
                            // Get resolution information
                            var resInfo = screenCaptureService.GetResolutionInfo(capturedImage);

                            // Show the captured image in the preview
                            previewBox.Image = capturedImage;
                            solveQueensButton.Enabled = true;
                            solveZipButton.Enabled = true;

                            // Display resolution info in status
                            statusLabel.Text = $"Captured region: {resInfo.Width}x{resInfo.Height} pixels, " +
                                            $"{resInfo.HorizontalDpi:F0} DPI, " +
                                            $"{resInfo.PhysicalWidthInches:F1}\"x{resInfo.PhysicalHeightInches:F1}\"" +
                                            (highQualityCheckbox.Checked ? " (High Quality)" : "");

                        }
                        else
                        {
                            MessageBox.Show("Failed to capture the selected region.", "Capture Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        statusLabel.Text = "Screen capture cancelled or invalid region selected.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during screen capture: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Restore the main form
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*";
                openFileDialog.Title = "Select an Image File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Load the selected image with appropriate quality
                        if (highQualityCheckbox.Checked)
                        {
                            capturedImage = screenCaptureService.LoadHighQualityImage(openFileDialog.FileName);
                        }
                        else
                        {
                            capturedImage = new Bitmap(openFileDialog.FileName);
                        }

                        // Get resolution information
                        var resInfo = screenCaptureService.GetResolutionInfo(capturedImage);

                        // Show the loaded image in the preview
                        previewBox.Image = capturedImage;
                        solveQueensButton.Enabled = true;
                        solveZipButton.Enabled = true;

                        // Display resolution info in status
                        statusLabel.Text = $"Loaded image: {resInfo.Width}x{resInfo.Height} pixels, " +
                                       $"{resInfo.HorizontalDpi:F0} DPI, " +
                                       $"{resInfo.PhysicalWidthInches:F1}\"x{resInfo.PhysicalHeightInches:F1}\"" +
                                       (highQualityCheckbox.Checked ? " (High Quality)" : "");

                        // Suggest resolution adjustment if too high
                        if (resInfo.Width > 1000 || resInfo.Height > 1000)
                        {
                            if (MessageBox.Show(
                                $"The loaded image is large ({resInfo.Width}x{resInfo.Height} pixels). " +
                                "Would you like to resize it to improve processing speed?",
                                "Large Image Loaded",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                // Create a temporary copy of the original region
                                Rectangle originalRegion = new Rectangle(0, 0, capturedImage.Width, capturedImage.Height);

                                // Calculate a more appropriate size (max 800 pixels on longest dimension)
                                int targetWidth = 0, targetHeight = 0;
                                if (resInfo.Width > resInfo.Height)
                                {
                                    targetWidth = 800;
                                }
                                else
                                {
                                    targetHeight = 800;
                                }

                                // Create a new bitmap with the target resolution
                                Bitmap resized = new Bitmap(
                                    targetWidth > 0 ? targetWidth : (int)(capturedImage.Width * (targetHeight / (float)capturedImage.Height)),
                                    targetHeight > 0 ? targetHeight : (int)(capturedImage.Height * (targetWidth / (float)capturedImage.Width))
                                );

                                // Create graphics object for the new bitmap
                                using (Graphics g = Graphics.FromImage(resized))
                                {
                                    // Set high quality rendering
                                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                                    // Draw the original image resized
                                    g.DrawImage(capturedImage, 0, 0, resized.Width, resized.Height);
                                }

                                // Dispose the old image and update
                                capturedImage.Dispose();
                                capturedImage = resized;
                                previewBox.Image = capturedImage;

                                // Update status with new resolution
                                var newResInfo = screenCaptureService.GetResolutionInfo(capturedImage);
                                statusLabel.Text = $"Resized image: {newResInfo.Width}x{newResInfo.Height} pixels, " +
                                               $"{newResInfo.HorizontalDpi:F0} DPI";
                            }
                        }
                        else if (resInfo.Width < 300 || resInfo.Height < 300)
                        {
                            MessageBox.Show(
                                $"The loaded image is small ({resInfo.Width}x{resInfo.Height} pixels). " +
                                "This may affect the accuracy of puzzle recognition.",
                                "Small Image Loaded",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading image: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void SolveQueensButton_Click(object sender, EventArgs e)
        {
            await SolveProblem(ProblemType.Queens);
        }

        private async void SolveZipButton_Click(object sender, EventArgs e)
        {
            await SolveProblem(ProblemType.Zip);
        }

        private async Task SolveProblem(ProblemType problemType)
        {
            if (capturedImage == null)
            {
                MessageBox.Show("Please capture a screen region or open an image file first.",
                    "No Image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Disable buttons while processing
            captureButton.Enabled = false;
            browseButton.Enabled = false;
            solveQueensButton.Enabled = false;
            solveZipButton.Enabled = false;

            string problemName = problemType == ProblemType.Queens ? "Queens Problem" : "Zip Problem";
            statusLabel.Text = $"Processing image and solving {problemName}...";

            try
            {
                if (problemType == ProblemType.Queens)
                {
                    // Process the Queens Problem image in a background task
                    var (resultImage, queens, boardBounds) = await Task.Run(() =>
                        queensImageProcessingService.ProcessAndSolveQueensProblem(capturedImage));

                    if (resultImage != null)
                    {
                        // Show results in a new form, passing the detected board boundaries
                        var resultForm = new ResultForm(resultImage, queens, originalScreenRegion, this, boardBounds);
                        resultForm.Show();
                    }
                }
                else // Zip Problem
                {
                    // Process the Zip Problem image in a background task
                    var (resultImage, solution, board, boardBounds) = await Task.Run(() =>
                        zipImageProcessingService.ProcessAndSolveZipPuzzle(capturedImage));

                    if (resultImage != null)
                    {
                        // Show results in a new form with the detected board bounds
                        var resultForm = new ResultForm(resultImage, solution, originalScreenRegion, this, boardBounds);
                        resultForm.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing {problemName}: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable buttons
                captureButton.Enabled = true;
                browseButton.Enabled = true;
                solveQueensButton.Enabled = capturedImage != null;
                solveZipButton.Enabled = capturedImage != null;
                statusLabel.Text = "Ready";
            }
        }
    }
}