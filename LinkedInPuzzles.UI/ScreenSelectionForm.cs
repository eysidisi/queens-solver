namespace LinkedInPuzzles.UI
{
    /// <summary>
    /// Form used to select a region of the screen
    /// </summary>
    public class ScreenSelectionForm : Form
    {
        private Point startPoint;
        private Point endPoint;
        private bool isSelecting = false;
        private Rectangle selectedRegion;

        public Rectangle SelectedRegion => selectedRegion;

        public ScreenSelectionForm()
        {
            InitializeComponents();
            SetupEventHandlers();
        }

        private void InitializeComponents()
        {
            // Configure the form to be transparent and cover the entire screen
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.Cursor = Cursors.Cross;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Opacity = 0.5;
            this.DoubleBuffered = true;

            // Set initial values
            startPoint = Point.Empty;
            endPoint = Point.Empty;
            selectedRegion = Rectangle.Empty;

            // Display instructions
            Label instructions = new Label
            {
                Text = "Click and drag to select the chess board region. Press ESC to cancel.",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 50, 50),
                AutoSize = true,
                Padding = new Padding(10),
                Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold)
            };

            instructions.Location = new Point(
                (this.Width - instructions.Width) / 2,
                20
            );

            this.Controls.Add(instructions);
        }

        private void SetupEventHandlers()
        {
            // Set up event handlers
            this.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    startPoint = e.Location;
                    isSelecting = true;
                }
            };

            this.MouseMove += (s, e) =>
            {
                if (isSelecting)
                {
                    endPoint = e.Location;
                    this.Invalidate(); // Force a redraw
                }
            };

            this.MouseUp += (s, e) =>
            {
                if (isSelecting && e.Button == MouseButtons.Left)
                {
                    isSelecting = false;

                    // Calculate the rectangle coordinates
                    int x = Math.Min(startPoint.X, endPoint.X);
                    int y = Math.Min(startPoint.Y, endPoint.Y);
                    int width = Math.Abs(startPoint.X - endPoint.X);
                    int height = Math.Abs(startPoint.Y - endPoint.Y);

                    // Ensure minimum size
                    if (width > 10 && height > 10)
                    {
                        selectedRegion = new Rectangle(x, y, width, height);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
            };

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            };

            this.Paint += OnPaint;
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (isSelecting && !startPoint.IsEmpty && !endPoint.IsEmpty)
            {
                // Draw the selection rectangle
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    int x = Math.Min(startPoint.X, endPoint.X);
                    int y = Math.Min(startPoint.Y, endPoint.Y);
                    int width = Math.Abs(startPoint.X - endPoint.X);
                    int height = Math.Abs(startPoint.Y - endPoint.Y);

                    e.Graphics.DrawRectangle(pen, x, y, width, height);

                    // Fill with a semi-transparent color to highlight the selection
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(50, 100, 200, 255)))
                    {
                        e.Graphics.FillRectangle(brush, x, y, width, height);
                    }

                    // Calculate and display dimensions
                    string dimensions = $"{width} x {height}";
                    using (Font font = new Font(FontFamily.GenericSansSerif, 10))
                    using (SolidBrush brush = new SolidBrush(Color.White))
                    using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                    {
                        SizeF textSize = e.Graphics.MeasureString(dimensions, font);
                        Rectangle textRect = new Rectangle(
                            x + (width - (int)textSize.Width) / 2,
                            y + height + 5,
                            (int)textSize.Width + 10,
                            (int)textSize.Height + 6
                        );

                        e.Graphics.FillRectangle(bgBrush, textRect);
                        e.Graphics.DrawString(dimensions, font, brush, textRect.X + 5, textRect.Y + 3);
                    }
                }
            }
        }
    }
}