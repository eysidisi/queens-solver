using System;
using System.Windows.Forms;

namespace QueensProblem.UI
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
