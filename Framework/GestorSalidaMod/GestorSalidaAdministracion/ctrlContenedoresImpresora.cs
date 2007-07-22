using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Framework.GestorSalida.Administracion.controladores;
using Framework.GestorSalida.DN;

namespace Framework.GestorSalida.Administracion
{
    public partial class ctrlContenedoresImpresora : MotorIU.ControlesP.BaseControlP
    {
        public ctrlContenedoresImpresora()
        {
            InitializeComponent();
        }

        private DataSet mDSCImpresoras;
        private ctrlContenedoresImpresoraCtrl mControlador;

        public override void Inicializar()
        {
            base.Inicializar();

            mControlador = new ctrlContenedoresImpresoraCtrl(Marco, this);
            RellenarCImpresoras();
        }


        public void RellenarCImpresoras()
        {
            mDSCImpresoras = new DataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add("CImpresora", typeof(ContenedorDescriptorImpresoraDN));
            dt.Columns.Add("ID", typeof(int)); 
            dt.Columns.Add("Impresora", typeof(string));
            dt.Columns.Add("Activo", typeof(bool));
            mDSCImpresoras.Tables.Add(dt);
            foreach (ContenedorDescriptorImpresoraDN ci in mControlador.RecuperarTodosContenedoresImpresora())
            {
                DataRow r = dt.NewRow();
                r["CImpresora"] = ci;
                r["ID"] = ci.ID;
                r["Impresora"] = ci.ToString();
                r["Activo"] = !ci.Baja;
                dt.Rows.Add(r);
            }
            dgvCImpresoras.DataSource = mDSCImpresoras.Tables[0];
            dgvCImpresoras.Columns["CImpresora"].Visible = false;
            dgvCImpresoras.Columns["ID"].Width = 50;
            dgvCImpresoras.Columns["Impresora"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvCImpresoras.Columns["Activo"].Width = 50;
        }


        public List<ContenedorDescriptorImpresoraDN> CImpresorasSeleccionadas()
        {
            List<ContenedorDescriptorImpresoraDN> lista = new List<ContenedorDescriptorImpresoraDN>();
            foreach (DataGridViewRow r in dgvCImpresoras.SelectedRows)
            {
                lista.Add((ContenedorDescriptorImpresoraDN)r.Cells["CImpresora"].Value);
            }
            return lista;
        }
    }
}
