using Emgu.CV;
using QueensProblem.Service.ImageProcessing;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QueensProblem.UI
{
    public class MainForm : Form
    {
        private Button captureButton;
        private Button browseButton;
        private PictureBox previewBox;
        private Button solveButton;
        private Label statusLabel;
        private Rectangle originalScreenRegion;
        private Bitmap capturedImage;
        private bool debugEnabled = true;
        private readonly ScreenCaptureService screenCaptureService;
        private readonly ImageProcessingService imageProcessingService;

        public MainForm()
        {
            screenCaptureService = new ScreenCaptureService();
            imageProcessingService = new ImageProcessingService(debugEnabled);
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Set up the form
            this.Text = "Queens Problem Solver";
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

            // Create bottom panel for solve button
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                Padding = new Padding(10)
            };

            // Create solve button
            solveButton = new Button
            {
                Text = "Solve Queens Problem",
                Width = 200,
                Height = 50,
                Location = new Point(this.ClientSize.Width / 2 - 100, 25),
                Enabled = false,
                BackColor = Color.Gold,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold)
            };
            solveButton.Click += SolveButton_Click;

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
            bottomPanel.Controls.Add(solveButton);
            
            this.Controls.Add(previewPanel);
            this.Controls.Add(topPanel);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(statusLabel);
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
                        
                        // Capture the selected region
                        capturedImage = screenCaptureService.CaptureScreenRegion(originalScreenRegion);
                        
                        if (capturedImage != null)
                        {
                            // Show the captured image in the preview
                            previewBox.Image = capturedImage;
                            solveButton.Enabled = true;
                            statusLabel.Text = $"Captured region: {capturedImage.Width}x{capturedImage.Height} pixels";
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
                openFileDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp";
                openFileDialog.Title = "Select an Image File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Load the selected image
                        capturedImage = new Bitmap(openFileDialog.FileName);
                        
                        // Show the loaded image in the preview
                        previewBox.Image = capturedImage;
                        solveButton.Enabled = true;
                        statusLabel.Text = $"Loaded image: {capturedImage.Width}x{capturedImage.Height} pixels";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading image: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void SolveButton_Click(object sender, EventArgs e)
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
            solveButton.Enabled = false;
            statusLabel.Text = "Processing image and solving Queens Problem...";

            try
            {
                // Process the image in a background task
                var (resultImage, queens, boardBounds) = await Task.Run(() => 
                    imageProcessingService.ProcessAndSolveQueensProblem(capturedImage));
                
                if (resultImage != null)
                {
                    // Show results in a new form, passing the detected board boundaries
                    var resultForm = new ResultForm(resultImage, queens, originalScreenRegion, this, boardBounds);
                    resultForm.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing image: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable buttons
                captureButton.Enabled = true;
                browseButton.Enabled = true;
                solveButton.Enabled = true;
                statusLabel.Text = "Ready - Please capture a screen region or open an image file";
            }
        }
    }
} 