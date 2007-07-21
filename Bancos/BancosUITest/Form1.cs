using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SerializadorTexto;
using Dom = BancosDF.Domiciliaciones;
using Dev = BancosDF.Devoluciones;
using BancosTest.Properties;
using System.IO;

namespace BancosTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        readonly string rutachunga = @"c:\blabla.txt";

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Settings.Default.Domiciliaciones;
            ofd.Multiselect = true;

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                foreach (string file in ofd.FileNames)
                {
                    Dom.Fichero fichero = Serializador.AbrirArchivo<Dom.Fichero>(file);
                    Serializador.GuardarArchivo(fichero, rutachunga);

                    ComprobarIguales(file, rutachunga); 
                }
            }

        }

        private void ComprobarIguales(string file, string rutachunga)
        {
            int linea=0; 
            using (StreamReader sr1 = File.OpenText(file))
            using (StreamReader sr2 = File.OpenText(rutachunga))
            {
                while (true)
                {
                    string s1 = sr1.ReadLine();
                    string s2 = sr2.ReadLine();

                    if (s1 != s2)
                        throw new InvalidOperationException("Error en la linea " + linea + ":\r\n" + s1 + "\r\n" + s2 + "\r\n");

                    if (s1 == null) break;

                    linea++;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Settings.Default.Devoluciones;
            ofd.Multiselect = true;

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                foreach (string file in ofd.FileNames)
                {
                    Dev.Fichero fichero = Serializador.AbrirArchivo<Dev.Fichero>(file);
                    Serializador.GuardarArchivo(fichero, rutachunga);

                    ComprobarIguales(file, rutachunga);
                }
            }

        }
    }
}
