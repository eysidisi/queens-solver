using QueensProblem.Service;
using QueensProblem.Service.QueensProblem.Algorithm;
using QueensProblem.Service.ZipProblem;
using System;
using System.Collections.Generic;
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
        private readonly List<ZipNode> zipSolution;
        private readonly Rectangle originalScreenRegion;
        private readonly Form parentForm;
        private readonly MouseAutomationService mouseAutomation;
        private readonly Rectangle boardBounds;
        private Panel buttonPanel;

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
            
            // Store the detected board bounds or estimate them
            this.boardBounds = detectedBoardBounds;

            InitializeComponents();
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
            Label statusLabel = new Label
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

            // Disable buttons during auto-clicking
            DisableButtons(buttonPanel);
            statusLabel.Text = "Auto-clicking Queens solution in progress...";
            
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

            // Disable buttons during auto-solving
            DisableButtons(buttonPanel);
            statusLabel.Text = "Auto-solving Zip solution in progress...";
            
            // Hide this form and the parent form
            this.WindowState = FormWindowState.Minimized;
            parentForm.WindowState = FormWindowState.Minimized;
            
            // Give time for forms to minimize
            await Task.Delay(100);
            
            try
            {
                // Pass the detected board bounds to the mouse automation service
                await mouseAutomation.DragBetweenZipNodes(
                    zipSolution, 
                    originalScreenRegion,
                    delayBetweenDrags: 1,
                    boardBounds: boardBounds);
                
                statusLabel.Text = "Auto-solving Zip completed successfully!";
                statusLabel.BackColor = Color.LightGreen;
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
                this.WindowState = FormWindowState.Normal;
                parentForm.WindowState = FormWindowState.Normal;
                
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