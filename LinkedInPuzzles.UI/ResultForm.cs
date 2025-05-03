using LinkedInPuzzles.Service;
using LinkedInPuzzles.Service.QueensProblem.Algorithm;
using LinkedInPuzzles.Service.ZipProblem;
using System.Drawing.Imaging;

namespace LinkedInPuzzles.UI
{
    public class ResultForm : Form
    {
        private readonly Bitmap resultImage;
        private readonly Queen[] queensSolution;
        private readonly List<ZipNode> zipSolution;
        private readonly Rectangle originalScreenRegion;
        private readonly Form parentForm;
        private readonly MouseAutomationService mouseAutomation;
        private readonly Rectangle boardBounds;
        private Panel buttonPanel;
        private CancellationTokenSource cancellationTokenSource;
        private Label statusLabel;

        public ResultForm(Bitmap resultImage, Queen[] queensSolution, Rectangle originalScreenRegion, Form parentForm, Rectangle detectedBoardBounds)
            : this(resultImage, queensSolution, null, originalScreenRegion, parentForm, detectedBoardBounds)
        {
        }

        public ResultForm(Bitmap resultImage, List<ZipNode> zipSolution, Rectangle originalScreenRegion, Form parentForm, Rectangle detectedBoardBounds)
            : this(resultImage, null, zipSolution, originalScreenRegion, parentForm, detectedBoardBounds)
        {
        }

        private ResultForm(Bitmap resultImage, Queen[] queensSolution, List<ZipNode> zipSolution, Rectangle originalScreenRegion, Form parentForm, Rectangle detectedBoardBounds)
        {
            this.resultImage = resultImage;
            this.queensSolution = queensSolution;
            this.zipSolution = zipSolution;
            this.originalScreenRegion = originalScreenRegion;
            this.parentForm = parentForm;
            this.mouseAutomation = new MouseAutomationService();
            this.cancellationTokenSource = new CancellationTokenSource();

            // Store the detected board bounds or estimate them
            this.boardBounds = detectedBoardBounds;

            InitializeComponents();
            
            // Add key event handler
            this.KeyPreview = true;
            this.KeyDown += ResultForm_KeyDown;
        }

