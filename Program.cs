using System;
using System.Windows.Forms;

namespace BlueStacks4FPSTweaker
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                System.IO.File.WriteAllText("crash.log", e.ExceptionObject.ToString());
            };
            Application.ThreadException += (s, e) => {
                System.IO.File.WriteAllText("crash.log", e.Exception.ToString());
            };
            try
            {
                System.IO.File.AppendAllText("debug.log", DateTime.Now + " - App Starting...\n");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                FigmaVioletForm f = new FigmaVioletForm();
                f.FormClosing += (s, e) => {
                    System.IO.File.AppendAllText("debug.log", DateTime.Now + " - FormClosing: " + e.CloseReason + "\n");
                };
                System.IO.File.AppendAllText("debug.log", DateTime.Now + " - Form initialized. Running Application.Run...\n");
                Application.Run(f);
                System.IO.File.AppendAllText("debug.log", DateTime.Now + " - Application.Run exited normally.\n");
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("crash.log", ex.ToString());
                MessageBox.Show(ex.ToString(), "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
