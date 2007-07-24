using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Framework.GestorInformes.ContenedorPlantilla.DN
{
    [Serializable]
    public class ContenedorPlantillaDN : Framework.DatosNegocio.EntidadDN
    {
        protected HuellaFicheroPlantillaDN mHuellaFichero;
        protected TipoPlantilla mTipoPlantilla;

        public HuellaFicheroPlantillaDN HuellaFichero
        {
            get { return mHuellaFichero; }
            set { CambiarValorRef<HuellaFicheroPlantillaDN>(value, ref mHuellaFichero); }
        }

        public TipoPlantilla TipoPlantilla
        {
            get { return mTipoPlantilla; }
            set { CambiarValorRef<TipoPlantilla>(value, ref mTipoPlantilla); }
        }
    }

    [Serializable()]
    public class TipoPlantilla : Framework.DatosNegocio.EntidadDN
    {

    }

    [Serializable]
    public class HuellaFicheroPlantillaDN : Framework.DatosNegocio.EntidadDN
    {
        protected string mRutaFichero;
        private Byte[] mDatos;

        public Byte[] Datos
        {
            get { return mDatos; }
        }

        public string RutaFichero
        {
            get { return mRutaFichero; }
            set { this.CambiarValorVal<string>(value,ref mRutaFichero); }
        }

        public string NombreFichero
        {
            get { return Path.GetFileName(this.mRutaFichero); }
        }

        public string ExtensionFichero
        {
            get { return Path.GetExtension(this.mRutaFichero); }
        }

        public void CargarFichero()
        {
            if (string.IsNullOrEmpty(this.mRutaFichero))
            {
                throw new ApplicationException("No se ha definido la ruta del fichero que se quiere cargar");
            }

            if (!File.Exists(this.mRutaFichero))
            {
                throw new FileNotFoundException("No se ha encontrado el fichero que se quiere cargar: " + this.mRutaFichero);
            }

            using (FileStream fs = File.OpenRead(this.mRutaFichero))
            {
                this.mDatos = new Byte[fs.Length];
                fs.Read( this.mDatos, 0, (int)fs.Length);
            }
        }
    }
}

