using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsClipboardManager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Initialize the form but don't run it yet
            Form1 form = new Form1();

            // Hide the form immediately after initialization
            form.Hide();

            // Run the application with the form hidden, and show it via the system tray later
            Application.Run();
        }
    }
}
