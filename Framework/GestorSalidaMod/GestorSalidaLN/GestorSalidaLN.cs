using System;
using System.Collections.Generic;
using System.Text;
using Framework.LogicaNegocios.Transacciones;
using Framework.GestorSalida.DN;
using Framework.GestorSalida.AD;
using System.IO;
using Framework.AccesoDatos.MotorAD.LN;

namespace Framework.GestorSalida.LN
{

    public class DocumentosalidaLN
    {
        static object keyLocker = new object();

        #region Insertar y Sacar Documento de la Cola de Salida

        /// <summary>
        /// Inserta el DocumentoSalida en su cola de proceso correspondiente y devuelve
        /// un ticket de identificación con el que poder referirse a él
        /// </summary>
        public string InsertarDocumentoSalidaEnCola(DocumentoSalida pDocumentoSalida)
        {
            //bloqueamos esta región de código 
            //para que las UnidadesRepositorio estén sincronizadas
            lock (keyLocker)
            {
                string ticket = pDocumentoSalida.GenerarTicket();
                using (Transaccion tr = new Transaccion())
                {
                    GuardarDocumentoSalida(pDocumentoSalida);

                    //obtenemos un repositorio de los que haya disponibles
                    UnidadRepositorioAD urAD = new UnidadRepositorioAD();
                    List<UnidadRepositorio> urDisponibles = urAD.GetNextUnidadRepositorioTemporalDisponibles();

                    if (urDisponibles == null || urDisponibles.Count == 0)
                    {
                        throw new ApplicationException("No hay espacio de almacenamiento disponible para guardar temporalmente los documentos de salida.\n\n Es necesario asignar nuevas Unidades de Repositorio para el almacenamiento temporal.");
                    }

                    Exception ultimaExcepcion;

                    //intentamos guardar el documento salida en uno de los repositorios temporales asignados
                    if (!GuardarDocumentoSalidaEnRepositorioDisponible(ref pDocumentoSalida, urDisponibles, out ultimaExcepcion))
                    {
                        if (ultimaExcepcion != null)
                        {
                            throw new ApplicationException("No se pudo copiar el documento en el repositorio temporal :" + ultimaExcepcion.Message, ultimaExcepcion);
                        }
                        throw new ApplicationException("No se pudo copiar el documento en el repositorio temporal");
                    }

                    //guardamos de nuevo el DocumentoSalida, ya que ahora su 
                    //configuracionRutaAlmacenamiento apunta a la ruta correcta
                    GuardarDocumentoSalida(pDocumentoSalida);

                    tr.Confirmar();
                }
                return ticket;
            }
        }


