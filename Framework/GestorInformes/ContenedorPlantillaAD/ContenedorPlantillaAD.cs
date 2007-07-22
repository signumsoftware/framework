using System;
using System.Collections.Generic;
using System.Text;
using Framework.LogicaNegocios.Transacciones;
using Framework.AccesoDatos;
using Framework.GestorInformes.ContenedorPlantilla.DN;
using System.Collections;

namespace Framework.GestorInformes.ContenedorPlantilla.AD
{
    public class ContenedorPlantillaAD
    {
        public List<ContenedorPlantillaDN> RecuperarPlantilla(string NombrePlantilla, TipoPlantilla pTipoPlantilla)
        {
            List<ContenedorPlantillaDN> lista = new List<ContenedorPlantillaDN>();

            using (Transaccion tr = new Transaccion())
            {
                List<System.Data.IDataParameter> Parametros = new List<System.Data.IDataParameter>();

                string sql = "SELECT ID FROM tlContenedorPlantillaDN WHERE Nombre=@Nombre";

                Parametros.Add(ParametrosConstAD.ConstParametroString("@Nombre", NombrePlantilla));

                if (pTipoPlantilla != null)
                {
                    sql += " AND idTipoPlantilla=@idTipoPlantilla";
                    Parametros.Add(ParametrosConstAD.ConstParametroID("@idTipoPlantilla", pTipoPlantilla.ID));
                }

                Framework.AccesoDatos.Ejecutor ej = new Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual);

                System.Data.DataSet ds = ej.EjecutarDataSet(sql, Parametros);
                ArrayList listaId = new ArrayList();
                foreach (System.Data.DataRow fila in ds.Tables[0].Rows)
                {
                    listaId.Add(fila[0].ToString());
                }

                Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN gi = new Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
                IList l = gi.Recuperar(listaId, typeof(ContenedorPlantillaDN), null);

                foreach (ContenedorPlantillaDN cp in l)
                {
                    lista.Add(cp);
                }

                tr.Confirmar();
            }

            return lista;
        }

        public List<TipoPlantilla> RecuperarTipoPlantilla(string nombre)
        {
            List<TipoPlantilla> lista = new List<TipoPlantilla>();

            using (Transaccion tr = new Transaccion())
            {
                List<System.Data.IDataParameter> Parametros = new List<System.Data.IDataParameter>();

                string sql = "SELECT ID FROM tlTipoPlantilla WHERE Nombre=@Nombre";

                Parametros.Add(ParametrosConstAD.ConstParametroString("@Nombre", nombre));

                Framework.AccesoDatos.Ejecutor ej = new Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual);

                System.Data.DataSet ds = ej.EjecutarDataSet(sql, Parametros);
                ArrayList listaId = new ArrayList();
                foreach (System.Data.DataRow fila in ds.Tables[0].Rows)
                {
                    listaId.Add(fila[0].ToString());
                }

                Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN gi = new Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
                IList l = gi.Recuperar(listaId, typeof(TipoPlantilla), null);

                foreach (TipoPlantilla tp in l)
                {
                    lista.Add(tp);
                }

                tr.Confirmar();
            }

            return lista;
        }

    }
}

