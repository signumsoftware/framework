using System;
using System.Collections.Generic;
using System.Text;
using Framework.GestorSalida.Cliente.Controladores;
using Framework.GestorSalida.DN;

namespace Framework.GestorSalida.Administracion
{
    class frmCategoriaImpresionCtrl: MotorIU.FormulariosP.ControladorFormBase 
    {
        public List<Framework.GestorSalida.DN.FuncionImpresora> RecuperarTodasFuncionesImpresion()
        { return new ControladorGestorSalida().RecuperarTodasFuncionesImpresora(); }
    }
}
