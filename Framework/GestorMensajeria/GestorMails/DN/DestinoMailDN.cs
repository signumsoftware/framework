using System;
using System.Collections.Generic;
using System.Text;

using Framework.DatosNegocio;
using Framework.Mensajeria.GestorMensajeriaDN;

namespace Framework.Mensajeria.GestorMails.DN
{
    [Serializable]
    public class DestinoMailDN : EntidadTipoDN, IDestinoDN
    {
        #region Atributos

        protected EmailDN mEmail;
        protected CanalMailDN mCanalMail;

        #endregion

        #region Constructores

        public DestinoMailDN() : base() { }

        public DestinoMailDN(EmailDN eMail, CanalMailDN canalMail)
        {
            string mensaje;

            if(!ValidarEmail(out mensaje,eMail))
                throw new ApplicationExceptionDN(mensaje);

            if(!ValidarCanalMail(out mensaje,canalMail))
                throw new ApplicationExceptionDN(mensaje);

            CambiarValorRef<EmailDN>(eMail, ref mEmail);
            CambiarValorRef<CanalMailDN>(canalMail, ref mCanalMail);

            mGUID = this.GetType().Name + "-" + eMail.ValorMail + "-" + canalMail.Nombre;
            //mHashValores = mGUID;
            modificarEstado = EstadoDatosDN.SinModificar;
        }

        #endregion

        #region Propiedades

        public EmailDN Email
        {
            get { return mEmail; }
        }

        public CanalMailDN CanalMail
        {
            get { return mCanalMail; }
        }

        #endregion

        #region Propiedades IDestinoDN

        public string Canal
        {
            get { return mCanalMail.Nombre; }
        }

        public string Direccion
        {
            get { return mEmail.ValorMail; }
        }

        #endregion

        #region Métodos validación

        private bool ValidarEmail(out string mensaje, EmailDN eMail)
        {
            mensaje = "";
            if (eMail == null)
            {
                mensaje = "El eMail no puede ser nulo";
                return false;
            }
            return true;
        }

        private bool ValidarCanalMail(out string mensaje, CanalMailDN canalMail)
        {
            mensaje = "";
            if (canalMail == null)
            {
                mensaje = "El canal no puede ser nulo";
                return false;
            }
            return true;
        }

        #endregion
    }

    [Serializable]
    public class ColDestinoMailDN : ArrayListValidable<DestinoMailDN>
    {


    }

}
