using System;
using System.Collections.Generic;
using System.Text;

using Framework.DatosNegocio;

namespace Framework.Mensajeria.GestorMails.DN
{
    [Serializable]
    public class MensajeBasicoDN : MensajeDN
    {
        #region Atributos

        protected bool mIsHtml;
        protected string mBody;
        protected string mSubject;

        #endregion

        #region Constructores

        public MensajeBasicoDN() : base() { }

        public MensajeBasicoDN(string body, string subject, bool isHtml)
        {
            mBody = body;
            mSubject = subject;
            mIsHtml = isHtml;
        }

        #endregion

        #region Propiedades

        public override string Body
        {
            get { return mBody; ; }
        }

        public override string Subject
        {
            get { return mSubject; }
        }

        public override bool IsHtml
        {
            get { return mIsHtml; }
        }

        #endregion
    }
}
