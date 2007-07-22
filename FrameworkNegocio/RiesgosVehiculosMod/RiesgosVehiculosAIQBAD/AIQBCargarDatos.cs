using System;
using System.Collections.Generic;
using System.Text;
using Framework.GestorInformes.ContenedorPlantilla.DN;
using Framework.LogicaNegocios.Transacciones;
using Framework.AccesoDatos.MotorAD.LN;
using System.IO;

namespace FN.RiesgosVehiculos.AD.QueryBuilding
{
    public static class AIQBCargarDatos
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

                //Plantilla

                tr.Confirmar();
            }

        }
    }
}
