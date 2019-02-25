// Original from: http://stackoverflow.com/a/3124252/122195
// Original code from https://github.com/thepirat000/Snipping-Ocr

namespace StudioAT.Utilitites.SnippingOCR
{
    using System;
    using System.Windows.Forms;

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
            Application.Run(new MainForm());
        }

    }
}
