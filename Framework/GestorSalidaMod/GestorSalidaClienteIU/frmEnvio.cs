using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Framework.GestorSalida.ClienteIU.controladoresForm;
using Framework.GestorSalida.DN;
using System.Collections;

namespace Framework.GestorSalida.ClienteIU
{
    public partial class frmEnvio : MotorIU.FormulariosP.FormularioBase
    {
        private frmEnvioCtrl mControlador;
        private PaqueteEnvioDocumentoSalida miPaquete;

        public frmEnvio()
        {
            InitializeComponent();
        }

        public override void Inicializar()
        {
            base.Inicializar();

            mControlador = (frmEnvioCtrl)Controlador;
            miPaquete = Paquete != null && Paquete.Contains("Paquete") ? (PaqueteEnvioDocumentoSalida)Paquete["Paquete"] : null;
            if (miPaquete == null)
            {
                throw new ApplicationException("No se ha recibido el paquete con los datos del documento de salida");
            }
        }

        private void frmEnvio_Shown(object sender, EventArgs e)
        {
            try
            {

                //comprimimos los archivos en un zip
                string nombreArchivo = "archivo.zip";
                Byte[] archivoZip = mControlador.ComprimirArchivos(miPaquete.ListaFicheros);

                //generamos el DocumentoSalida
                DocumentoSalida docS = new DocumentoSalida();
                docS.Documento = archivoZip;
                docS.NombreFichero = nombreArchivo;
                docS.PersistenciaDocumento = miPaquete.Peristente;
                docS.Prioridad = miPaquete.Prioridad;
                docS.FechaCreacion = DateTime.Now;
                docS.CanalSalida = miPaquete.CanalSalida;

                switch (miPaquete.CanalSalida)
                {
                    case CanalSalida.indefinido:
                        throw new ApplicationException("El canal de salida no puede ser indefinido");
                    case CanalSalida.email:
                        CrearConfiguracionEmail(docS, miPaquete);
                        break;
                    case CanalSalida.impresora:
                        CrearConfiguracionImpresora(docS, miPaquete);
                        break;
                    case CanalSalida.fax:
                        CrearConfiguracionFax(docS, miPaquete);
                        break;
                    default:
                        throw new ApplicationException("El canal de salida seleccionado no es correcto");
                }

                //enviamos el documento salida al gestor de salida
                miPaquete.Ticket = mControlador.InsertarDocumentoSalidaEnCola(docS);

                if (miPaquete.MostrarTicket)
                {
                    this.ControlBox = true;
                    this.loadingCircle1.Active = false;
                    this.loadingCircle1.Visible = false;
                    label1.Text = "Ticket de identificación del Documento:";
                    txtTicket.Text = miPaquete.Ticket;
                    txtTicket.Visible = true;
                    cMarco.MostrarInformacion("Los documentos han sido enviados al Gestor de Salida.\r\n\r\nPara salir cierre esta ventana.", "Envío completado");
                    this.BringToFront();
                }
                else
                {
                    cMarco.MostrarInformacion("Los documentos han sido enviados al Gestor de Salida.", "Envío completado");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MostrarError(ex);
                Hashtable ht = miPaquete.GenerarPaquete();
                cMarco.Navegar("EnvioConfiguracionImpresion", this, MotorIU.Motor.TipoNavegacion.CerrarLanzador, ref ht);
            }
        }

        private void CrearConfiguracionFax(DocumentoSalida docS, PaqueteEnvioDocumentoSalida miPaquete)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        private void CrearConfiguracionEmail(DocumentoSalida docS, PaqueteEnvioDocumentoSalida miPaquete)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        private void CrearConfiguracionImpresora(DocumentoSalida docS, PaqueteEnvioDocumentoSalida miPaquete)
        {
            ConfiguracionImpresionDocumentoSalidaDN conf = new ConfiguracionImpresionDocumentoSalidaDN();
            conf.FuncionImpresora = miPaquete.FuncionImpresora;
            conf.NumeroCopias = miPaquete.NumeroCopias;
            docS.ConfiguracionDocumentosalida = conf;
        }
    }
}