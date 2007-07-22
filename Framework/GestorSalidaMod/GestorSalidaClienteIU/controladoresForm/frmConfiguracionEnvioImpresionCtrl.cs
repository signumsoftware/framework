using System;
using System.Collections.Generic;
using System.Text;
using Framework.GestorSalida.Cliente.Controladores;
using Framework.GestorSalida.DN;

namespace Framework.GestorSalida.ClienteIU.controladoresForm
{
    public class frmConfiguracionEnvioImpresionCtrl:MotorIU.FormulariosP.ControladorFormBase
    {
        public List<FuncionImpresora> RecuperarTodasFuncionesImpresora()
        { return new ControladorGestorSalida().RecuperarTodasFuncionesImpresora(); }
    }
}
