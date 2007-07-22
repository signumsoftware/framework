using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Framework.GestorSalida.Administracion;
using MotorIU.Motor;
using System.Collections;

namespace Framework.GestorSalida.Administracion
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            NavegadorBase navegador = new NavegadorBase(new Hashtable());

            new ModuloCarga().CargarTablaNavegacion(navegador);

            new Usuarios.IUWin.Form.CargadorMarco().CargarTablaNavegacion(navegador);

            Usuarios.IUWin.Form.PaqueteLogin p = new Framework.Usuarios.IUWin.Form.PaqueteLogin();
            p.Titulo = "Administración Gestor Salida";
            p.FuncionNavegacion = "AdministracionImpresion";

            navegador.NavegarInicial("Login", p.GenerarPaquete());

            Application.Run();
        }
    }

    public class ModuloCarga : IProveedorTablaNavegacion
    {
        #region IProveedorTablaNavegacion Members

        public void CargarTablaNavegacion(INavegador navegador)
        {
            navegador.TablaNavegacion.Add("AdministracionImpresion", new Destino(typeof(frmAdministrarAreaImpresion), typeof(Controladores.frmAdministracionAreaImpresionCtrl)));
            navegador.TablaNavegacion.Add("FuncionImpresion", new Destino(typeof(frmFuncionImpresion), null));
            navegador.TablaNavegacion.Add("SeleccionarImpresoras", new Destino(typeof(frmSeleccionarImpresoras), null));
            navegador.TablaNavegacion.Add("CategoriaImpresoras", new Destino(typeof(frmCategoriaImpresoras), typeof(frmCategoriaImpresionCtrl)));
        }

        #endregion
    }
}