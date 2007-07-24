using System;
using System.Collections.Generic;
using System.Text;
using Framework.LogicaNegocios.Transacciones;
using Framework.AccesoDatos.MotorAD.LN;
using FN.RiesgosVehiculos.AD.QueryBuilding.Properties;
using Framework.GestorInformes.AdaptadorInformesQueryBuilding;
using Framework.GestorInformes.ContenedorPlantilla.DN;
using System.IO;
using Framework.GestorInformes.AdaptadorInformesQueryBuilding.AD;

namespace FN.RiesgosVehiculos.AD.QueryBuilding
{
    public class AIQueryBuildingGBD : Framework.AccesoDatos.MotorAD.GBDBase
    {
        public AIQueryBuildingGBD(Framework.LogicaNegocios.Transacciones.IRecursoLN recurso)
        {
            mRecurso = recurso;
        }

        public override void CrearVistas()
        {
            Framework.AccesoDatos.Ejecutor ej;
            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiPresupuesto);

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiRespuestaAñosSinsiniestroCuestionarioPresupuesto);

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiRespuestaCPEstacionamientoCuestionarioPresupuesto);

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiRespuestaFechaCarnetCuestionarioPresupuesto);

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiRespuestaJustificantesCuestionarioPresupuesto);

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiRespuestaTipoCarnetCuestionarioPresupuesto);

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiRiesgoMotorPresupuesto);

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiTomadorPresupuesto);

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiPresupuestoFraccionamientoAnual);

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiPresupuestoFraccionamientoTrimestral);

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiPresupuestoFraccionamientoSemestral);

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiPresupuestoMulticonductoresCuestionarioPresupuesto );

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiPresupuestoLineasProductoEstablecidas );

            ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.vwiPresupuestoLineasProductoAlcanzables );
        }

        public override void CrearTablas()
        {
            //llamamos al gbd de AIQBGBD (framework)
            new AdaptadorInformesQueryBuildingGBD(mRecurso).CrearTablas();

            //creamos la función de formateo
            Framework.AccesoDatos.Ejecutor ej = new Framework.AccesoDatos.Ejecutor(null, mRecurso);
            ej.EjecutarNoConsulta(Resources.FormatShortDate);
        }

        public void CargarDatosBasicos(string rutaPlantillas)
        {
            using (Transaccion tr = new Transaccion())
            {
                GestorInstanciacionLN gi;

                //Presupuesto
                TipoPlantilla tp = new TipoPlantilla();
                tp.Nombre = "Presupuesto";
                ContenedorPlantillaDN p = new ContenedorPlantillaDN();
                p.Nombre = "Presupuesto";
                p.TipoPlantilla = tp;
                p.HuellaFichero = new HuellaFicheroPlantillaDN();
                p.HuellaFichero.RutaFichero = Path.Combine(rutaPlantillas, "Presupuesto.docx");
                gi = new GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
                gi.Guardar(p);

                tr.Confirmar();
            }

        }
    }
}
