using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using Framework.GestorSalida.DN;
using Framework.GestorSalida.LN;
using Framework.LogicaNegocios.Transacciones;
using Framework.AccesoDatos.MotorAD.LN;
using System.Collections;
using System.Threading;
using Framework.Colecciones;

namespace Framework.GestorSalida.Servicios
{
    public partial class ServicioGestorImpresion : GestorSalidaServicioBase
    {

        #region Atributos

        private static IRecursoLN mRecurso;
        /// <summary>
        /// proporciona todas las categorías de impresoras por su función
        /// </summary>
        private static CategoriasImpresorasPorFuncion mCategoriasImpresoras;
        /// <summary>
        /// Contiene los documentos en memoria para enviar
        /// </summary>
        private List<DocumentoSalida> mDocumentosEnCola;
        private static bool mClusterImpresoras = false;
        /// <summary>
        /// El número de errores consecutivos que debe acumular una impresora antes
        /// de pasar al pool de impresoras con error
        /// </summary>
        private static int mErroresTopeImpresora;
        private static List<ContenedorDescriptorImpresoraDN> mImpresorasError = new List<ContenedorDescriptorImpresoraDN>();
        private static int mNumeroReintentosEnvio;


        #endregion

        #region Constructor
        public ServicioGestorImpresion()
        {
            InitializeComponent();
        }
        #endregion

        #region Lanzar Servicio

        private void ObtenerRecurso()
        {
            String connectionstring;
            Dictionary<string, object> htd = new Dictionary<string, object>();

            connectionstring = "server=localhost;database=ssPruebasFT;user=sa;pwd='sa'";
            htd.Add("connectionstring", connectionstring);
            mRecurso = new Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd);

            GestorInstanciacionLN.GestorMapPersistenciaCampos = new Framework.GestorSalida.AD.MapeadoInstanciacion();
        }

        private void LevantarImpresoras()
        {
            using (CajonHiloLN c = new CajonHiloLN(mRecurso))
            {
                mCategoriasImpresoras = new GestorSalidaImpresionLN().RecuperarCategoriasOrdenadas();
            }
            Dictionary<string, string> config = Framework.Configuracion.LectorConfiguracionXML.LeerConfiguracion("ServicioGestorImpresion.xml");
            mErroresTopeImpresora = int.Parse(config["ErroresTopeImpresora"]);
            mNumeroReintentosEnvio = int.Parse(config["ReintentosEnvio"]);
            mClusterImpresoras = bool.Parse(config["ClusterImpresoras"]);
        }


        private void LanzarServicio()
        {
            //levantamos los elementos necesarios para el servicio y luego lanzamos el hilo
            //de envío de documentos
            ObtenerRecurso();
            LevantarImpresoras();
            base.OnStart(null);
        }



        #endregion


        #region Imprimir Documentos

        /// <summary>
        /// Obtiene los documentos que hay que enviar. Si los hay, los imprime en un
        /// hilo para cada uno y sale de la ejecución. Si no hay, aguarda el tiempo de espera especificado y sale
        /// de la ejecución.
        /// </summary>
        protected override void EjecutarEnvioDocumento()
        {
            //recuperamos los 5 primeros documentos a enviar
            CargarDocumentosEnCola();
            //si no tenemos trabajo que hacer, esperamos el tiempo indicado
            if (mDocumentosEnCola == null || mDocumentosEnCola.Count == 0)
            {
                Thread.Sleep(TiempoEspera);
            }
            else
            {
                //tenemos que procesar los documentos que hay en la cola
                foreach (DocumentoSalida documento in mDocumentosEnCola)
                {
                    //lanzamos cada impresión de documento en un hilo diferente para
                    //que no haya bloqueos ni tiempos de espera
                    Thread hiloImpresion = new Thread(new ParameterizedThreadStart(ImprimirDocumento));
                    hiloImpresion.Start(documento);
                }
                //limpiamos los documentos de la cola
                mDocumentosEnCola.Clear();
            }
        }

        /// <summary>
        /// Carga los primeros documentos disponibles en la cola de envío
        /// </summary>
        private void CargarDocumentosEnCola()
        {
            using (CajonHiloLN c = new CajonHiloLN(mRecurso))
            {
                DocumentosalidaLN ln = new DocumentosalidaLN();
                mDocumentosEnCola = ln.RecuperarPrimerosDocumentoSalidaPorCanal(CanalSalida.impresora, 5);
            }
        }

