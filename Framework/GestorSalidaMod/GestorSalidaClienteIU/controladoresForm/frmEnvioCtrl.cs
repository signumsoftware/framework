using System;
using System.Collections.Generic;
using System.Text;
using Framework.GestorSalida.Cliente.Controladores;
using Framework.GestorSalida.DN;

namespace Framework.GestorSalida.ClienteIU.controladoresForm
{
    class frmEnvioCtrl:MotorIU.FormulariosP.ControladorFormBase 
    {
        public byte[] ComprimirArchivos(List<System.IO.FileInfo> listaFicheros)
        {
            return new ControladorGestorSalida().ComprimirArchivos(listaFicheros);
        }


        internal string InsertarDocumentoSalidaEnCola(DocumentoSalida docS)
        {
            return new ControladorGestorSalida().EnviarDocumentoSalida(docS);
        }
    }
}
