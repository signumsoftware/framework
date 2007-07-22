using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using System.Threading;
using System.Net;
using System.Net.Mime;

using Framework.LogicaNegocios.Transacciones;
using Framework.Configuracion;

using Framework.Mensajeria.GestorMails.LN;
using Framework.Mensajeria.GestorMensajeriaDN;
using Framework.Mensajeria.GestorMails;
using Framework.Mensajeria.GestorMails.Properties;

namespace Framework.Mensajeria.GestorMails
{
    public class DespachadorMails
    {
        static int intervaloEspera;
        public static readonly string Clave = "despachador";
        RecursoLN recurso;
        bool running = true;

        Thread thread;

        public Thread Thread
        {
            get { return thread; }
        }

        /// <summary>
        /// Constuye y configura el despachador
        /// </summary>
        /// <param name="intervaloEspera">en milisegundos</param>
        public DespachadorMails()
        {


            int tiempoEspera;


            if (!int.TryParse(Framework.Configuracion.AppConfiguracion.DatosConfig["EsperaInactivoSegs"].ToString(), out tiempoEspera))
            {

                tiempoEspera = 4;
            }

            if (tiempoEspera == 0)
            {
                tiempoEspera = 10;
            }

            intervaloEspera = tiempoEspera * 1000;
         

            recurso = AppConfiguracion.DatosConfig["recurso"] as RecursoLN;
            thread = new Thread(Despachar);
        }



        public void Start()
        {
            if (!thread.IsAlive)
                thread.Start();
        }

        public void Stop()
        {
            running = false;
        }


        public void Despachar()
        {
            SmtpClient client = DameSmtpClient();
            while (running )
            {
                //&&  (Boolean)AppConfiguracion.DatosConfig["EnviarMail"]==true
                System.Diagnostics.Debug.WriteLine ("llamada" + DateTime.Now.ToString()  );
                EnviadorMailsLN enviador = new EnviadorMailsLN(null, recurso);
              bool resultado=  enviador.EnviarSiguienteMensaje(client);
              bool enviarmail = false;

              try
              {
                if (AppConfiguracion.DatosConfig.ContainsKey("EnviarMail"))
              {
                  if (AppConfiguracion.DatosConfig["EnviarMail"].ToString().ToLower() == "true")
                      enviarmail = true;
              }
              }
              catch (Exception  ex)
              {
                  System.Diagnostics.Debug.WriteLine("error:" + DateTime.Now.ToString() + ex.Message);
                               }

           

              if (!resultado || !enviarmail)
              {
                  Thread.Sleep(intervaloEspera);
              }
            }
        }

        private SmtpClient DameSmtpClient()
        {
            SmtpClient client = new SmtpClient(AppConfiguracion.DatosConfig["SmtpHost"].ToString());
            client.Credentials = new NetworkCredential(
                          AppConfiguracion.DatosConfig["SmtpLogin"].ToString(),
                          AppConfiguracion.DatosConfig["SmtpPassword"].ToString());
            return client;
        }


        public ThreadState Estado
        {
            get { return thread.ThreadState; }
        }
    }



}