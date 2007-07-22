using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.GestorSalida.DN
{
    [Serializable]
    public class ConfiguracionFaxDocumentoSalidaDN : ConfiguracionDocumentoSalidaBaseDN
    {
        protected ColNumeroFax mNumerosFax;

        public ColNumeroFax NumerosFax
        {
            get { return mNumerosFax; }
            set { this.CambiarValorCol<ColNumeroFax>(value, ref mNumerosFax); }
        }
    }

    [Serializable]
    public class NumeroFax : Framework.DatosNegocio.EntidadDN
    {
        public string ValorNumeroFax
        {
            get { return base.Nombre; }
            set { base.Nombre = value; }
        }
    }

    [Serializable]
    public class ColNumeroFax : Framework.DatosNegocio.ArrayListValidable<NumeroFax>
    { }
}