        /// <summary>
        /// Se encarga de decidir a qué impresora se envía el documento, así como de
        /// guardar el estado del documento antes y después de la impresión
        /// </summary>
        /// <param name="oDocumento"></param>
        private void ImprimirDocumento(object oDocumento)
        {
            DocumentoSalida documento = (DocumentoSalida)oDocumento;
            ConfiguracionImpresionDocumentoSalidaDN config = (ConfiguracionImpresionDocumentoSalidaDN)documento.ConfiguracionDocumentosalida;
            string[] archivosRelacionados = documento.ObtenerFicheros();

            //comprobamos que haya archivos para mandar
            if (archivosRelacionados == null || archivosRelacionados.Length == 0)
            {
                ErrorEnviandoDocumento(documento, "El documento no contiene archivos que enviar.");
            }

            documento.EstadoEnvio = EstadoEnvio.En_Proceso;
            GuardarDocumento(documento);

            Exception excepcion = null;
            ContenedorDescriptorImpresoraDN impresora = null;
            try
            {
                if (!mClusterImpresoras)
                {
                    //asignamos una impresora para todas las copias
                    impresora = RecuperarImpresoraLibre(config.FuncionImpresora);
                }
                for (int i = 0; i < config.NumeroCopias; i++)
                {
                    //si no hay impresora la seleccionamos (para que se comporte como cluster de impresoras)
                    if (impresora == null) { impresora = RecuperarImpresoraLibre(config.FuncionImpresora); }
                    //enviamos cada fichero a imprimir a la impresora
                    foreach (string fichero in archivosRelacionados)
                    {
                        string rutaFichero = System.IO.Path.Combine(documento.ConfiguracionRutaDocumento.RutaAbsoluta(), fichero);
                        excepcion = EjecutarTrabajoImpresion(documento, rutaFichero, 1, impresora);
                        if (excepcion != null && excepcion.GetType() != typeof(System.ServiceProcess.TimeoutException)) { throw excepcion; }
                        documento.ConfiguracionDocumentosalida.CopiasRealizadasTotales += 1;
                    }
                    documento.ConfiguracionDocumentosalida.CopiasRealizadas += 1;
                }
            }
            catch (Exception ex)
            {
                //apuntamos el error en el documento
                ErrorEnviandoDocumento(documento, ex.Message);
                //apuntamos el error en la impresora
                ErrorEnImpresora(impresora);
            }
            if (excepcion != null)
            {
                documento.EstadoEnvio = EstadoEnvio.Enviado;
                GuardarDocumento(documento);
                using (CajonHiloLN c = new CajonHiloLN(mRecurso))
                {
                    DocumentosalidaLN ln = new DocumentosalidaLN();
                    ln.SacarDocumentoSalidaDeCola(documento);
                }
            }
            //termina aquí y sale del hilo de ejecución
        }




