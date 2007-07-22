using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Framework.GestorSalida.DN;
using Framework.AccesoDatos.MotorAD.LN;
using Framework.LogicaNegocios.Transacciones;
using Framework.Colecciones;



namespace Framework.GestorSalida.LN
{
    public class GestorSalidaImpresionLN
    {
        #region Recuperar Categorias Ordenadas

        public List<CategoriaImpresoras> RecuperarCategoriasImpresora()
        {
            List<CategoriaImpresoras> categorias;
            using (Transaccion tr = new Transaccion())
            {
                Framework.ClaseBaseLN.BaseTransaccionConcretaLN ln = new Framework.ClaseBaseLN.BaseTransaccionConcretaLN();
                categorias = new List<CategoriaImpresoras>();
                foreach (CategoriaImpresoras categoria in ln.RecuperarLista(typeof(CategoriaImpresoras)))
                {
                    categorias.Add(categoria);
                }
                tr.Confirmar();
            }
            return categorias;
        }

        public CategoriasImpresorasPorFuncion RecuperarCategoriasOrdenadas()
        {
            List<CategoriaImpresoras> categorias = RecuperarCategoriasImpresora();
            CategoriasImpresorasPorFuncion co = new CategoriasImpresorasPorFuncion();
            co.AddItems(categorias);
            return co;
        }

        #endregion

        /// <summary>
        /// Da de baja el contenedor de impresora, eliminándolo de todas las categorías
        /// en las que se encuentre
        /// </summary>
        /// <param name="impresora"></param>
        public ContenedorDescriptorImpresoraDN BajaContenedorDescriptorImpresora(ContenedorDescriptorImpresoraDN impresora)
        {
            using (Transaccion tr = new Transaccion())
            {
                List<CategoriaImpresoras> categorias = RecuperarCategoriasImpresora();
                Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN gi;
                foreach (CategoriaImpresoras categoria in categorias)
                {
                    if (categoria.ColImpresoras.Contiene(impresora.ID))
                    {
                        categoria.ColImpresoras.EliminarEntidadDN(impresora.ID);
                        gi = new GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
                        gi.Guardar(categoria);
                    }
                }
                object referencia = impresora;
                new Framework.ClaseBaseLN.BaseTransaccionConcretaLN().BajaGenericoDN(ref referencia);
                impresora = (ContenedorDescriptorImpresoraDN)referencia;
                tr.Confirmar();
            }
            return impresora;
        }

        public ContenedorDescriptorImpresoraDN AltaContenedorDescriptorImpresora(ContenedorDescriptorImpresoraDN impresora)
        {
            using (Transaccion tr = new Transaccion())
            {
                List<ContenedorDescriptorImpresoraDN> impresorasCoincidentes = new AD.ConfiguracionImpresionAD().RecuperarContenedorDescriptorImpresoraPorNombre(impresora.NombreImpresora);
                if (impresorasCoincidentes.Count != 0)
                {
                    throw new ApplicationException("Ya existe un contenedor para esa impresora (" + impresora.NombreImpresora + ")");
                }
                GestorInstanciacionLN gi = new GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
                gi.Guardar(impresora);
                tr.Confirmar();
            }
            return impresora;
        }
    }
}
