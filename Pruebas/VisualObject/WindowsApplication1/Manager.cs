using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace VisualObject
{
    public partial class Manager : Form
    {
        string dllPath = null; 

        public Manager()
        {
            //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.Load(dllPath + args.Name.Split(',')[0] + ".dll"); 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
                dllPath = fbd.SelectedPath; 
        }

        private void Manager_Load(object sender, EventArgs e)
        {

        }
    }
}