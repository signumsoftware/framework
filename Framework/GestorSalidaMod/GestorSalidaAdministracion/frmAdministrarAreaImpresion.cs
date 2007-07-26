using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Framework.GestorSalida.Administracion.Controladores;
using Framework.GestorSalida.DN;
using System.Collections;
using Framework.AgenteImpresion;
using Framework.IU.IUComun;
using MotorIU.Motor;

namespace Framework.GestorSalida.Administracion
{
    public partial class frmAdministrarAreaImpresion : MotorIU.FormulariosP.FormularioBase
    {
        public frmAdministrarAreaImpresion()
        {
            InitializeComponent();
        }

        private frmAdministracionAreaImpresionCtrl mControlador;
        private DataSet mDSFuncionesImpresion;
        private DataSet mDSCategoriasImpresion;

        public override void Inicializar()
        {
            base.Inicializar();

            mControlador = (frmAdministracionAreaImpresionCtrl)Controlador;
            RellenarFuncionesImpresion();
            RellenarCategorias();
            RellenarImpresoras();
        }




        #region Funciones de Impresión

        private void RellenarFuncionesImpresion()
        {
            List<FuncionImpresora> funciones = mControlador.RecuperarTodasFuncionesImpresora();
            mDSFuncionesImpresion = new DataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("FuncionImpresora", typeof(FuncionImpresora)));
            dt.Columns.Add(new DataColumn("ID", typeof(int)));
            dt.Columns.Add(new DataColumn("Nombre", typeof(string)));
            dt.Columns.Add(new DataColumn("Descripción", typeof(string)));
            dt.Columns.Add(new DataColumn("Activo", typeof(bool)));
            mDSFuncionesImpresion.Tables.Add(dt);

            foreach (FuncionImpresora fi in funciones)
            {
                DataRow r = dt.NewRow();
                r["FuncionImpresora"] = fi;
                r["ID"] = fi.ID;
                r["Nombre"] = fi.Nombre;
                r["Descripción"] = fi.Descripcion;
                r["Activo"] = !(fi.Baja);
                dt.Rows.Add(r);
            }

            dgvFunciones.DataSource = mDSFuncionesImpresion.Tables[0];
            dgvFunciones.Columns["FuncionImpresora"].Visible = false;
            dgvFunciones.Columns["ID"].Width = 50;
            dgvFunciones.Columns["Descripción"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvFunciones.Columns["Activo"].Width = 50;
        }

