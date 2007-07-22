#region Importaciones

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Framework.AccesoDatos;
using Framework.AccesoDatos.MotorAD.AD;
using Framework.AccesoDatos.MotorAD.LN;
using Framework.DatosNegocio;

#endregion

namespace Framework.LogicaNegocios.Transacciones
{
    public abstract class BaseGenericLN
    {
        private GestorInstanciacionLN NuevoGestor()
        {
            return new GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
        }

        private Ejecutor NuevoEjecutor()
        {
            return new Ejecutor(Transaccion.Actual, Recurso.Actual);
        }

        private AccesorMotorAD NuevoAccesorMotorAD(IConstructorBusquedaAD constructor)
        {
            return new AccesorMotorAD(Transaccion.Actual, Recurso.Actual, constructor);
        }

        #region Metodos

        protected void GenerarTablas<T>()
        {
            using (Transaccion tr = new Transaccion())
            {
                NuevoGestor().GenerarTablas2(typeof (T), null);

                tr.Confirmar();
            }
        }

        /// <summary>
        /// Solo se puede utilziar si el código es genérico sin lógica
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dato"></param>
        protected void Guardar<T>(T dato) where T : EntidadDN
        {
            using (Transaccion tr = new Transaccion())
            {
                NuevoGestor().Guardar(dato);

                tr.Confirmar();
            }
        }

        /// <summary>
        /// Solo se puede utilziar si el código es genérico sin lógica
        /// </summary>
        /// <typeparam name="List<T>"></typeparam>
        /// <param name="lista"></param>
        protected void GuardarLista<T>(List<T> lista) where T : EntidadDN
        {
            using (Transaccion tr = new Transaccion())
            {
                GestorInstanciacionLN gestor = NuevoGestor();

                for (int i = 0; i < lista.Count; i++)
                    gestor.Guardar(lista[i]);

                tr.Confirmar();
            }
        }

        /// <summary>
        /// Solo se puede utilizar si el código es genérico sin lógica
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        protected T Recuperar<T>(string id) where T : EntidadDN
        {
            using (Transaccion tr = new Transaccion())
            {
                T result = NuevoGestor().Recuperar<T>(id);

                tr.Confirmar();

                return result;
            }
        }

        protected void Eliminar<T>(string id)
        {
            using (Transaccion tr = new Transaccion())
            {
                string nombreTipo = typeof (T).Name;

                Ejecutor ejec = NuevoEjecutor();
                string misql = "DELETE FROM tl" + nombreTipo + " WHERE ID = " + id;

                if (ejec.EjecutarNoConsulta(misql) == 0)
                    throw new NingunaFilaAfectadaException("No se ejecutó correctamente la eliminación del " +
                                                           nombreTipo + ". Ninguna fila afectada");

                tr.Confirmar();
            }
        }

        protected int EliminarRelacion(string table, string campo, string idCampo)
        {
            using (Transaccion tr = new Transaccion())
            {
                Ejecutor ejec = NuevoEjecutor();
                string misql = "DELETE FROM " + table + " WHERE " + campo + " = " + idCampo;

                int val = ejec.EjecutarNoConsulta(misql);

                tr.Confirmar();

                return val;
            }
        }


        /// <summary>
        /// Solo se puede utilziar si el código es genérico sin lógica
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        protected void Reactivar<T>(string id) where T : EntidadDN
        {
            IDatoPersistenteDN dato = (IDatoPersistenteDN) Recuperar<T>(id);
            dato.Baja = false;
            Guardar<T>((T) dato);
        }

        /// <summary>
        /// Solo se puede utilizar si el código es genérico sin lógica
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        protected void Baja<T>(string id) where T : EntidadDN
        {
            IDatoPersistenteDN dato = (IDatoPersistenteDN) Recuperar<T>(id);

            using (Transaccion tr = new Transaccion())
            {
                dato.Baja = true;
                NuevoGestor().Baja(dato);

                tr.Confirmar();
            }
        }

        protected List<T> RecuperarLista<T>() where T : EntidadBaseDN
        {
            using (Transaccion tr = new Transaccion())
            {
                AccesorMotorAD aMAD = NuevoAccesorMotorAD(new ConstructorAL(typeof(T)));

                //ArrayList alIDs = aMAD.BuscarGenericoIDS("tl" + typeof (T).Name, null);
                ArrayList alIDs = aMAD.BuscarGenericoIDS( typeof(T), null);

                ArrayList alObj = (ArrayList) NuevoGestor().Recuperar(alIDs, typeof (T), null);

                List<T> objetos = new List<T>(alObj.Count);
                for (int i = 0; i < alObj.Count; i++)
                    objetos.Add((T) alObj[i]);

                tr.Confirmar();

                return objetos;
            }
        }

        protected List<T> RecuperarListaCondicional<T>(IConstructorBusquedaAD constructor) where T : EntidadDN
        {
            using (Transaccion tr = new Transaccion())
            {
                AccesorMotorAD amd = NuevoAccesorMotorAD(constructor);

                ArrayList alIDs = amd.BuscarGenericoIDS(typeof(T));

                ArrayList alObj = (ArrayList) NuevoGestor().Recuperar(alIDs, typeof (T), null);

                List<T> objetos = new List<T>(alObj.Count);
                for (int i = 0; i < alObj.Count; i++)
                    objetos.Add((T) alObj[i]);

                tr.Confirmar();

                return objetos;
            }
        }

        protected List<T> RecuperarListaCondicionalConEntidadesRecuperadas<T>(IConstructorBusquedaAD constructor,
                                                                              IList recuperadas) where T : EntidadDN
        {
            using (Transaccion tr = new Transaccion())
            {
                AccesorMotorAD amd = NuevoAccesorMotorAD(constructor);

                ArrayList alIDs = amd.BuscarGenericoIDS(typeof(T));

                GestorInstanciacionLN gestor = NuevoGestor();

                //hace que los enlaces se hagan bien
                gestor.AñadirAColIntanciasRecuperadas(recuperadas);

                ArrayList alObj = (ArrayList) gestor.Recuperar(alIDs, typeof (T), null);

                List<T> objetos = new List<T>(alObj.Count);
                for (int i = 0; i < alObj.Count; i++)
                    objetos.Add((T) alObj[i]);

                tr.Confirmar();

                return objetos;
            }
        }

        protected delegate T ConstructorDN<T>(DataRow dr);

        protected List<T> RecuperarListaCondicionalConstructorManual<T>(IConstructorBusquedaAD constSQL,
                                                                        ConstructorDN<T> constDN)
        {
            using (Transaccion tr = new Transaccion())
            {
                AccesorMotorAD amd = NuevoAccesorMotorAD(constSQL);

                DataSet ds = null;
                ds = amd.BuscarGenerico(ref ds);

                if (ds == null || ds.Tables.Count != 1)
                    throw new ApplicationException("Error: No se ha podido hacer la consulta");

                List<T> lista = new List<T>();

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    T dn = constDN(dr);
                    lista.Add(dn);
                }

                tr.Confirmar();

                return lista;
            }
        }

        #endregion
    }
}