using System;
using System.Collections.Generic;
using System.Text;
using Framework.GestorSalida.DN;
using Framework.GestorSalida.Cliente.Controladores;

namespace Framework.GestorSalida.Administracion.Controladores
{
    class frmAdministracionAreaImpresionCtrl:MotorIU.FormulariosP.ControladorFormBase 
    {

        public List<FuncionImpresora> RecuperarTodasFuncionesImpresora()
        {  return new ControladorGestorSalida().RecuperarTodasFuncionesImpresora(); }

        public void GuardarFuncionImpresora(FuncionImpresora fi)
        {
             new ControladorGestorSalida().GuardarFuncionImpresora(fi);
        }

        public void BajaFuncionImpresora(FuncionImpresora fi)
        {
            new ControladorGestorSalida().BajaFuncionImpresora(fi);
        }

        public void ReactivarFuncionImpresora(FuncionImpresora fi)
        {
            new ControladorGestorSalida().ReactivarFuncionImpresora(fi);
        }

        public List<CategoriaImpresoras> RecuperarTodasCategorias()
        {
           return new ControladorGestorSalida().RecuperarTodasCategorias();
        }

        public void GuardarCategoriaImpresora(CategoriaImpresoras categoriaImpresoras)
        {
            new ControladorGestorSalida().GuardarCategoriaImpresoras(categoriaImpresoras);
        }

        internal void BajaCategoriaImpresoras(CategoriaImpresoras c)
        {
            new ControladorGestorSalida().BajaCategoriaImpresoras(c);
        }

        internal void ReactivarCagetoriaImpresoras(CategoriaImpresoras c)
        {
            new ControladorGestorSalida().ReactivarCategoriaImpresoras(c);
        }

        internal List<ContenedorDescriptorImpresoraDN> RecuperarTodasImpresoras()
        {
           return new ControladorGestorSalida().RecuperarTodasImpresoras();
        }

        internal void GuardarImpresora(ContenedorDescriptorImpresoraDN cImp)
        {
            new ControladorGestorSalida().AltaContenedorDescriptorImpresora(cImp);
        }

        internal void ReactivarImpresora(ContenedorDescriptorImpresoraDN impresora)
        {
            new ControladorGestorSalida().ReactivarImpresora(impresora);
        }

        internal void BajaImpresora(ContenedorDescriptorImpresoraDN impresora)
        {
            new ControladorGestorSalida().BajaContenedorImpresora(impresora); ;
        }
    }
}
