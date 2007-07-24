using System;
using System.Collections.Generic;
using System.Text;
using Framework.LogicaNegocios.Transacciones;
using Framework.ClaseBaseLN;
using Framework.Cuestionario.CuestionarioDN;
using FN.RiesgosVehiculos.LN.RiesgosVehiculosLN;
using GSAMV.LN;
using FN.Seguros.Polizas.DN;
using SerializadorTexto;
using FicherosWebAD;

namespace FicherosWebDF
{
    public static class CargadorWebLN
    {
        public static string[] CargadorFichero(byte[] fichero)
        {
            List<string> errores = new List<string>();
            BaseTransaccionConcretaLN btln = new BaseTransaccionConcretaLN();
            CuestionarioDN cuestionario = btln.RecuperarLista(typeof(CuestionarioDN))[0] as CuestionarioDN;

            FicheroWeb ficheroWeb = Serializador.AbrirBytes<FicheroWeb>(fichero);
            for (int i = 0; i < ficheroWeb.Lineas.Count; i++)
            {
                try
                {
                    using (Transaccion tr = new Transaccion())
                    {
                        CuestionarioResueltoDN resuelto = TraductorCuestionarios.Traducir(ficheroWeb.Lineas[0], cuestionario);
                        AdaptadorCuestionarioLN ln = new AdaptadorCuestionarioLN();
                        PresupuestoDN presupuesto = ln.GenerarPresupuestoxCuestionarioRes(resuelto);
                        btln.GuardarGenerico(cuestionario); 
                        tr.Confirmar();
                    }
                }
                catch (Exception ex)
                {
                    errores.Add("Linea " + i + ":" + ex.Message);
                }
            }

            return errores.ToArray();
        }
    }
}
