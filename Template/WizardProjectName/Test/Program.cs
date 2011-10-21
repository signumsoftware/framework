using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WizardProjectName;

namespace Test
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
            using (TypeDialog td = new TypeDialog() { FileImputName = "Product" })
            {
                if (td.ShowDialog() == DialogResult.OK)
                {
                    Console.Write(td.WPFEntityControl);
                }
            }
        }
    }
}
