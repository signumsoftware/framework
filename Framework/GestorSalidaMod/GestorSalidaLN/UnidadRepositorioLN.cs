using System;
using System.Collections.Generic;
using System.Text;
using Framework.GestorSalida.DN;
using System.IO;
using Framework.AccesoDatos.MotorAD.LN;
using Framework.LogicaNegocios.Transacciones;


namespace Framework.GestorSalida.LN
{
    public class UnidadRepositorioLN
    {
        static object keyLocker = new object();

        public void ComprobarEstado(UnidadRepositorio ur, bool IgnorarErrores)
        {
            //bloqueamos esta región de código a través del mismo objeto para
            //hacer que las UnidadesRepositorio se mantengan sincronizados
            lock (keyLocker)
            {
                try
                {
                    //si se trata de una carpeta de red, no podemos acceder
                    if (!ur.RutaFisica.StartsWith(@"\\"))
                    {
                        string unidad = ur.RutaFisica.Substring(0, 1);
                        DriveInfo info = new DriveInfo(unidad);
                        long espacioDisponible = info.AvailableFreeSpace;
                        long espacioTotal = info.TotalSize;

                        int disponibleX = (int)((espacioTotal * espacioDisponible) / 100);

                        EstadoRepositorio estado = disponibleX <= 50 ? EstadoRepositorio.Medio : EstadoRepositorio.Disponible;

                        if (ur.EstadoRepositorio != estado)
                        {
                            //hay que modificar el ur
                            ur.EstadoRepositorio = estado;
                            using (Transaccion tr = new Transaccion())
                            {
                                GestorInstanciacionLN gi = new GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
                                gi.Guardar(ur);
                                tr.Confirmar();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    if (!IgnorarErrores) { throw; }
                }
            }

        }

    }
}
