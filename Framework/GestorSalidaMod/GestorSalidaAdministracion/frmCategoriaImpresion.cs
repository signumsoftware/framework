using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Framework.GestorSalida.DN;
using Framework.GestorSalida.Administracion.Controladores;
using System.Collections;

namespace Framework.GestorSalida.Administracion
{
    public partial class frmCategoriaImpresoras : MotorIU.FormulariosP.FormularioBase
    {
        public frmCategoriaImpresoras()
        {
            InitializeComponent();
        }

        private frmCategoriaImpresionCtrl mControlador;
        private CategoriaImpresoras mCategoria;
        private PaquetefrmCategoriaImpresion miPaquete;


        public override void Inicializar()
        {
            base.Inicializar();

            mControlador = (frmCategoriaImpresionCtrl)Controlador;

            lstImpresoras.DisplayMember = "NombreImpresora";
            cboFuncion.Items.AddRange(mControlador.RecuperarTodasFuncionesImpresion().ToArray());

            miPaquete = Paquete != null && Paquete.Contains("Paquete") ? (PaquetefrmCategoriaImpresion)Paquete["Paquete"] : null;
            if (miPaquete != null)
            {
                mCategoria = miPaquete.CategoriaImpresoras;
                if (mCategoria != null)
                {
                    txtNombre.Text = mCategoria.Nombre;
                    foreach (FuncionImpresora fi in cboFuncion.Items)
                    {
                        if (fi.ID == mCategoria.FuncionImpresora.ID)
                        {
                            cboFuncion.SelectedItem = fi;
                            break;
                        }
                    }
                    lstImpresoras.Items.AddRange(mCategoria.ColImpresoras.ToArray());
                }
            }

        }

        private void cmd_Aceptar_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Validar()) return;
                if (mCategoria == null) mCategoria = new CategoriaImpresoras();
                mCategoria.Nombre = txtNombre.Text;
                if (mCategoria.FuncionImpresora == null || mCategoria.FuncionImpresora.ID != ((FuncionImpresora)cboFuncion.SelectedItem).ID) mCategoria.FuncionImpresora = (FuncionImpresora)cboFuncion.SelectedItem;
                if (mCategoria.ColImpresoras == null)
                {
                    mCategoria.ColImpresoras = new ColContenedorDescriptorImpresorasDN();
                    foreach (ContenedorDescriptorImpresoraDN imp in lstImpresoras.Items)
                    {
                        mCategoria.ColImpresoras.Add(imp);
                    }
                }
                if (miPaquete == null) miPaquete = new PaquetefrmCategoriaImpresion();
                miPaquete.CategoriaImpresoras = mCategoria;
                Paquete = miPaquete.GenerarPaquete(Paquete);
                Close();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private bool Validar()
        {
            if (cboFuncion.SelectedItem == null)
            {
                cMarco.MostrarAdvertencia("Debe seleccionar una Función de Impresión asociada a la Categoría", "Atención");
                return false;
            }
            if (string.IsNullOrEmpty(txtNombre.Text.Trim()))
            {
                cMarco.MostrarAdvertencia("Debe definir un nombre para la configuración de impresoras", "Atención");
                return false;
            }
            return true;
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

        private void cmdEliminarCategoria_Click(object sender, EventArgs e)
        {
            try
            {
                EliminarCategoria();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void EliminarCategoria()
        {
            if (lstImpresoras.SelectedItem == null)
            {
                cMarco.MostrarAdvertencia("No ha seleccionado ninguna impresora", "Eliminar");
                return;
            }
            ContenedorDescriptorImpresoraDN imp = (ContenedorDescriptorImpresoraDN)lstImpresoras.SelectedItem;
            lstImpresoras.Items.Remove(imp);
            if (mCategoria != null && mCategoria.ColImpresoras != null)
            {
                mCategoria.ColImpresoras.EliminarEntidadDN(imp.ID);
            }
        }

        private void cmdAgregarCategoria_Click(object sender, EventArgs e)
        {
            try
            {
                using (new AuxIU.CursorScope())
                {
                    Hashtable ht = new Hashtable();
                    cMarco.Navegar("SeleccionarImpresoras", this, MotorIU.Motor.TipoNavegacion.Modal, ref ht);
                    PaquetefrmSeleccionarImpresoras mip = (ht != null && ht.Contains("Paquete")) ? (PaquetefrmSeleccionarImpresoras)ht["Paquete"] : null;
                    if (mip != null)
                    {
                        foreach (ContenedorDescriptorImpresoraDN ci in mip.ImpresorasSeleccionadas)
                        {
                            if (!ci.Baja)
                            {
                                bool existe = false;
                                foreach (ContenedorDescriptorImpresoraDN ciE in lstImpresoras.Items)
                                {
                                    if (ciE.ID == ci.ID) existe = true;
                                }
                                if (!existe)
                                {
                                    lstImpresoras.Items.Add(ci);
                                    if (mCategoria != null)
                                    {
                                        if (mCategoria.ColImpresoras == null) mCategoria.ColImpresoras = new ColContenedorDescriptorImpresorasDN();
                                        mCategoria.ColImpresoras.Add(ci);
                                    }
                                }
                            }
                            else cMarco.MostrarAdvertencia("No se pueden agregar contenedores de impresora que estén dasos de baja", "Atención");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }
    }

    public class PaquetefrmCategoriaImpresion : MotorIU.PaqueteIU
    {
        public CategoriaImpresoras CategoriaImpresoras;
    }
}