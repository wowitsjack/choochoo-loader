using System;
using System.Threading;
using System.Windows.Forms;
using ChooChooEngine.App.Forms;

namespace ChooChooEngine.App
{
    static class Program
    {
        private static Mutex _mutex = null;
        private const string MutexName = "ChooChooEngineInjectorSingleInstance";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // Another instance is already running
                MessageBox.Show("ChooChoo Injection Engine is already running!", 
                    "Already Running", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(args));
            }
            finally
            {
                // Release the mutex
                _mutex.ReleaseMutex();
            }
        }
    }
} 