using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace Framework.GestorSalida.Servicios
{
    public partial class GestorSalidaServicioBase : ServiceBase
    {
        private Thread Hilo;
        protected static int TiempoEspera;
        protected static ManualResetEvent semaforo = new ManualResetEvent(true);
        protected static volatile bool Apagar = false;

        #region Constructor

        public GestorSalidaServicioBase()
        {
            int.TryParse(System.Configuration.ConfigurationManager.AppSettings["TiempoEspera"],out TiempoEspera);
            InitializeComponent();
        }

        #endregion


        protected override void OnStart(string[] args)
        {
            ThreadStart start = new ThreadStart(EnviarDocumento);
            Hilo = new Thread(start);
            Hilo.Priority = ThreadPriority.Normal;
            Hilo.Start();
        }

        /// <summary>
        /// Realiza el proceso cíclico de enviar los documentos a través del canal de
        /// salida, y en el caso de que no tenga ninguno para enviar, aguarda durante
        /// el tiempo de espera determinado antes de volver a solicitarlo.
        /// 
        /// Este método trabaja en un hilo diferenciado.
        /// </summary>
        protected void EnviarDocumento()
        {
            while (!Apagar)
            {
                //esperamos el semáforo (si es que está en rojo)
                semaforo.WaitOne();
                //ejecutamos el ciclo de envío
                EjecutarEnvioDocumento();
            }
        }


        protected virtual void EjecutarEnvioDocumento()
        {
        }


        private void CerrarySalir()
        {
            CerrarHilo();
            //salimos del hilo principal del servicio
            semaforo.Close();
            this.Dispose();
        }

        private void CerrarHilo()
        {
            //decimos al hilo que se apague y esperamos a que termine
            Apagar = true;
            Hilo.Join();
        }

        protected override void OnStop()
        {
            CerrarHilo();
        }

        protected override void OnPause()
        {
            base.OnPause();
            //cerramos el semaforo
            semaforo.Reset();
        }

        protected override void OnContinue()
        {
            base.OnContinue();
            //abrimos el semaforo
            semaforo.Set();
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
            CerrarySalir();
        }


    }
}


