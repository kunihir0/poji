using System;
using System.Windows.Forms;

namespace poji
{
    static class Program
    {
        // Make this static and public so it can be accessed from MainForm
        public static bool UseDirectXRendering = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Ask user about experimental DirectX mode
            UseDirectXRendering = ShowDirectXOptionDialog();
            
            if (UseDirectXRendering)
            {
                Console.WriteLine("DirectX rendering enabled");
                try
                {
                    Application.Run(new DirectXOverlayForm());
                }
                catch (Exception ex)
                {
                    string errorMessage = $"DirectX rendering failed: {ex.Message}";
                    if (ex.InnerException != null)
                    {
                        errorMessage += $"\nInner exception: {ex.InnerException.Message}";
                    }
                    
                    MessageBox.Show(errorMessage, "DirectX Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine(errorMessage);
                    
                    // Fall back to legacy rendering
                    Console.WriteLine("Falling back to legacy rendering");
                    Application.Run(new MainForm());
                }
            }
            else
            {
                Console.WriteLine("Legacy rendering enabled");
                Application.Run(new MainForm());
            }
        }

        /// <summary>
        /// Shows a dialog asking the user if they want to use experimental DirectX rendering
        /// </summary>
        /// <returns>True if user selected DirectX rendering, false otherwise</returns>
        private static bool ShowDirectXOptionDialog()
        {
            DialogResult result = MessageBox.Show(
                "Would you like to use experimental DirectX rendering?\n\n" +
                "This feature is currently under development and may improve performance.",
                "Experimental Feature",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            
            return result == DialogResult.Yes;
        }
    }
}