        private void cmdAgregarFuncion_Click(object sender, EventArgs e)
        {
            try
            {
                AgregarFuncionImpresion();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void AgregarFuncionImpresion()
        {
            Hashtable miPaquete = new Hashtable();
            cMarco.Navegar("FuncionImpresion", this, TipoNavegacion.Modal, ref miPaquete);
            if (miPaquete != null && miPaquete.Contains("Paquete"))
            {
                PaquetefrmFuncionImpresion p = (PaquetefrmFuncionImpresion)miPaquete["Paquete"];
                FuncionImpresora fi = p.FuncionImpresora;
                if (fi != null) { mControlador.GuardarFuncionImpresora(fi); }
                RellenarFuncionesImpresion();
            }
        }

        private void cmdEditarFuncionImpresion_Click(object sender, EventArgs e)
        {
            try
            {
                if (!AlgoSeleccionadoFuncion()) { return; }
                if (dgvFunciones.SelectedRows.Count > 1)
                {
                    cMarco.MostrarAdvertencia("Debe seleccionar sólo la Función de Impresión que sea editar", "Atención");
                    return;
                }
                FuncionImpresora fi = (FuncionImpresora)dgvFunciones.SelectedRows[0].Cells["FuncionImpresora"].Value;
                if (fi.Baja) { cMarco.MostrarAdvertencia("No se puede editar un objeto que no está Activo.\r\nSi desea editar este objeto primero debe reactivarlo.", "Atención"); return; }
                PaquetefrmFuncionImpresion p = new PaquetefrmFuncionImpresion();
                p.FuncionImpresora = fi;
                Hashtable ht = p.GenerarPaquete();
                cMarco.Navegar("FuncionImpresion", this, TipoNavegacion.Modal, ref ht);
                if (ht != null && ht.Contains("Paquete"))
                {
                    p = (PaquetefrmFuncionImpresion)ht["Paquete"];
                    if (p.FuncionImpresora.Estado == Framework.DatosNegocio.EstadoDatosDN.Modificado)
                    {
                        mControlador.GuardarFuncionImpresora(p.FuncionImpresora);
                        RellenarFuncionesImpresion();
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private bool AlgoSeleccionadoFuncion()
        {
            if (dgvFunciones.SelectedRows.Count == 0)
            {
                cMarco.MostrarAdvertencia("Debe seleccionar la Función de Impresión", "Atención");
                return false;
            }
            return true;
        }

        private void cmdEliminarFuncion_Click(object sender, EventArgs e)
        {
            try
            {
                using (new AuxIU.CursorScope())
                {
                    if (!AlgoSeleccionadoFuncion()) { return; }
                    bool modificado = false;
                    for (int i = 0; i < dgvFunciones.SelectedRows.Count; i++)
                    {
                        FuncionImpresora fi = (FuncionImpresora)dgvFunciones.SelectedRows[i].Cells["FuncionImpresora"].Value;
                        if (fi.Baja) { break; }
                        mControlador.BajaFuncionImpresora(fi);
                        modificado = true;
                    }
                    if (modificado) { RellenarFuncionesImpresion(); }
                }
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void cmdReactivarFuncionImpresion_Click(object sender, EventArgs e)
        {
            try
            {
                using (new AuxIU.CursorScope())
                {
                    if (!AlgoSeleccionadoFuncion()) { return; }
                    bool modificado = false;
                    for (int i = 0; i < dgvFunciones.SelectedRows.Count; i++)
                    {
                        FuncionImpresora fi = (FuncionImpresora)dgvFunciones.SelectedRows[i].Cells["FuncionImpresora"].Value;
                        if (!fi.Baja) { break; }
                        mControlador.ReactivarFuncionImpresora(fi);
                        modificado = true;
                    }
                    if (modificado) { RellenarFuncionesImpresion(); }
                }
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        #endregion

        #region Categorías Impresión

        private void RellenarCategorias()
        {
            List<CategoriaImpresoras> categorias = mControlador.RecuperarTodasCategorias();
            mDSCategoriasImpresion = new DataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("CategoriaImpresion", typeof(CategoriaImpresoras)));
            dt.Columns.Add(new DataColumn("ID", typeof(int)));
            dt.Columns.Add(new DataColumn("Nombre", typeof(string)));
            dt.Columns.Add(new DataColumn("Función Asociada", typeof(string)));
            dt.Columns.Add(new DataColumn("Impresoras Asociadas", typeof(int)));
            dt.Columns.Add(new DataColumn("Activo", typeof(bool)));
            mDSCategoriasImpresion.Tables.Add(dt);

            foreach (CategoriaImpresoras ci in categorias)
            {
                DataRow r = dt.NewRow();
                r["CategoriaImpresion"] = ci;
                r["ID"] = ci.ID;
                r["Nombre"] = ci.Nombre;
                r["Función Asociada"] = ci.FuncionImpresora.Nombre;
                r["Impresoras Asociadas"] = ci.ColImpresoras.Count;
                r["Activo"] = !ci.Baja;
                dt.Rows.Add(r);
            }
            dgvCategorias.DataSource = mDSCategoriasImpresion.Tables[0];
            dgvCategorias.Columns["CategoriaImpresion"].Visible = false;
            dgvCategorias.Columns["ID"].Width = 50;
            dgvCategorias.Columns["Función Asociada"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvCategorias.Columns["Activo"].Width = 50;
        }

        private bool SoloUnoSeleccionadoCategoria()
        {
            if (dgvCategorias.SelectedRows.Count > 1)
            {
                cMarco.MostrarAdvertencia("Debe seleccionar sólo el elemento que desea editar", "Atención");
                return false;
            }
            return true;
        }

        private bool AlgoSeleccionadoCategoria()
        {
            if (dgvCategorias.SelectedRows.Count == 0)
            {
                cMarco.MostrarAdvertencia("Debe seleccionar alguna categoría", "Atención");
                return false;
            }
            return true;
        }

        private void cmdEditarCategoria_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (!AlgoSeleccionadoCategoria()) return;
                if (!SoloUnoSeleccionadoCategoria()) return;
                PaquetefrmCategoriaImpresion mip = new PaquetefrmCategoriaImpresion();
                mip.CategoriaImpresoras = (CategoriaImpresoras)dgvCategorias.SelectedRows[0].Cells["CategoriaImpresion"].Value;
                Hashtable ht = mip.GenerarPaquete();
                cMarco.Navegar("CategoriaImpresoras", this, TipoNavegacion.Modal, ref ht);
                mip = (ht != null && ht.Contains("Paquete")) ? (PaquetefrmCategoriaImpresion)ht["Paquete"] : null;
                if (mip != null && mip.CategoriaImpresoras.Estado == Framework.DatosNegocio.EstadoDatosDN.Modificado)
                {
                    mControlador.GuardarCategoriaImpresora(mip.CategoriaImpresoras);
                    RellenarCategorias();
                }
            }
            catch (Exception ex) { MostrarError(ex); }


        }

        private void cmdAgregarCategoria_Click_1(object sender, EventArgs e)
        {
            try
            {
                Hashtable ht = new Hashtable();
                cMarco.Navegar("CategoriaImpresoras", this, TipoNavegacion.Modal, ref ht);
                using (new AuxIU.CursorScope())
                {
                    PaquetefrmCategoriaImpresion miPaquete = (ht != null && ht.Contains("Paquete")) ? (PaquetefrmCategoriaImpresion)ht["Paquete"] : null;
                    if (miPaquete != null)
                    {
                        mControlador.GuardarCategoriaImpresora(miPaquete.CategoriaImpresoras);
                        RellenarCategorias();
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void cmdReactivarCategoria_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (!AlgoSeleccionadoCategoria()) return;
                bool modificado;
                for (int i = 0; i < dgvCategorias.SelectedRows.Count; i++)
                {
                    CategoriaImpresoras c = (CategoriaImpresoras)dgvCategorias.SelectedRows[i].Cells["CategoriaImpresion"].Value;
                    if (c.Baja)
                    {
                        mControlador.ReactivarCagetoriaImpresoras(c);
                        modificado = true;
                    }
                }
                RellenarCategorias();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void cmdEliminarCategoria_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (!AlgoSeleccionadoCategoria()) return;
                bool modificado;
                for (int i = 0; i < dgvCategorias.SelectedRows.Count; i++)
                {
                    CategoriaImpresoras c = (CategoriaImpresoras)dgvCategorias.SelectedRows[i].Cells["CategoriaImpresion"].Value;
                    if (!c.Baja)
                    {
                        mControlador.BajaCategoriaImpresoras(c);
                        modificado = true;
                    }
                }
                RellenarCategorias();
            }
            catch (Exception ex) { MostrarError(ex); }
        }


        #endregion

        #region Impresoras

        private void RellenarImpresoras()
        {
            ctrlContenedoresImpresora1.RellenarCImpresoras();
        }

        private void cmdRefrescarImpresors_Click(object sender, EventArgs e)
        {
            try
            {
                using (new AuxIU.CursorScope())
                {
                    List<GestorImpresora.DescriptorImpresora> impresorasSistema = GestorImpresora.ObtenerImpresorasSistema();
                    lstImpresorasSistema.Items.Clear();
                    foreach (GestorImpresora.DescriptorImpresora di in impresorasSistema)
                    {
                        lstImpresorasSistema.Items.Add(di);
                    }
                    lstImpresorasSistema.Refresh();
                }
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void cmdAgregarImpresora_Click(object sender, EventArgs e)
        {
            try
            {
                if (lstImpresorasSistema.SelectedItems.Count == 0)
                {
                    cMarco.MostrarAdvertencia("Debe seleccionar alguna impresora del sistema", "Atención");
                    return;
                }
                List<ContenedorDescriptorImpresoraDN> impresoras = mControlador.RecuperarTodasImpresoras();
                int flag = 0;
                for (int i = 0; i < lstImpresorasSistema.SelectedItems.Count; i++)
                {
                    GestorImpresora.DescriptorImpresora impSis = (GestorImpresora.DescriptorImpresora)lstImpresorasSistema.Items[i];
                    flag += AgregarImpresoras(impSis, impresoras);
                }
                if (flag != 0) RellenarImpresoras();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private int AgregarImpresoras(GestorImpresora.DescriptorImpresora impSis, List<ContenedorDescriptorImpresoraDN> impresoras)
        {
            bool existe = false;
            foreach (ContenedorDescriptorImpresoraDN impresora in impresoras)
            {
                if (impresora.NombreImpresora == impSis.Nombre)
                {
                    cMarco.MostrarAdvertencia("La impresora del sistema seleccionada ya se encuentra en las impresoras asociadas(" + impresora.NombreImpresora + ")", "Error");
                    existe = true;
                    break;
                }
            }
            if (!existe)
            {
                ContenedorDescriptorImpresoraDN cImp = new ContenedorDescriptorImpresoraDN(impSis);
                mControlador.GuardarImpresora(cImp);
                return 1;
            }
            return 0;
        }

        private void cmdReactivarimpresora_Click(object sender, EventArgs e)
        {
            try
            {
                if (!AlgoSeleccionadoImpresoras()) return;
                bool modificado = false;
                using (new AuxIU.CursorScope())
                {
                    foreach (ContenedorDescriptorImpresoraDN impresora in ctrlContenedoresImpresora1.CImpresorasSeleccionadas())
                    {
                        if (impresora.Baja)
                        {
                            mControlador.ReactivarImpresora(impresora);
                            modificado = true;
                        }
                    }
                    if (modificado) RellenarImpresoras();
                }
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private bool AlgoSeleccionadoImpresoras()
        {
            if (ctrlContenedoresImpresora1.CImpresorasSeleccionadas().Count == 0)
            {
                cMarco.MostrarAdvertencia("Debe seleccionar una impresora asociada", "Atención");
                return false;
            }
            return true;
        }

        private void cmdEliminarImpresora_Click(object sender, EventArgs e)
        {
            try
            {
                if (!AlgoSeleccionadoImpresoras()) return;
                if (MessageBox.Show("Si da de baja alguna impresora ésta desaparecerá de todas las categorías a las que esté asociada\r\n¿Desea continuar?", "Atención", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return;
                bool modificado = false;
                using (new AuxIU.CursorScope())
                {
                    foreach (ContenedorDescriptorImpresoraDN impresora in ctrlContenedoresImpresora1.CImpresorasSeleccionadas())
                    {
                        if (!impresora.Baja)
                        {
                            mControlador.BajaImpresora(impresora);
                            modificado = true;
                        }
                    }
                    if (modificado)
                    {
                        RellenarImpresoras();
                        RellenarCategorias();
                    }
                }
            }
            catch (Exception ex) { MostrarError(ex); }
        }
        #endregion




    }
}