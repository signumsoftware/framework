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
    public partial class frmFuncionImpresion : MotorIU.FormulariosP.FormularioBase
    {
        public frmFuncionImpresion()
        {
            InitializeComponent();
        }

        private FuncionImpresora mFuncion;
        private PaquetefrmFuncionImpresion miPaquete;

        public override void Inicializar()
        {
            base.Inicializar();

            if (Paquete != null && Paquete.Contains("Paquete"))
            {
                miPaquete = (PaquetefrmFuncionImpresion)Paquete["Paquete"];
                mFuncion = miPaquete.FuncionImpresora;
                if (mFuncion != null)
                {
                    this.txtNombre.Text = mFuncion.Nombre;
                    this.txtDescripcion.Text = mFuncion.Descripcion;
                }
            }
        }

        private void cmd_Aceptar_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtDescripcion.Text.Trim()))
                {
                    cMarco.MostrarAdvertencia("El nombre no puede estar vacío","Atención");
                    return;
                }

                if (mFuncion == null) { mFuncion = new FuncionImpresora(); }

                mFuncion.Nombre = txtNombre.Text ;
                mFuncion.Descripcion = txtDescripcion.Text ;

                if (miPaquete == null) { miPaquete = new PaquetefrmFuncionImpresion(); }

                miPaquete.FuncionImpresora = mFuncion;

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

    public class PaquetefrmFuncionImpresion : MotorIU.PaqueteIU
    {
        public FuncionImpresora FuncionImpresora;
    }
}