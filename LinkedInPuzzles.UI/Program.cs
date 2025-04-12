namespace LinkedInPuzzles.UI
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            // Set up the application
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Start the main form
            Application.Run(new MainForm());
        }
    }
}
