using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

using Framework.AccesoDatos;
using Framework.AccesoDatos.MotorAD.AD;

namespace Framework.Mensajeria.GestorMails.AD
{
    public class ConstructorTodosVistaAD : IConstructorBusquedaAD



    {
        #region Campos

        string _tabla;

        #endregion

        /// <summary>
        /// Vale para encontrar todas las entidades
        /// </summary>
        public ConstructorTodosVistaAD(string tabla)
        {
            this._tabla = tabla;
        }

        #region Metodos IConstructorBusquedaAD

        public string ConstruirSQLBusqueda(string pNombreVistaVisualizacion, string pNombreVistaFiltro, List<Framework.AccesoDatos.MotorAD.DN.CondicionRelacionalDN> pFiltro, ref List<System.Data.IDataParameter> pParametros)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public string ConstruirSQLBusqueda(string pNombreVistaVisualizacion, string pNombreVistaFiltro, Framework.AccesoDatos.MotorAD.DN.FiltroDN pFiltro, ref List<System.Data.IDataParameter> pParametros)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public string ConstruirSQLBusqueda(ref List<System.Data.IDataParameter> pParametros)
        {
            string sql;
            sql = "select ID from " + _tabla;
            return sql;
        }

        #endregion

        #region IConstructorBusquedaAD Members

        public string ConstruirSQLBusqueda(Type pTypo, string pNombreVistaFiltro, List<Framework.AccesoDatos.MotorAD.DN.CondicionRelacionalDN> pFiltro, ref List<IDataParameter> pParametros)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
