using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Mail;

using Framework.LogicaNegocios.Transacciones;
using Framework.AccesoDatos.MotorAD.LN;
using Framework.Configuracion;

using Framework.Mensajeria.GestorMensajeriaDN;
using Framework.Mensajeria.GestorMails.Properties;
using Framework.Mensajeria.GestorMails.DN;

namespace Framework.Mensajeria.GestorMails.LN
{
    class EnviadorMailsLN : BaseTransaccionLN
    {
     
        static TimeSpan[] esperaReintento;

        #region Arranque Estático

        static EnviadorMailsLN()
        {
            ParsearReintentos();
    

        }

        /// <summary>
        /// Coje una cadena con el formato "1; 5; 40" y lo mete en un array de TimeSpan 
        /// </summary>
        static void ParsearReintentos()
        {
            string strReintentos = AppConfiguracion.DatosConfig["EsperaReintentosSegsPYComa"].ToString();
            string[] splReintentos = strReintentos.Split(';');
            esperaReintento = new TimeSpan[splReintentos.Length];
            for (int i = 0; i < splReintentos.Length; i++)
            {
                int ts = int.Parse(splReintentos[i]);
                esperaReintento[i] = new TimeSpan(0, 0, ts);
            }
        }
        #endregion

        public EnviadorMailsLN(ITransaccionLogicaLN pTL, IRecursoLN pRec)
            : base(pTL, pRec)
        {

        }


        public Boolean  EnviarSiguienteMensaje(SmtpClient client)
        {

            ITransaccionLogicaLN tlproc = this.ObtenerTransaccionDeProceso();

            try
            {
                SobreDN s;

                CorreoLN correoln = new CorreoLN(tlproc, mRec);
                s = correoln.RecuperarSiguienteAEnviar();
                System.Diagnostics.Debug.WriteLine("para enviar:" + DateTime.Now.ToString());
                if (s != null)
                {
                    System.Diagnostics.Debug.WriteLine("para enviar:" + s.ID.ToString());
                    try
                    {
                        EnviarMensaje(client, s);
                        s.Enviado = true;
                        s.FechaEnviado = DateTime.Now;
//                        correoln = new CorreoLN(tlproc, mRec);
                        correoln.Guardar(s);
                    }
                    catch (SmtpFailedRecipientException ex)
                    {
                        if (s.Reintentos < esperaReintento.Length)
                        {
                            s.FechaReintento += esperaReintento[s.Reintentos];
                            s.Reintentos++;
                        }
                        else
                        {
                            s.Descartado = true;
                        }
                        correoln.Guardar(s);
                    }


                    tlproc.Confirmar();
                    return true ;
                }
                else
                {
                    tlproc.Confirmar();
                    return false;
                    //  Thread.Sleep(intervaloEspera);
                }
               

            }
            catch (Exception ex)
            {
                tlproc.Cancelar();
                return false;
            }
        }

        private void EnviarMensaje(SmtpClient client, SobreDN s)
        {
            MensajeDN m = s.Mensaje;
            MailMessage mail = new MailMessage();
            mail.To.Add(s.Email.ValorMail);
            mail.From = new MailAddress(AppConfiguracion.DatosConfig["SmtpAddress"].ToString());
            mail.Body = m.Body;
            mail.Subject = m.Subject;
            mail.IsBodyHtml = m.IsHtml;
            mail.BodyEncoding = mail.SubjectEncoding = Encoding.GetEncoding("iso-8859-1");

            client.Send(mail);
        }
    }
}
