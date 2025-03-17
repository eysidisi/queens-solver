using QueensProblem.Service;
using QueensProblem.Service.Algorithm;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QueensProblem.UI
{
    public class ResultForm : Form
    {
        private readonly Bitmap resultImage;
        private readonly Queen[] queensSolution;
        private readonly Rectangle originalScreenRegion;
        private readonly Form parentForm;
        private readonly MouseAutomationService mouseAutomation;
        private readonly Rectangle boardBounds;

        public ResultForm(Bitmap resultImage, Queen[] queensSolution, Rectangle originalScreenRegion, Form parentForm)
            : this(resultImage, queensSolution, originalScreenRegion, parentForm, null)
        {
        }

        public ResultForm(Bitmap resultImage, Queen[] queensSolution, Rectangle originalScreenRegion, Form parentForm, Rectangle? detectedBoardBounds)
        {
            this.resultImage = resultImage;
            this.queensSolution = queensSolution;
            this.originalScreenRegion = originalScreenRegion;
            this.parentForm = parentForm;
            this.mouseAutomation = new MouseAutomationService();
            
            // Store the detected board bounds or estimate them
            this.boardBounds = detectedBoardBounds ?? EstimateBoardBounds();

            InitializeComponents();
        }

        private Rectangle EstimateBoardBounds()
        {
            // Apply a basic heuristic if no board detection information is available
            // Assume a 5% margin around the board
            int margin = Math.Min(resultImage.Width, resultImage.Height) / 20;
            
            return new Rectangle(
                margin, 
                margin, 
                resultImage.Width - (margin * 2), 
                resultImage.Height - (margin * 2)
            );
        }

        private void InitializeComponents()
        {
            // Set up the form
            this.Text = "Queens Problem Solution";
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
            Panel buttonPanel = new Panel
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

            // Create click solution button
            Button clickSolutionButton = new Button
            {
                Text = "Auto-Click Solution",
                Width = 150,
                Height = 40,
                Location = new Point(140, 10),
                BackColor = Color.LightGreen
            };

            // Add status label
            Label statusLabel = new Label
            {
                Text = "Ready to auto-click solution",
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.LightGray
            };

            // Configure button event handlers
            saveButton.Click += (s, e) => SaveImage();
            clickSolutionButton.Click += async (s, e) => await AutoClickSolution(clickSolutionButton, saveButton, statusLabel);

            // Add controls to the form
            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(clickSolutionButton);
            
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
                saveDialog.FileName = "queens_solution.png";

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

        private async Task AutoClickSolution(Button clickButton, Button saveButton, Label statusLabel)
        {
            if (originalScreenRegion.IsEmpty)
            {
                MessageBox.Show("No original screen region available. Please capture a screen region first.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Disable buttons during auto-clicking
            clickButton.Enabled = false;
            saveButton.Enabled = false;
            statusLabel.Text = "Auto-clicking solution in progress...";
            
            // Hide this form and the parent form
            this.WindowState = FormWindowState.Minimized;
            parentForm.WindowState = FormWindowState.Minimized;
            
            // Give time for forms to minimize
            await Task.Delay(100);
            
            try
            {
                // Pass the detected board bounds to the mouse automation service
                await mouseAutomation.ClickQueenPositions(
                    queensSolution, 
                    originalScreenRegion,
                    delayBetweenClicks: null,
                    boardBounds: boardBounds);
                
                statusLabel.Text = "Auto-clicking completed successfully!";
                statusLabel.BackColor = Color.LightGreen;
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
                this.WindowState = FormWindowState.Normal;
                parentForm.WindowState = FormWindowState.Normal;
                
                // Re-enable buttons
                clickButton.Enabled = true;
                saveButton.Enabled = true;
            }
        }
    }
} 