using System;
using System.Collections.Generic;
using System.Text;
using Framework.GestorInformes.ContenedorPlantilla.DN;
using Framework.LogicaNegocios.Transacciones;
using Framework.AccesoDatos.MotorAD.LN;
using System.IO;
using Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN;
using Framework.GestorInformes.AdaptadorInformesQueryBuilding.LN;
using System.Collections;

namespace FN.RiesgosVehiculos.LN.QueryBuilding
{
    public class QueryBuildingLN
    {

        public static void CargarDatosBasicos(string rutaPlantillas)
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

                AdaptadorInformesQueryBuildingDN ai = new AdaptadorInformesQueryBuildingDN();
                ai.Plantilla = p;
                ai.TokenTipo = typeof(FN.Seguros.Polizas.DN.PresupuestoDN);
                ai.TablasPrincipales = new ColTablaPrincipalAIQB();

                TablaPrincipalAIQB tpr = new TablaPrincipalAIQB();
                tpr.NombreTabla = "Presupuesto";
                tpr.NombreTablaBD = "vwiPresupuesto";
                tpr.TablasRelacionadas = new ColTablaRelacionadaAIQB();

                TablaRelacionadaAIQB tr2 = new TablaRelacionadaAIQB();
                tr2.fkPadre = "IDPresupuesto";
                tr2.fkPropio = "IDPresupuesto";
                tr2.NombreTablaBD = "vwiPresupuestoLineasProductoAlcanzables";
                tr2.NombreTabla = "LineasProductoAlcanzables";
                tr2.NombreRelacion = "LineaProductoAlcanzable";
                tr2.TablaPadre = tpr;

                TablaRelacionadaAIQB tr3 = new TablaRelacionadaAIQB();
                tr3.fkPadre = "IDPresupuesto";
                tr3.fkPropio = "IDPresupuesto";
                tr3.NombreTablaBD = "vwiPresupuestoLineasProductoEstablecidas";
                tr3.NombreTabla = "LineasProductoEstablecidas";
                tr3.NombreRelacion = "LineaProductoestablecida";
                tr3.TablaPadre = tpr;

                TablaRelacionadaAIQB tr4 = new TablaRelacionadaAIQB();
                tr4.fkPadre = "IDPresupuesto";
                tr4.fkPropio = "IDPresupuesto";
                tr4.NombreTablaBD = "vwiPresupuestoMulticonductoresCuestionarioPresupuesto";
                tr4.NombreTabla = "Multiconductores";
                tr4.NombreRelacion = "ConductorAdicional";
                tr4.TablaPadre = tpr;

                TablaRelacionadaAIQB tr5 = new TablaRelacionadaAIQB();
                tr5.fkPadre = "IDPresupuesto";
                tr5.fkPropio = "IDPresupuesto";
                tr5.NombreTablaBD = "vwiRespuestaAñosSinsiniestroCuestionarioPresupuesto";
                tr5.NombreTabla = "AñosSinsiniestro";
                tr5.NombreRelacion = "AñosSinsiniestro";
                tr5.TablaPadre = tpr;

                TablaRelacionadaAIQB tr6 = new TablaRelacionadaAIQB();
                tr6.fkPadre = "IDPresupuesto";
                tr6.fkPropio = "IDPresupuesto";
                tr6.NombreTablaBD = "vwiRespuestaCPEstacionamientoCuestionarioPresupuesto";
                tr6.NombreTabla = "CPEstacionamiento";
                tr6.NombreRelacion = "CPEstacionamiento";
                tr6.TablaPadre = tpr;

                TablaRelacionadaAIQB tr7 = new TablaRelacionadaAIQB();
                tr7.fkPadre = "IDPresupuesto";
                tr7.fkPropio = "IDPresupuesto";
                tr7.NombreTablaBD = "vwiRespuestaFechaCarnetCuestionarioPresupuesto";
                tr7.NombreTabla = "FechaCarnet";
                tr7.NombreRelacion = "FechaCarnet";
                tr7.TablaPadre = tpr;

                TablaRelacionadaAIQB tr8 = new TablaRelacionadaAIQB();
                tr8.fkPadre = "IDPresupuesto";
                tr8.fkPropio = "IDPresupuesto";
                tr8.NombreTablaBD = "vwiRespuestaJustificantesCuestionarioPresupuesto";
                tr8.NombreTabla = "Justificantes";
                tr8.NombreRelacion = "Justificantes";
                tr8.TablaPadre = tpr;

