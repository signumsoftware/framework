using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace VisualObject
{
    public partial class Manager : Form
    {


        public Manager()
        {
            InitializeComponent();
            
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName an = new AssemblyName(args.Name);
            string name = string.Format(@"{0}\{1}.dll", lbDirectory.Text,an.Name ); 
            Assembly ase =  Assembly.LoadFile(name);
            return ase; 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
                lbDirectory.Text = fbd.SelectedPath; 
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                lbFile.Text = ofd.FileName;
                lbDirectory.Text = Path.GetDirectoryName(ofd.FileName); 

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            BinaryFormatter bf = new BinaryFormatter();
            object obj = bf.Deserialize(File.OpenRead(lbFile.Text));
            VisualObjectFrm vf = new VisualObjectFrm();
            vf.CurrentObject = obj;
            vf.ShowDialog();

        }

  
    }
}