        /// <summary>
        /// Envía el documento al Agente de Impresión, aumentando y reduciendo el número
        /// de trabajos en cola de la impresora, y devolviendo la excepción que se haya
        /// producido si es oportuno
        /// </summary>
        private Exception EjecutarTrabajoImpresion(DocumentoSalida documento, string rutaFichero, int numCopias, ContenedorDescriptorImpresoraDN impresora)
        {
            Exception excepcionProducida = null;
            Framework.AgenteImpresion.AgenteImpresion ai = new Framework.AgenteImpresion.AgenteImpresion();
            Framework.AgenteImpresion.TipoSincronizacion sinc = Framework.AgenteImpresion.TipoSincronizacion.Sincrona_PrinterStatus;

            //para aumentar y reducir el trabajo de la impresora de forma desatendida
            using (new TrabajoImpresora(impresora))
            {
                try
                {
                    switch (System.IO.Path.GetExtension(rutaFichero))
                    {
                        case ".doc":
                        case ".docx":
                            //hay que imprimirlo usando interop
                            ai.ImprimirDocumento(rutaFichero, impresora.Nombre, numCopias, sinc, 0, string.Empty);
                            break;
                        default:
                            //hay que imprimirlo utilizando el shell del sistema
                            ai.ImprimirCualquierDocumento(rutaFichero, impresora.Nombre, numCopias, sinc, 0);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    //apuntamos el error que se ha producido
                    excepcionProducida = ex;
                }
            }
            return excepcionProducida;
        }


        /// <summary>
        /// Aumenta y reduce el número de trabajos en cola de la impresora
        /// </summary>
        internal class TrabajoImpresora : IDisposable
        {
            private ContenedorDescriptorImpresoraDN mImpresora;

            public TrabajoImpresora(ContenedorDescriptorImpresoraDN impresora)
            {
                mImpresora = impresora;
                lock (mImpresora)
                {
                    mImpresora.TrabajosEnCola += 1;
                }
            }

            #region IDisposable Members

            public void Dispose()
            {
                lock (mImpresora)
                {
                    mImpresora.TrabajosEnCola -= 1;
                }
            }
            #endregion
        }


        /// <summary>
        /// Controla de manera genérica el estado del documento cuando se ha producido
        /// un error en la impresión. Aumenta el número de errores en uno y, si corresponde,
        /// establece el estado en error, y a continuación guarda el documentosalida
        /// </summary>
        /// <param name="documento"></param>
        /// <param name="textoError"></param>
        private void ErrorEnviandoDocumento(DocumentoSalida documento, string textoError)
        {
            documento.Error = textoError;
            documento.IntentosEnvio += 1;
            if (documento.IntentosEnvio >= mNumeroReintentosEnvio) { documento.EstadoEnvio = EstadoEnvio.Error; }
            GuardarDocumento(documento);
        }

        /// <summary>
        /// Apunta el error en la impresora y, si supera el tope establecido
        /// en la configuración, la mete en el listado de impresoras con error
        /// </summary>
        /// <param name="impresora"></param>
        private void ErrorEnImpresora(ContenedorDescriptorImpresoraDN impresora)
        {
            lock (impresora)
            {
                impresora.Errores += 1;
                if (impresora.Errores > mErroresTopeImpresora) mImpresorasError.Add(impresora);
            }
        }

        /// <summary>
        /// Recupera la 1ª impresora que haya libre (o con menos trabajo) para una categoría
        /// </summary>
        /// <param name="funcionImpresora"></param>
        /// <returns></returns>
        private ContenedorDescriptorImpresoraDN RecuperarImpresoraLibre(FuncionImpresora funcionImpresora)
        {
            //comprobamos que la función que nos han pasado está en la colección
            if (!mCategoriasImpresoras.ContainsKey(funcionImpresora))
            {
                throw new ApplicationException("La función que se ha especificado para la impresión del documento no existe en la función de impresoras disponibles (" + funcionImpresora + ")");
            }

            //obtenemos las categorias asignadas a esta funcion
            PriorityQueue<CategoriaImpresoras> categorias = mCategoriasImpresoras[funcionImpresora];

            //comprobamos si no hay impresoras para la función especificada
            if (categorias.Count == 0)
            {
                throw new ApplicationException("No hay ninguna impresora asignada a la categoría para la función especificada (" + funcionImpresora + ")");
            }

            //obtenemos la 1ª categoría (la que menos trabajos tiene y no está en estado error)
            CategoriaImpresoras cat = categorias.Peek();
            ContenedorDescriptorImpresoraDN impresora = cat.ImpresorasPorTrabajos.Peek();

            if (impresora.Errores >= mErroresTopeImpresora) throw new ApplicationException("No hay impresoras sin errores disponibles");
            if (impresora == null) throw new ApplicationException("No hay ninguna impresora disponible o sin estado de error para imprimir el documento");

            return impresora;
        }


        /// <summary>
        /// Guarda el Documentosalida en la Base de Datos
        /// </summary>
        /// <param name="documento"></param>
        private void GuardarDocumento(DocumentoSalida documento)
        {
            using (CajonHiloLN c = new CajonHiloLN(mRecurso))
            {
                using (Transaccion t = new Transaccion())
                {
                    GestorInstanciacionLN gi;
                    gi = new GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual);
                    gi.Guardar(documento);
                    t.Confirmar();
                }
            }
        }

        #endregion


        #region Recuperador errores impresora

        /// <summary>
        /// Mantiene un bucle en el que intenta reparar las impresoras
        /// que se encuentran en estado error
        /// </summary>
        private void RecuperadorErrores()
        {
            while (!Apagar)
            {
                //esperamos el semáforo (si es que está en rojo)
                semaforo.WaitOne();
                //ejecutamos el ciclo de recuperación
                for (int i = 0; i < mImpresorasError.Count; i++)
                {
                    ((ContenedorDescriptorImpresoraDN)mImpresorasError[i]).Errores -= 1;
                    mImpresorasError.RemoveAt(i);
                }
                Thread.Sleep(TiempoEspera);
            }
        }

        #endregion


        #region Métodos Sobrescritos Servicio

        protected override void OnStart(string[] args)
        {
            //hilo general de ejecuciones de envío
            new Thread(new ThreadStart(LanzarServicio)).Start();
            //hilo de recuperación de errores
            new Thread(new ThreadStart(RecuperadorErrores)).Start();
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
        }

        #endregion



    }
}
