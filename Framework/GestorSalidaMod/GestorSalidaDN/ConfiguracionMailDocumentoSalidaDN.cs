using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.GestorSalida.DN
{
    [Serializable]
    public class ConfiguracionMailDocumentoSalidaDN : ConfiguracionDocumentoSalidaBaseDN
    {
        protected Framework.Mensajeria.GestorMails.DN.ColEmailDN mColEmails;
        protected Conversion mConvertirDocumentos;

        public Framework.Mensajeria.GestorMails.DN.ColEmailDN ColEmails
        {
            get { return mColEmails; }
            set { this.CambiarValorCol<Framework.Mensajeria.GestorMails.DN.ColEmailDN>(value, ref mColEmails); }
        }

        /// <summary>
        /// Determina si los documentos se deben convertir a algún formato antes de enviarse
        /// por mail
        /// </summary>
        public Conversion ConvertirDocumentos
        {
            get { return mConvertirDocumentos; }
            set { this.CambiarValorVal<Conversion>(value, ref mConvertirDocumentos); }
        }

    }
    
    
    public enum Conversion
    {
        Ninguna = 0,
        PDF = 1,
        XPS = 3,
        MDI = 4,
        Word2003_97 = 5,
        HTML = 6,
        RTF = 7,
        Plantilla_DOT = 8,
        Plantilla_DOT_Word2003_97 = 9,
        TXT = 10,
    }


}
