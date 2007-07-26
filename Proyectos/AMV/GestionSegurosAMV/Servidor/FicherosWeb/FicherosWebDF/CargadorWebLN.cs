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
using System.Drawing;

namespace GSAMV.DF
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
                        LineaWeb linea = ficheroWeb.Lineas[i];
                        CuestionarioResueltoDN resuelto = TraductorCuestionarios.Traducir(linea, cuestionario);
                        AdaptadorCuestionarioLN ln = new AdaptadorCuestionarioLN();

                        switch (linea.TipoProyecto)
                        {
                            case TipoProyecto.Tarificado:
                                TarifaDN tarifa = ln.GenerarTarifaxCuestionarioRes(resuelto);
                                btln.GuardarGenerico(tarifa);
                                break;
                            case TipoProyecto.Presupuestado:
                                PresupuestoDN presupuesto = ln.GenerarPresupuestoxCuestionarioRes(resuelto);
                                btln.GuardarGenerico(presupuesto);
                                break;
                            case TipoProyecto.Contratado:
                                break;
                            default:
                                break;
                        }

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
