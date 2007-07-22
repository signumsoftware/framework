using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Framework.GestorSalida.DN;
using MotorIU.Motor;

namespace Framework.GestorSalida.ClienteIU
{

    public partial class frmEnviarDocumentoSalida : MotorIU.FormulariosP.FormularioBase
    {
        public frmEnviarDocumentoSalida()
        {
            InitializeComponent();
        }

        public override void Inicializar()
        {
            base.Inicializar();

            CanalSalida[] canales = (CanalSalida[])Enum.GetValues(typeof(CanalSalida));
            for (int i = 0; i < canales.Length; i++)
            {
                //quitamos todos los canales menos el de impresión
                if (canales[i] == CanalSalida.impresora)
                    this.cboCanalSalida.Items.Add(canales[i]);
            }
            this.cboCanalSalida.SelectedIndex = 0;

            this.lstArchivosAdjuntos.DisplayMember = "Name";

            if (this.Paquete != null && Paquete.Contains("Paquete"))
            {
                PaqueteEnvioDocumentoSalida miPaquete = (PaqueteEnvioDocumentoSalida)Paquete["Paquete"];
                if (miPaquete.ListaFicheros != null)
                {
                    this.lstArchivosAdjuntos.Items.AddRange(miPaquete.ListaFicheros.ToArray());
                }
                CalcularTamañoTotal();

                this.cboCanalSalida.SelectedItem = miPaquete.CanalSalida;
            }
        }

        private void CalcularTamañoTotal()
        {
            long tamaño = 0;
            foreach (System.IO.FileInfo fi in this.lstArchivosAdjuntos.Items)
            {
                tamaño += fi.Length;
            }
            this.lblTamañoTotal.Text = string.Concat("(", AuxIU.ConversorTamañoArchivos.ToComputerSize(tamaño), ")");
            this.lblTamañoTotal.Visible = true;
        }

        private void cmdAgregarFichero_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog fd = new OpenFileDialog();
                fd.CheckFileExists = true;
                fd.CheckPathExists = true;
                fd.Multiselect = true;
                fd.RestoreDirectory = false;
                fd.Title = "Agregar ficheros para el envío";
                fd.ValidateNames = true;
                fd.SupportMultiDottedExtensions = true;
                fd.InitialDirectory = Application.StartupPath;

                if (fd.ShowDialog(this) == DialogResult.OK)
                {
                    foreach (string fichero in fd.FileNames)
                    {
                        System.IO.FileInfo fi = new System.IO.FileInfo(fichero);
                        lstArchivosAdjuntos.Items.Add(fi);
                    }
                    CalcularTamañoTotal();
                }

            }
            catch (Exception ex)
            {
                MostrarError(ex, (Control)sender);
            }
        }


        private void cmd_Aceptar_Click(object sender, EventArgs e)
        {
            try
            {
                NavegarSiguiente();
            }
            catch (Exception ex)
            {
                MostrarError(ex, (Control)sender);
            }
        }


        private void NavegarSiguiente()
        {
            if ((CanalSalida)cboCanalSalida.SelectedItem == CanalSalida.indefinido)
            {
                cMarco.MostrarAdvertencia("Debe seleccionar un Tipo de Envío", "Datos incompletos");
                return;
            }
            PaqueteEnvioDocumentoSalida miPaquete = Paquete != null && Paquete.Contains("Paquete") ? (PaqueteEnvioDocumentoSalida)Paquete["Paquete"] : null;
            if (miPaquete == null) { miPaquete = new PaqueteEnvioDocumentoSalida(); }
            miPaquete.ListaFicheros = new List<System.IO.FileInfo>();
            foreach (System.IO.FileInfo fi in lstArchivosAdjuntos.Items)
            {
                miPaquete.ListaFicheros.Add(fi);
            }
            miPaquete.CanalSalida = (CanalSalida)cboCanalSalida.SelectedItem;
            System.Collections.Hashtable ht = miPaquete.GenerarPaquete();
            cMarco.Navegar("EnvioConfiguracionImpresion", this, this.MdiParent, TipoNavegacion.CerrarLanzador, ref ht);
        }

        private void cmdCancelar_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("¿Seguro que desea salir del envío de documentos?", "Salir", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MostrarError(ex, (Control)sender);
            }
        }
    }



}