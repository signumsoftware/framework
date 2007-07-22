using System;
using System.Collections.Generic;
using System.Text;
using MotorIU.Motor;

namespace Framework.GestorSalida.ClienteIU
{
    public class ModuloCarga:IProveedorTablaNavegacion
    {
        #region IProveedorTablaNavegacion Members

        public void CargarTablaNavegacion(INavegador navegador)
        {
            navegador.TablaNavegacion.Add("EnvioDocumento",new Destino(typeof(frmEnviarDocumentoSalida),null));
            navegador.TablaNavegacion.Add("EnvioConfiguracionImpresion",new Destino(typeof(frmConfiguracionEnvioImpresion), typeof(controladoresForm.frmConfiguracionEnvioImpresionCtrl)));
            navegador.TablaNavegacion.Add("EnvioGestorSalida", new Destino(typeof(frmEnvio), typeof(controladoresForm.frmEnvioCtrl)));
        }

        #endregion
    }
}
