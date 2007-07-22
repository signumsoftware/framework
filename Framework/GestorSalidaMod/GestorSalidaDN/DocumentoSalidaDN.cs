using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.GestorSalida.DN
{
    [Serializable]
    public class DocumentoSalida : Framework.DatosNegocio.EntidadDN
    {
        #region Atributos

        protected Byte[] mDocumento;
        protected string mNombreFichero;
        protected DateTime mFechaCreacion;
        protected DateTime mFechaFinEnvio;
        protected EstadoEnvio mEstadoEnvio;
        protected int mPrioridad;
        protected CanalSalida mCanalSalida;
        protected IConfiguracionDocumentoSalida mConfiguracionDocumentosalida;
        protected string mTicket;
        protected bool mPersistenciaDocumento;
        protected ConfiguracionRutaDocumento mConfiguracionRutaDocumento;
        /// <summary>
        /// El texto del error que se provocó en el documento
        /// </summary>
        protected string mError;
        /// <summary>
        /// Los intentos de envío que se han producido sobre este DocumentoSalida
        /// </summary>
        protected int mIntentosEnvio;

        #endregion

        #region Propiedades

        public int IntentosEnvio
        {
            get { return mIntentosEnvio; }
            set { CambiarValorVal<int>(value, ref mIntentosEnvio); }
        }

        public string Error
        {
            get { return mError; }
            set { CambiarValorVal<string>(value, ref mError); }
        }


        public ConfiguracionRutaDocumento ConfiguracionRutaDocumento
        {
            get { return mConfiguracionRutaDocumento; }
            set { CambiarValorRef<ConfiguracionRutaDocumento>(value, ref mConfiguracionRutaDocumento); }
        }

        public bool PersistenciaDocumento
        {
            get { return mPersistenciaDocumento; }
            set { this.CambiarValorVal<bool>(value, ref mPersistenciaDocumento); }
        }

        public string Ticket
        { get { return mTicket; } }

        public IConfiguracionDocumentoSalida ConfiguracionDocumentosalida
        {
            get { return mConfiguracionDocumentosalida; }
            set { this.CambiarValorRef<IConfiguracionDocumentoSalida>(value, ref mConfiguracionDocumentosalida); }
        }

        public Byte[] Documento
        {
            get { return mDocumento; }
            set
            {
                if (!ByteArraysIguales(value, mDocumento))
                {
                    mDocumento = value;
                    this.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Modificado;
                }
            }
        }

        public bool ByteArraysIguales(Byte[] array1, Byte[] array2)
        {
            if (array1 == array2)
                return true;
            if ((array1 == null) != (array2 == null))
                return false;
            if (array1.Length != array2.Length)
                return false;
            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                    return false;
            }
            return true;
        }

        public string NombreFichero
        {
            get { return mNombreFichero; }
            set { this.CambiarValorVal<string>(value, ref mNombreFichero); }
        }

        public DateTime FechaCreacion
        {
            get { return mFechaCreacion; }
            set { this.CambiarValorVal<DateTime>(value, ref mFechaCreacion); }
        }

        public DateTime FechaFinEnvio
        {
            get { return mFechaFinEnvio; }
            set { this.CambiarValorVal<DateTime>(value, ref mFechaFinEnvio); }
        }

        public EstadoEnvio EstadoEnvio
        {
            get { return mEstadoEnvio; }
            set { this.CambiarValorVal<EstadoEnvio>(value, ref mEstadoEnvio); }
        }

        //Define la prioridad, de manera ascendente (a mayor valor, más prioritario)
        public int Prioridad
        {
            get { return mPrioridad; }
            set { this.CambiarValorVal<int>(value, ref mPrioridad); }
        }

        public CanalSalida CanalSalida
        {
            get { return mCanalSalida; }
            set { this.CambiarValorRef<CanalSalida>(value, ref mCanalSalida); }
        }

        #endregion

        #region Métodos

        public string GenerarTicket()
        {
            this.mTicket = System.Guid.NewGuid().ToString();
            return this.mTicket;
        }

        /// <summary>
        /// Devuelve una lista con los nombres de los ficheros asociados a este DocumentoSalida
        /// </summary>
        public string[] ObtenerFicheros()
        {
            if (this.mConfiguracionRutaDocumento != null)
            {
                return System.IO.Directory.GetFiles(this.mConfiguracionRutaDocumento.RutaAbsoluta());
            }
            return null;
        }

        #endregion

        #region validación

        public bool Estadoconsistente(ref string Mensaje)
        {
            if (this.mConfiguracionDocumentosalida == null)
            {
                Mensaje = "No se ha establecido la configuración del documento de salida";
                return false;
            }
            switch (this.CanalSalida)
            {
                case CanalSalida.email:
                    if (!(this.mConfiguracionDocumentosalida.GetType() == typeof(ConfiguracionMailDocumentoSalidaDN)))
                    {
                        Mensaje = "El canal de salida no coincide con los datos de configuración establecidos";
                        return false;
                    }
                    break;
                case CanalSalida.impresora:
                    if (!(this.mConfiguracionDocumentosalida.GetType() == typeof(ConfiguracionImpresionDocumentoSalidaDN)))
                    {
                        Mensaje = "El canal de salida no coincide con los datos de configuración establecidos";
                        return false;
                    }
                    break;
                case CanalSalida.fax:
                    if (!(this.mConfiguracionDocumentosalida.GetType() == typeof(ConfiguracionFaxDocumentoSalidaDN)))
                    {
                        Mensaje = "El canal de salida no coincide con los datos de configuración establecidos";
                        return false;
                    }
                    break;
                case CanalSalida.ftp:
                    throw new NotImplementedException("El canal de salida FTP no está implementado");
                case CanalSalida.Servicio_Web:
                    throw new NotImplementedException("El canal de salida ServicioWeb no está implementado");
                default:
                    Mensaje = "No se ha definido un tipo de canal apropiado";
                    return false;
                    break;
            }
            Mensaje = string.Empty;
            return true;
        }

        #endregion
    }



    /// <summary>
    /// Define el tipo de canal por el que debe salir el documento (impresora, fax, mail...)
    /// </summary>
    public enum CanalSalida
    {
        indefinido = 0,
        email = 1,
        impresora = 2,
        fax = 3,
        ftp = 4,
        Servicio_Web = 5
    }


    public enum EstadoEnvio
    {
        Desconocido = 0,
        En_Cola = 1,
        En_Proceso = 2,
        Enviado = 3,
        Persistido = 4,
        Error = 5
    }


    public interface IConfiguracionDocumentoSalida
    {
        /// <summary>
        /// El número de copias que se quieren enviar
        /// </summary>
        int NumeroCopias { get;set;}
        /// <summary>
        /// Las copias del Documento Salida que ya se han enviado 
        /// </summary>
        int CopiasRealizadas { get;set;}
        /// <summary>
        /// Las copias que se han enviado por cada uno de los ficheros que contiene
        /// el Documento Salida (con propósito estadístico)
        /// </summary>
        int CopiasRealizadasTotales { get;set;}
    }


    [Serializable]
    public class ConfiguracionDocumentoSalidaBaseDN : Framework.DatosNegocio.EntidadDN, IConfiguracionDocumentoSalida
    {
        protected int mNumerocopias;
        protected int mCopiasRealizadas;
        protected int mNumeroCopiasTotales;
        protected int mCopiasRealizadasTotales;


        #region IConfiguracionDocumentoSalida Members


        public int NumeroCopias
        {
            get { return this.mNumerocopias; }
            set { this.CambiarValorVal<int>(value, ref this.mNumerocopias); }
        }

        public int CopiasRealizadas
        {
            get { return this.mCopiasRealizadas; }
            set { this.CambiarValorVal<int>(value, ref this.mCopiasRealizadas); }
        }

        public int NumeroCopiasTotales
        {
            get { return this.mNumeroCopiasTotales; }
            set { this.CambiarValorVal<int>(value, ref this.mNumeroCopiasTotales); }
        }

        public int CopiasRealizadasTotales
        {
            get { return this.mCopiasRealizadasTotales; }
            set { this.CambiarValorVal<int>(value, ref this.mCopiasRealizadasTotales); }
        }


        #endregion

    }


    [Serializable]
    public class ConfiguracionRutaDocumento : Framework.DatosNegocio.EntidadDN
    {
        protected UnidadRepositorio mUnidadRepositorio;
        protected string mRutaRelativaDocumento;


        public ConfiguracionRutaDocumento()
        { }

        public ConfiguracionRutaDocumento(UnidadRepositorio uRepositorio, string RutaDocumento)
        {
            this.UnidadRepositorio = uRepositorio;
            this.RutaRelativaDocumento = RutaDocumento;
        }

        public string RutaRelativaDocumento
        {
            get { return mRutaRelativaDocumento; }
            set { CambiarValorVal<string>(value, ref mRutaRelativaDocumento); }
        }

        public UnidadRepositorio UnidadRepositorio
        {
            get { return mUnidadRepositorio; }
            set { CambiarValorRef<UnidadRepositorio>(value, ref mUnidadRepositorio); }
        }

        public string RutaAbsoluta()
        {
            string respuesta = string.Empty;
            if (this.mUnidadRepositorio != null && !string.IsNullOrEmpty(this.mRutaRelativaDocumento))
            {
                respuesta = System.IO.Path.Combine(this.mUnidadRepositorio.RutaFisica, this.mRutaRelativaDocumento);
            }
            return respuesta;
        }

    }


    [Serializable]
    public class UnidadRepositorio : Framework.DatosNegocio.EntidadDN
    {
        protected TipoRepositorio mTiporepositorio;
        protected EstadoRepositorio mEstadoRepositorio;
        protected string mRutaFisica;

        public string RutaFisica
        {
            get { return mRutaFisica; }
            set { CambiarValorVal<string>(value, ref mRutaFisica); }
        }

        public TipoRepositorio Tiporepositorio
        {
            get { return mTiporepositorio; }
            set { CambiarValorVal<TipoRepositorio>(value, ref mTiporepositorio); }
        }
        public EstadoRepositorio EstadoRepositorio
        {
            get { return mEstadoRepositorio; }
            set { CambiarValorVal<EstadoRepositorio>(value, ref mEstadoRepositorio); }
        }
    }


    public enum EstadoRepositorio
    {
        Disponible = 0,
        Medio = 1,
        Lleno = 2
    }


    public enum TipoRepositorio
    {
        Temporal = 0,
        Persistente = 1
    }
}
