using System;
using System.Collections.Generic;
using System.Text;
using MotorIU.Motor;
using MotorIU.ControlesP;

namespace Framework.GestorSalida.Administracion.controladores
{
    class ctrlContenedoresImpresoraCtrl : MotorIU.ControlesP.ControladorControlBase
    {
        public ctrlContenedoresImpresoraCtrl(INavegador navegador, IControlP control)
            : base(navegador, control)
        { }

        internal List<Framework.GestorSalida.DN.ContenedorDescriptorImpresoraDN> RecuperarTodosContenedoresImpresora()
        {
           return new GestorSalida.Cliente.Controladores.ControladorGestorSalida().RecuperarTodasImpresoras();
        }
    }
}
