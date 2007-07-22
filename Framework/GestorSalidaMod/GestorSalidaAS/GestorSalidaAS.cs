using System;
using System.Collections.Generic;
using System.Text;
using Framework.GestorSalida.DN;
using GestorSalidaAS.GestorSalidaWS;

namespace Framework.GestorSalida.AS
{
    public class GestorSalidaAS : Framework.AS.BaseAS
    {
        public string EnviarDocumentoSalida(DocumentoSalida pDocumentoSalida)
        {
            GestorSalidaWS servicio = new GestorSalidaWS();
            servicio.Url = RedireccionURL(servicio.Url);
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC;

            Byte[] paquete = Framework.Utilidades.Serializador.Serializar(pDocumentoSalida);

            return servicio.EnviarDocumentoSalida(paquete);
        }


        public DocumentoSalida RecuperarDocumentoSalidaPorTicket(string ticket)
        {
            GestorSalidaWS servicio = new GestorSalidaWS();
            servicio.Url = RedireccionURL(servicio.Url);
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC;
            Byte[] paquete = servicio.RecuperarDocumentoSalidaPorTicket(ticket);
            return (DocumentoSalida)Utilidades.Serializador.DesSerializar(paquete);
        }


        public Framework.GestorSalida.DN.EstadoEnvio RecuperarEstadoEnvioPorTicket(string ticket)
        {
            GestorSalidaWS servicio = new GestorSalidaWS();
            servicio.Url = RedireccionURL(servicio.Url);
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC;
            return (Framework.GestorSalida.DN.EstadoEnvio)servicio.RecuperarEstadoEnvioPorTicket(ticket);
        }

        public ContenedorDescriptorImpresoraDN BajaContenedorDescriptorImpresora(ContenedorDescriptorImpresoraDN impresora)
        { 
            GestorSalidaWS servicio = new GestorSalidaWS();
            servicio.Url = RedireccionURL(servicio.Url);
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC;
            byte[] arr = Framework.Utilidades.Serializador.Serializar(impresora);
            byte[] resp = servicio.BajaContenedorDescriptorImpresora(arr);
            return (ContenedorDescriptorImpresoraDN)Utilidades.Serializador.DesSerializar(resp);
        }

        public ContenedorDescriptorImpresoraDN AltaContenedorDescriptorImpresora(ContenedorDescriptorImpresoraDN impresora)
        { 
            GestorSalidaWS servicio = new GestorSalidaWS();
            servicio.Url = RedireccionURL(servicio.Url);
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC;
            byte[] arr = Framework.Utilidades.Serializador.Serializar(impresora);
            byte[] resp = servicio.AltaContenedorDescriptorImpresora(arr);
            return (ContenedorDescriptorImpresoraDN)Utilidades.Serializador.DesSerializar(resp);
        }

    }
}