        /// <summary>
        /// Pone el DocumentoSalida en estado Enviado si no lo estaba, copia los archivos
        /// en una ruta persistente si procede, elimina los archivos de la ruta temporal,
        /// actualiza los datos de la configuración de ruta y guarda el DocumentoSalida.
        /// </summary>
        /// <param name="pDocumentoSalida">El Documentosalida que se quiere 
        /// sacar de la cola de salida</param>
        /// <exception>Este método no produce ninguna excepción</exception>
        public void SacarDocumentoSalidaDeCola(DocumentoSalida pDocumentoSalida)
        {
            if (pDocumentoSalida.EstadoEnvio == EstadoEnvio.Enviado && pDocumentoSalida.PersistenciaDocumento)
            {
                try
                {
                    //hay que guardar los ficheros en la unidad de persistencia final
                    lock (keyLocker)
                    {
                        //obtenemos un repositorio de los que haya disponibles
                        UnidadRepositorioAD urAD = new UnidadRepositorioAD();
                        List<UnidadRepositorio> urDisponibles = urAD.GetNextUnidadRepositorioPersistenteDisponibles();

                        if (urDisponibles != null && urDisponibles.Count > 0)
                        {
                            string oldDir = pDocumentoSalida.ConfiguracionRutaDocumento.RutaAbsoluta();
                            Exception ultimaExcepcion = null;
                            if (GuardarDocumentoSalidaEnRepositorioDisponible(ref pDocumentoSalida, urDisponibles, out ultimaExcepcion))
                            {
                                //se ha conseguido guardar en el repositorio temporal
                                pDocumentoSalida.EstadoEnvio = EstadoEnvio.Persistido;
                                //eliminamos el directorio anterior
                                BorrarDirectorioTemporal(oldDir);
                            }
                        }
                    }
                }
                //no lanzamos ningún error
                catch (Exception) { }
            }
            else
            {
                try
                {
                    //eliminamos el directorio anterior
                    BorrarDirectorioTemporal(pDocumentoSalida.ConfiguracionRutaDocumento.RutaAbsoluta());
                }
                //no lanzamos ningún error
                catch (Exception) { }
                try
                {
                    //quitamos los datos de la ruta de persistencia
                    pDocumentoSalida.ConfiguracionRutaDocumento = null;
                }
                catch (Exception) { }
            }

            if (pDocumentoSalida.EstadoEnvio == EstadoEnvio.En_Cola || pDocumentoSalida.EstadoEnvio == EstadoEnvio.En_Proceso)
            {
                pDocumentoSalida.EstadoEnvio = EstadoEnvio.Enviado;
            }
            using (Transaccion tr = new Transaccion())
            {
                GuardarDocumentoSalida(pDocumentoSalida);
                tr.Confirmar();
            }
        }


        /// <summary>
        /// Borra todos los archivos que haya en un directorio y, después,
        /// el directorio especificado
        /// </summary>
        /// <param name="dir">El directorio que se quiere borrar</param>
        private static void BorrarDirectorioTemporal(string dir)
        {
            string[] archivos = Directory.GetFiles(dir);
            foreach (string archivo in archivos)
            {
                File.Delete(Path.Combine(dir, archivo));
            }
            Directory.Delete(dir);
        }

        #region GuardarDocumentoSalida en UnidadRepositorio

        /// <summary>
        /// Guarda el DocumentoSalida en el primer UnidadRepositorio que pueda, y se apuntan los datos de almacenamiento
        /// en su propiedad ConfiguracionRutaAlmacenamiento.
        /// Si no se puede en ninguno, se devuelve la última excepción byref.
        /// Si alguno de los repositorios da un problema de espacio, éste será configurado como "Lleno" y se continuará con
        /// el siguiente disponible si lo hay
        /// </summary>
        /// <param name="pDocumentoSalida">El DocumentoSalida que se quiere guardar</param>
        /// <param name="urDisponibles">Una lista con las unidadesRepositorio en las que se puede almacenar el DocumentoSalida</param>
        /// <param name="lastException">(out) La última excepción que se ha producido en el repositorio</param>
        /// <returns>true si se ha conseguido guardar, false si no se ha sido posible</returns>
        private bool GuardarDocumentoSalidaEnRepositorioDisponible(ref DocumentoSalida pDocumentoSalida, List<UnidadRepositorio> urDisponibles, out Exception lastException)
        {
            lastException = null;
            bool correcto = false;
            using (Transaccion tr = new Transaccion())
            {
                for (int i = 0; i < urDisponibles.Count; i++)
                {
                    UnidadRepositorio ur = urDisponibles[i];
                    if (ur.EstadoRepositorio != EstadoRepositorio.Lleno)
                    {
                        try
                        {
                            //intentamos grabar en el repositorio de salida el documento
                            correcto = GuardarDocumentoEnRepositorio(ref pDocumentoSalida, ur);
                            break;
                        }
                        catch (System.IO.IOException ex)
                        {
                            if (ex.Message.Contains(" space ") || ex.Message.Contains(" espacio "))
                            {
                                //no hay espacio en la unidad repositorio
                                lastException = ex; //apuntamos la última excepción que se produjo
                                //marcamos la unidad repositorio como que no tiene suficiente espacio
                                ur.EstadoRepositorio = EstadoRepositorio.Lleno;
                                GestorInstanciacionLN gi = new GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
                                gi.Guardar(ur);
                                //ahora ejecutará otra vez el bucle con el siguiente repositorio si lo hay
                            }
                            else //se trata de otro error
                            { throw; }
                        }
                        catch (Exception)
                        { throw; }
                    }
                }
                tr.Confirmar();
            }
            return correcto;
        }

