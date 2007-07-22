using System;
using System.Collections.Generic;
using System.Text;

using Framework.DatosNegocio;

namespace Framework.Mensajeria.GestorMails.DN
{
    [Serializable]
    public class SobreDN : EntidadDN
    {

        #region Campos

        EmailDN mEmail;
        DateTime mFechaEncolado;
        DateTime mFechaEnviado;
        DateTime mFechaReintento;

        bool mEnviado;
        bool mDescartado;
        int mReintentos;
        string mTipoMensaje;

        string mXmlMensaje;
        MensajeDN mMensaje;

        #endregion

        #region Propiedades
        public EmailDN Email
        {
            get { return mEmail; }
        }

        public DateTime FechaEncolado
        {
            get { return mFechaEncolado; }
            set { CambiarValorVal(value, ref mFechaEncolado); }
        }

        public DateTime FechaEnviado
        {
            get { return mFechaEnviado; }
            set { CambiarValorVal(value, ref mFechaEnviado); }
        }

        public DateTime FechaReintento
        {
            get { return mFechaReintento; }
            set { CambiarValorVal(value, ref mFechaReintento); }
        }

        public string XmlMensaje
        {
            get { return mXmlMensaje; }
        }

        public MensajeDN Mensaje
        {
            get { return mMensaje; }
        }

        public string TipoMensaje
        {
            get { return mTipoMensaje; }
        }

        public bool Enviado
        {
            get { return mEnviado; }
            set { CambiarValorVal(value, ref mEnviado); }
        }
        public bool Descartado
        {
            get { return mDescartado; }
            set { CambiarValorVal(value, ref mDescartado); }
        }
        public int Reintentos
        {
            get { return mReintentos; }
            set { CambiarValorVal(value, ref mReintentos); }
        }
        #endregion

        #region Constructores

        public SobreDN()
            : base()
        {
        }

        public SobreDN(EmailDN email, MensajeDN mensaje)
        {
            CambiarValorRef(email, ref mEmail);
            CambiarValorRef(mensaje, ref mMensaje);
            CambiarValorVal(false, ref mEnviado);
            CambiarValorVal(false, ref mDescartado);
            CambiarValorVal(0, ref mReintentos);
            CambiarValorVal(mensaje.GetType().ToString(), ref mTipoMensaje);
            this.modificarEstado = EstadoDatosDN.SinModificar;
        }

        #endregion

        #region Métodos

        public void Abrir()
        {
            mMensaje = MensajeDN.FromXml(mXmlMensaje);
            mXmlMensaje = null;
        }

        public void Cerrar()
        {
            mXmlMensaje = MensajeDN.ToXml(mMensaje);
            mMensaje = null;
        }

        #endregion



    }

}
