using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Framework.GestorSalida.DN;

namespace Framework.GestorSalida.Administracion
{
    public partial class frmSeleccionarImpresoras : MotorIU.FormulariosP.FormularioBase 
    {
        public frmSeleccionarImpresoras()
        {
            InitializeComponent();
        }

        private PaquetefrmSeleccionarImpresoras miPaquete;

        public override void Inicializar()
        {
            base.Inicializar();

            if (Paquete != null && Paquete.Contains("Paquete")) miPaquete = (PaquetefrmSeleccionarImpresoras)Paquete["Paquete"];
        }

        private void cmd_Aceptar_Click(object sender, EventArgs e)
        {
            try
            {
                List<ContenedorDescriptorImpresoraDN> lista = ctrlContenedoresImpresora1.CImpresorasSeleccionadas();
                if (lista.Count==0)
                {
                    cMarco.MostrarAdvertencia("Debe seleccionar alguna impresora", "Atención");
                    return;
                }
                if (miPaquete == null) miPaquete = new PaquetefrmSeleccionarImpresoras();
                miPaquete.ImpresorasSeleccionadas = lista;
                Paquete = miPaquete.GenerarPaquete(Paquete);
                Close();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void cmdCancelar_Click(object sender, EventArgs e)
        {
            try
            {
                Paquete.Clear();
                Close();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }
    }

    public class PaquetefrmSeleccionarImpresoras : MotorIU.PaqueteIU
    {
       public List<ContenedorDescriptorImpresoraDN> ImpresorasSeleccionadas;
    }
}