        private void ResultForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                cancellationTokenSource.Cancel();
                statusLabel.Text = "Operation cancelled by user";
                statusLabel.BackColor = Color.LightPink;
            }
        }

        private void InitializeComponents()
        {
            // Set up the form
            this.Text = "Problem Solution";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // Create PictureBox to display the result
            PictureBox resultBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = resultImage
            };

            // Create panel for buttons
            buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60
            };

            // Create save button
            Button saveButton = new Button
            {
                Text = "Save Image",
                Width = 120,
                Height = 40,
                Location = new Point(10, 10)
            };

            // Add status label
            statusLabel = new Label
            {
                Text = "Ready to auto-solve",
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.LightGray
            };

            int buttonXPosition = 140;

            // Create click solution button for Queens problem if we have a solution
            if (queensSolution != null)
            {
                Button clickSolutionButton = new Button
                {
                    Text = "Auto-Click Queens Solution",
                    Width = 180,
                    Height = 40,
                    Location = new Point(buttonXPosition, 10),
                    BackColor = Color.LightGreen
                };

                // Configure button event handlers
                clickSolutionButton.Click += async (s, e) => await AutoClickQueensSolution(clickSolutionButton, saveButton, statusLabel);

                buttonPanel.Controls.Add(clickSolutionButton);
                buttonXPosition += 190;
            }

            // Create solve zip button if we have a zip solution
            if (zipSolution != null)
            {
                Button solveZipButton = new Button
                {
                    Text = "Auto-Solve Zip Solution",
                    Width = 180,
                    Height = 40,
                    Location = new Point(buttonXPosition, 10),
                    BackColor = Color.LightBlue
                };

                // Configure button event handlers
                solveZipButton.Click += async (s, e) => await AutoSolveZipSolution(solveZipButton, saveButton, statusLabel);

                buttonPanel.Controls.Add(solveZipButton);
                buttonXPosition += 190;
            }

            // Configure save button event handler
            saveButton.Click += (s, e) => SaveImage();

            // Add controls to the form
            buttonPanel.Controls.Add(saveButton);

            this.Controls.Add(resultBox);
            this.Controls.Add(buttonPanel);
            this.Controls.Add(statusLabel);
        }

        private void SaveImage()
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                saveDialog.Title = "Save Solution Image";
                saveDialog.FileName = "solution.png";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string extension = Path.GetExtension(saveDialog.FileName).ToLower();
                        ImageFormat format = extension == ".jpg" || extension == ".jpeg"
                            ? ImageFormat.Jpeg
                            : extension == ".bmp"
                                ? ImageFormat.Bmp
                                : ImageFormat.Png;

                        resultImage.Save(saveDialog.FileName, format);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving image: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async Task AutoClickQueensSolution(Button clickButton, Button saveButton, Label statusLabel)
        {
            if (originalScreenRegion.IsEmpty)
            {
                MessageBox.Show("No original screen region available. Please capture a screen region first.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Reset cancellation token
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Disable buttons during auto-clicking
            DisableButtons(buttonPanel);
            statusLabel.Text = "Auto-clicking Queens solution in progress... (Press ESC to cancel)";

            // Move form to background instead of minimizing
            this.TopMost = false;
            this.WindowState = FormWindowState.Normal;
            this.Location = new Point(-1000, -1000); // Move off screen
            parentForm.TopMost = false;
            parentForm.WindowState = FormWindowState.Normal;
            parentForm.Location = new Point(-1000, -1000);

            // Give time for forms to move
            await Task.Delay(100, token);

            try
            {
                // Pass the detected board bounds to the mouse automation service
                await mouseAutomation.ClickQueenPositions(
                    queensSolution,
                    originalScreenRegion,
                    delayBetweenClicks: null,
                    boardBounds: boardBounds,
                    cancellationToken: token);

                statusLabel.Text = "Auto-clicking completed successfully!";
                statusLabel.BackColor = Color.LightGreen;
            }
            catch (OperationCanceledException)
            {
                statusLabel.Text = "Auto-clicking cancelled by user";
                statusLabel.BackColor = Color.LightPink;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during auto-clicking: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Auto-clicking failed!";
                statusLabel.BackColor = Color.LightPink;
            }
            finally
            {
                // Restore windows
                this.Location = new Point(100, 100); // Move back on screen
                parentForm.Location = new Point(100, 100);

                // Re-enable buttons
                EnableButtons(buttonPanel);
            }
        }

        private async Task AutoSolveZipSolution(Button solveButton, Button saveButton, Label statusLabel)
        {
            if (originalScreenRegion.IsEmpty)
            {
                MessageBox.Show("No original screen region available. Please capture a screen region first.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Reset cancellation token
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Disable buttons during auto-solving
            DisableButtons(buttonPanel);
            statusLabel.Text = "Auto-solving Zip solution in progress... (Press ESC to cancel)";

            // Move form to background instead of minimizing
            this.TopMost = false;
            this.WindowState = FormWindowState.Normal;
            this.Location = new Point(-1000, -1000); // Move off screen
            parentForm.TopMost = false;
            parentForm.WindowState = FormWindowState.Normal;
            parentForm.Location = new Point(-1000, -1000);

            // Give time for forms to move
            await Task.Delay(100, token);

            try
            {
                // Pass the detected board bounds to the mouse automation service
                await mouseAutomation.DragBetweenZipNodes(
                    zipSolution,
                    originalScreenRegion,
                    delayBetweenDrags: 1,
                    boardBounds: boardBounds,
                    cancellationToken: token);

                statusLabel.Text = "Auto-solving Zip completed successfully!";
                statusLabel.BackColor = Color.LightGreen;
            }
            catch (OperationCanceledException)
            {
                statusLabel.Text = "Auto-solving cancelled by user";
                statusLabel.BackColor = Color.LightPink;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during auto-solving Zip: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Auto-solving Zip failed!";
                statusLabel.BackColor = Color.LightPink;
            }
            finally
            {
                // Restore windows
                this.Location = new Point(100, 100); // Move back on screen
                parentForm.Location = new Point(100, 100);

                // Re-enable buttons
                EnableButtons(buttonPanel);
            }
        }

        private void DisableButtons(Panel panel)
        {
            foreach (Control control in panel.Controls)
            {
                if (control is Button button)
                {
                    button.Enabled = false;
                }
            }
        }

        private void EnableButtons(Panel panel)
        {
            foreach (Control control in panel.Controls)
            {
                if (control is Button button)
                {
                    button.Enabled = true;
                }
            }
        }
    }
}