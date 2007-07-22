using System;
using System.Collections.Generic;
using System.Text;

using System.Text.RegularExpressions;

using Framework.DatosNegocio;
using Framework.Mensajeria.GestorMensajeriaDN;

namespace Framework.Mensajeria.GestorMails.DN
{
    [Serializable]
    public class EmailDN : EntidadTipoDN
    {
        #region Atributos

        #endregion

        #region Constructores

        public EmailDN() : base() { }

        public EmailDN(string valorMail) : base(valorMail)
        {
            string mensaje;
            if (!ValidarValor(valorMail, out mensaje))
                throw new ApplicationExceptionDN(mensaje);

            this.modificarEstado = EstadoDatosDN.SinModificar;
        }

        #endregion

        #region Propiedades

        public string ValorMail
        {
            get { return mNombre; }
        }

        #endregion

        #region Métodos Validaciones

        public bool ValidarValor(string valor, out string mensaje)
        {
            mensaje = "";
            if (!IsEmail(valor))
            {
                mensaje = "El mail no es válido";
                return false;
            }
            else
                return true;
        }

        #endregion

        #region Métodos


        public override EstadoIntegridadDN EstadoIntegridad(ref string pMensaje)
        {
            if (!ValidarValor(mNombre, out pMensaje))
                return EstadoIntegridadDN.Inconsistente;
            else
                return EstadoIntegridadDN.Consistente;
        }

        public static bool IsEmail(string address)
        {
            address = address == null ? "" : address;
            string strRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                  @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                  @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
            Regex re = new Regex(strRegex);
            if (re.IsMatch(address))
                return true;
            else
                return false;
        }       

        #endregion

    }


    [Serializable]
    public class ColEmailDN : ArrayListValidable<EmailDN>
    {



    }

}
