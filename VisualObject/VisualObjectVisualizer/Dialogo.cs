using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace VisualObjectVisualizer
{
    public partial class Dialogo : Form
    {
        public Dialogo()
        {
            InitializeComponent();
        }

        public Stream MyStream; 

        private void Form1_Load(object sender, EventArgs e)
        {
            label1.Text = "Length:  " + MyStream.Length; 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (FileStream file = File.OpenWrite(textBox1.Text))
            {
                Copy(MyStream, file); 
            }
            this.Close(); 
        }

        void Copy(Stream reader, Stream writer)
        {
            byte[] buffer = new byte[32768];
            int read; 
            while((read = reader.Read(buffer, 0, buffer.Length))!=0)
            {
                writer.Write(buffer, 0, read);
            }
        }

        private void textBox1_DoubleClick(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
                textBox1.Text = sfd.FileName; 
        }
    }
}