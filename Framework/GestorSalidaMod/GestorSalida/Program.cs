using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MotorIU.Motor;

namespace Framework.GestorSalida.ClienteIUWin
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

            NavegadorBase navegador = new NavegadorBase(new System.Collections.Hashtable());

            new Framework.GestorSalida.ClienteIU.ModuloCarga().CargarTablaNavegacion(navegador);
            new Usuarios.IUWin.Form.CargadorMarco().CargarTablaNavegacion(navegador);

            Usuarios.IUWin.Form.PaqueteLogin p = new Framework.Usuarios.IUWin.Form.PaqueteLogin();
            p.Titulo = "Envío documentos";
            p.FuncionNavegacion = "EnvioDocumento";

            navegador.NavegarInicial("Login", p.GenerarPaquete());

            Application.Run();
        }
    }
}