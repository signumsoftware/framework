using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Framework.GestorSalida.ClienteIU.controladoresForm;
using MotorIU.Motor;
using Framework.GestorSalida.DN;

namespace Framework.GestorSalida.ClienteIU
{
    public partial class frmConfiguracionEnvioImpresion : MotorIU.FormulariosP.FormularioBase
    {
        private frmConfiguracionEnvioImpresionCtrl mControlador;

        public frmConfiguracionEnvioImpresion()
        {
            InitializeComponent();
        }


        public override void Inicializar()
        {
            base.Inicializar();

            mControlador = (frmConfiguracionEnvioImpresionCtrl)this.Controlador;

            cboFuncionImpresora.Items.AddRange(mControlador.RecuperarTodasFuncionesImpresora().ToArray());

            if (Paquete != null && Paquete.Contains("Paquete"))
            {
                PaqueteEnvioDocumentoSalida miPaquete = (PaqueteEnvioDocumentoSalida)Paquete["Paquete"];
                this.nudCopias.Value = miPaquete.NumeroCopias;
                if (miPaquete.FuncionImpresora != null)
                {
                    foreach (Framework.GestorSalida.DN.FuncionImpresora fi in cboFuncionImpresora.Items)
                    {
                        if (fi.ID == miPaquete.FuncionImpresora.ID)
                        {
                            this.cboFuncionImpresora.SelectedItem = fi;
                            break;
                        }
                    }
                }
                this.chkPersistente.Checked = miPaquete.Peristente;
                this.tcbPrioridad.Value = miPaquete.Prioridad;
                this.chkMostrarTicket.Checked = miPaquete.MostrarTicket;
            }
        }


        private void cmdAtras_Click(object sender, EventArgs e)
        {
            try
            {
                NavegarAtras();
            }
            catch (Exception ex)
            {
                MostrarError(ex, (Control)sender);
            }
        }


        private void NavegarAtras()
        {
            PaqueteEnvioDocumentoSalida miPaquete = DamePaquete();
            System.Collections.Hashtable ht = CargarPaquete(ref miPaquete);
            cMarco.Navegar("EnvioDocumento", this, this.MdiParent, TipoNavegacion.CerrarLanzador, ref ht);
        }

        private PaqueteEnvioDocumentoSalida DamePaquete()
        {
            PaqueteEnvioDocumentoSalida miPaquete = Paquete != null && Paquete.Contains("Paquete") ? (PaqueteEnvioDocumentoSalida)Paquete["Paquete"] : null;
            return miPaquete;
        }


        private System.Collections.Hashtable CargarPaquete(ref PaqueteEnvioDocumentoSalida miPaquete)
        {
            if (miPaquete == null) { miPaquete = new PaqueteEnvioDocumentoSalida(); }
            miPaquete.MostrarTicket = chkMostrarTicket.Checked;
            miPaquete.Prioridad = tcbPrioridad.Value;
            miPaquete.FuncionImpresora = (FuncionImpresora)cboFuncionImpresora.SelectedItem;
            miPaquete.NumeroCopias = (int)nudCopias.Value;
            System.Collections.Hashtable ht = miPaquete.GenerarPaquete();
            return ht;
        }


        private void cmd_Aceptar_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Validar()) return;
                PaqueteEnvioDocumentoSalida miPaquete = DamePaquete();
                System.Collections.Hashtable ht = CargarPaquete(ref miPaquete);
                cMarco.Navegar("EnvioGestorSalida", this, this.MdiParent, TipoNavegacion.CerrarLanzador, ref ht);
            }
            catch (Exception ex)
            {
                cMarco.MostrarError(ex, (Control)sender);
            }
        }


        private bool Validar()
        {
            if ((FuncionImpresora)cboFuncionImpresora.SelectedItem == null)
            {
                cMarco.MostrarAdvertencia("Debe seleccionar el Tipo de Impresora", "Datos incompletos");
                return false;
            }
            return true;
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