        /// <summary>
        /// Guarda un documentoSalida en una UnidadRepositorio y guarda en su propiedad ConfiguracionRutaDocumento
        /// la unidad repositorio y el directorio relativo correspondiente.
        /// </summary>
        /// <param name="docS">El DocumentoSalida que se quiere guardar</param>
        /// <param name="uRepositorio">La unidadRepositorio en la que se quiere guardar el DocumentoSalida</param>
        /// <returns></returns>
        private static bool GuardarDocumentoEnRepositorio(ref DocumentoSalida docS, UnidadRepositorio uRepositorio)
        {
            string nombreDirectorio = string.Empty;
            try
            {
                nombreDirectorio = CrearDirectorioEnRepositorio(docS, uRepositorio);
                if (docS.Documento != null)
                {
                    //si tiene cargado un documento, hay que generar el/los archivo/s
                    //en la ruta de persistencia temporal
                    GenerarDocEnRepositorioTemporal(docS, nombreDirectorio);
                }
                else
                {
                    //hay que copiar todos los archivos de la ruta temporal al nuevo
                    //directorio
                    CopiarDocEnRepositorioPersistente(docS, nombreDirectorio);
                }
            }
            catch (Exception)
            {
                //si hay un error y se ha creado el directorio temporal, lo eliminamos con lo que haya dentro
                if (!string.IsNullOrEmpty(nombreDirectorio))
                {
                    Directory.Delete(nombreDirectorio, true);
                }
                throw;
            }

            //establecemos en el DocumentoSalida la configuracionRutadocumento
            if (docS.ConfiguracionRutaDocumento == null)
            {
                docS.ConfiguracionRutaDocumento = new ConfiguracionRutaDocumento(uRepositorio, nombreDirectorio.Replace(uRepositorio.RutaFisica, string.Empty));
            }
            else
            {
                docS.ConfiguracionRutaDocumento.UnidadRepositorio = uRepositorio;
                docS.ConfiguracionRutaDocumento.RutaRelativaDocumento = nombreDirectorio.Replace(uRepositorio.RutaFisica, string.Empty);
            }
            //comprobamos el estado de UnidadRepositorio
            UnidadRepositorioLN urLN = new UnidadRepositorioLN();
            urLN.ComprobarEstado(uRepositorio, true);
            return true;
        }

        /// <summary>
        /// Copia todos los ficheros que haya en la ruta de persistencia temporal
        /// en el directorio persistente. Si hay algún error lanza una excepción
        /// </summary>
        /// <param name="docS">El DocumentoSalida cuyos archivos se van a pasar a
        /// la unidad persistente</param>
        /// <param name="nombreDirectorio">El nombre del directorio en el que se 
        /// van a copiar los ficheros</param>
        private static void CopiarDocEnRepositorioPersistente(DocumentoSalida docS, string nombreDirectorio)
        {
            string rutaOrigen = docS.ConfiguracionRutaDocumento.RutaAbsoluta();
            string[] ficherosOrigen = Directory.GetFiles(rutaOrigen);
            foreach (string fichero in ficherosOrigen)
            {
                File.Copy(Path.Combine(rutaOrigen, fichero), Path.Combine(nombreDirectorio, fichero), true);
            }
        }