                TablaRelacionadaAIQB tr9 = new TablaRelacionadaAIQB();
                tr9.fkPadre = "IDPresupuesto";
                tr9.fkPropio = "IDPresupuesto";
                tr9.NombreTablaBD = "vwiRespuestaTipoCarnetCuestionarioPresupuesto";
                tr9.NombreTabla = "TipoCarnet";
                tr9.NombreRelacion = "TipoCarnet";
                tr9.TablaPadre = tpr;

                TablaRelacionadaAIQB tr10 = new TablaRelacionadaAIQB();
                tr10.fkPadre = "IDPresupuesto";
                tr10.fkPropio = "IDPresupuesto";
                tr10.NombreTablaBD = "vwiRiesgoMotorPresupuesto";
                tr10.NombreTabla = "RiesgoMotor";
                tr10.NombreRelacion = "RiesgoMotor";
                tr10.TablaPadre = tpr;

                TablaRelacionadaAIQB tr11 = new TablaRelacionadaAIQB();
                tr11.fkPadre = "IDPresupuesto";
                tr11.fkPropio = "IDPresupuesto";
                tr11.NombreTablaBD = "vwiTomadorPresupuesto";
                tr11.NombreTabla = "Tomador";
                tr11.NombreRelacion = "Tomador";
                tr11.TablaPadre = tpr;

                TablaRelacionadaAIQB tr12 = new TablaRelacionadaAIQB();
                tr12.fkPadre = "IDPresupuesto";
                tr12.fkPropio = "IDPresupuesto";
                tr12.NombreTablaBD = "vwiPresupuestoFraccionamientoAnual";
                tr12.NombreTabla = "FraccAnual";
                tr12.NombreRelacion = "FraccionamientoAnual";
                tr12.TablaPadre = tpr;

                TablaRelacionadaAIQB tr13 = new TablaRelacionadaAIQB();
                tr13.fkPadre = "IDPresupuesto";
                tr13.fkPropio = "IDPresupuesto";
                tr13.NombreTablaBD = "vwiPresupuestoFraccionamientoTrimestral";
                tr13.NombreTabla = "FraccTrimestral";
                tr13.NombreRelacion = "FraccionamientoTrimestral";
                tr13.TablaPadre = tpr;

                TablaRelacionadaAIQB tr14 = new TablaRelacionadaAIQB();
                tr14.fkPadre = "IDPresupuesto";
                tr14.fkPropio = "IDPresupuesto";
                tr14.NombreTablaBD = "vwiPresupuestoFraccionamientoSemestral";
                tr14.NombreTabla = "FraccSemestral";
                tr14.NombreRelacion = "FraccionamientoSemestral";
                tr14.TablaPadre = tpr;


                ai.TablasPrincipales.Add(tpr);

                gi = new GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
                gi.Guardar(ai);

                tr.Confirmar();
            }

        }


        public void PresupuestoCargarEsquemaXML()
        {
            using (Transaccion tr = new Transaccion())
            {
                GestorInstanciacionLN gi = new GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
                IList lista = gi.RecuperarListaPorNombre(typeof(AdaptadorInformesQueryBuildingDN),"Presupuesto");
                AdaptadorInformesQueryBuildingDN ai = (AdaptadorInformesQueryBuildingDN)lista[0];
                System.IO.FileInfo fi = new AdaptadorInformesQueryBuildingLN().GenerarEsquemaXMLEnPlantilla(ai);
                fi.CopyTo(System.IO.Path.Combine(AdaptadorInformesQueryBuildingLN.ObtenerRutaInformes(), "Presupuesto.docx"), true);
                tr.Confirmar();
            }
        }

        public void InformePresupuesto(string idPresupuesto)
        {
            string sql = "SELECT ID FROM tlPresupuestoDN WHERE ID=" + idPresupuesto;
            using (Transaccion tr = new Transaccion())
            {
                GestorInstanciacionLN gi = new GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
                IList lista = gi.RecuperarListaPorNombre(typeof(AdaptadorInformesQueryBuildingDN),"Presupuesto");
                AdaptadorInformesQueryBuildingDN ai = (AdaptadorInformesQueryBuildingDN)lista[0];
                ai.TablasPrincipales[0].CargarDatosSelect("IDPresupuesto", sql, null);
                System.IO.FileInfo fi = new AdaptadorInformesQueryBuildingLN().GenerarInforme(ai);
                string fs = AdaptadorInformesQueryBuildingLN.ObtenerRutaTemporal() + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString() + "-" + DateTime.Now.Millisecond.ToString() + ".docx";
                fi.CopyTo(fs, true);
                System.Diagnostics.Process.Start(fi.FullName);
            }
        }
    }
}