        /// <summary>
        /// Escribe en fichero el documento que contiene el DcumentoSalida, o bien
        /// deszipea su contenido en él.
        /// </summary>
        /// <param name="docS">El DocumentoSalida cuyo documento se quiere guardar</param>
        /// <param name="nombreDirectorio">El nombre del directorio de persistencia temporal
        /// en el que se van a generar los documentos</param>
        private static void GenerarDocEnRepositorioTemporal(DocumentoSalida docS, string nombreDirectorio)
        {
            //comprobamos si se trata de un zip
            if (Path.GetExtension(docS.NombreFichero).ToLower() == ".zip")
            {
                Framework.GestorSalida.Utilidades.Deszipear(nombreDirectorio, docS.Documento);
            }
            else
            {
                //escribimos el documento dentro del directorio
                using (BinaryWriter w = new BinaryWriter(File.Open(nombreDirectorio, FileMode.CreateNew)))
                {
                    w.Write(docS.Documento);
                    w.Flush();
                }
            }
        }


        /// <summary>
        /// Crea el directorio en el repositorio para el documentoSalida y se lo asigna
        /// en su propiedad ConfiguracionRutaDocumento.RutaDocumento
        /// </summary>
        private static string CrearDirectorioEnRepositorio(DocumentoSalida docS, UnidadRepositorio uRepositorio)
        {
            string NombreDirectorio = Path.Combine(uRepositorio.RutaFisica, Path.GetRandomFileName());
            int i = 0;
            string nombreFinal = NombreDirectorio;
            while (Directory.Exists(nombreFinal))
            {
                i++;
                nombreFinal = NombreDirectorio + i.ToString();
            }
            Directory.CreateDirectory(nombreFinal);
            return nombreFinal;
        }

        /// <summary>
        /// Escapea los caracteres no válidos para los nombres de archivo
        /// </summary>
        /// <param name="nombrePropuesto">El valor a escapear</param>
        /// <returns></returns>
        private static string EscapearNombreArchivo(string nombrePropuesto)
        {
            string escape = string.Empty;
            return nombrePropuesto.Replace(" ", escape).Replace(":", escape).Replace("?", escape).Replace(@"\", escape).Replace(">", escape).Replace("*", escape);
        }

        #endregion

        #endregion


        /// <summary>
        /// Guarda el DocumentoSalida en la Base de Datos
        /// </summary>
        /// <returns></returns>
        private static void GuardarDocumentoSalida(DocumentoSalida pDocumentoSalida)
        {
            using (Transaccion tr = new Transaccion())
            {
                GestorInstanciacionLN gi = new GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
                gi.Guardar(pDocumentoSalida);
                tr.Confirmar();
            }
        }


        /// <summary>
        /// Recupera los primeros DocumentosSalida para un canal determinado (ordenados
        /// por la prioridad y la antigüedad)
        /// </summary>
        /// <param name="TipoCanal">El tipo de canal que se está escuchando</param>
        /// <param name="NumeroElementos">El número de DocumentosSalida que se quieren recuperar</param>
        /// <returns></returns>
        public List<DocumentoSalida> RecuperarPrimerosDocumentoSalidaPorCanal(CanalSalida TipoCanal, int NumeroElementos)
        {
            List<DocumentoSalida> lista;
            using (Transaccion tr = new Transaccion())
            {
                GestorSalida.AD.DocumentoSalidaAD ad = new DocumentoSalidaAD();
                lista = ad.RecuperarPrimerosDSPorCanal(TipoCanal, NumeroElementos);
                tr.Confirmar();
            }
            return lista;
        }



        public DocumentoSalida RecuperarDocumentoSalidaPorTicket(string ticket)
        {
            DocumentoSalida ds = null;
            using (Transaccion tr = new Transaccion())
            {
                DocumentoSalidaAD ad = new DocumentoSalidaAD();
                ds = ad.RecuperarDocumentoSalidaPorTicket(ticket);
                tr.Confirmar();
            }
            return ds;
        }


        public EstadoEnvio RecuperarEstadoEnvioPorTicket(string ticket)
        {
            EstadoEnvio ee = EstadoEnvio.Desconocido;
            using (Transaccion tr = new Transaccion())
            {
                DocumentoSalidaAD ad = new DocumentoSalidaAD();
                ee = ad.RecuperarEstadoEnvioPorTicket(ticket);
                tr.Confirmar();
            }
            return ee;
        }

    }


}
