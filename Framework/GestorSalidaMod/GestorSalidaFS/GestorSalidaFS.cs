using System;
using System.Collections.Generic;
using System.Text;
using Framework.Usuarios.DN;
using Framework.GestorSalida.DN;
using Framework.LogicaNegocios.Transacciones;
using Framework.FachadaLogica;
using Framework.GestorSalida.LN;

namespace Framework.GestorSalida.FS
{
    public class GestorSalidaFS : BaseFachadaFL
    {
        public GestorSalidaFS(ITransaccionLogicaLN tl, RecursoLN rec)
            : base(tl, rec)
        { }


        public string InsertarDocumentoSalidaEnCola(PrincipalDN actor, string idSesion, DocumentoSalida pDocumentosalida)
        {
            MetodoFachadaHelper fh = new MetodoFachadaHelper();
            string ticket = string.Empty;
            using (new CajonHiloLN(base.mRec))
            {
                try
                {
                    //'1º guardar log de inicio
                    fh.EntradaMetodo(idSesion, actor, Recurso.Actual);

                    //'2º verificacion de permisos por rol de usuario
                    actor.Autorizado();

                    //'-----------------------------------------------------------------------------
                    //'3º creacion de la ln y ejecucion del metodo

                    DocumentosalidaLN ln = new DocumentosalidaLN();
                    ticket = ln.InsertarDocumentoSalidaEnCola(pDocumentosalida);
                    //'-----------------------------------------------------------------------------

                    //'4º guardar log de fin de metodo , con salidas excepcionales incluidas
                    fh.SalidaMetodo(idSesion, actor, Recurso.Actual);
                }
                catch (Exception ex)
                {
                    fh.SalidaMetodoExcepcional(idSesion, actor, ex, "", Recurso.Actual);
                    throw;
                }
            }
            return ticket;
        }


        public DocumentoSalida RecuperarDocumentoSalidaPorTicket(PrincipalDN actor, string idSesion, string ticket)
        {
            MetodoFachadaHelper fh = new MetodoFachadaHelper();
            DocumentoSalida ds = null;
            using (new CajonHiloLN(base.mRec))
            {
                try
                {
                    //'1º guardar log de inicio
                    fh.EntradaMetodo(idSesion, actor, Recurso.Actual);

                    //'2º verificacion de permisos por rol de usuario
                    actor.Autorizado();

                    //'-----------------------------------------------------------------------------
                    //'3º creacion de la ln y ejecucion del metodo

                    DocumentosalidaLN ln = new DocumentosalidaLN();
                    ds = ln.RecuperarDocumentoSalidaPorTicket(ticket);
                    //'-----------------------------------------------------------------------------

                    //'4º guardar log de fin de metodo , con salidas excepcionales incluidas
                    fh.SalidaMetodo(idSesion, actor, Recurso.Actual);
                }
                catch (Exception ex)
                {
                    fh.SalidaMetodoExcepcional(idSesion, actor, ex, "", Recurso.Actual);
                    throw;
                }
            }
            return ds;
        }


        public EstadoEnvio RecuperarEstadoEnvioPorTicket(PrincipalDN actor, string idSesion, string ticket)
        {
            MetodoFachadaHelper fh = new MetodoFachadaHelper();
            EstadoEnvio ee = EstadoEnvio.Desconocido;
            using (new CajonHiloLN(base.mRec))
            {
                try
                {
                    //'1º guardar log de inicio
                    fh.EntradaMetodo(idSesion, actor, Recurso.Actual);

                    //'2º verificacion de permisos por rol de usuario
                    actor.Autorizado();

                    //'-----------------------------------------------------------------------------
                    //'3º creacion de la ln y ejecucion del metodo

                    DocumentosalidaLN ln = new DocumentosalidaLN();
                    ee = ln.RecuperarEstadoEnvioPorTicket(ticket);
                    //'-----------------------------------------------------------------------------

                    //'4º guardar log de fin de metodo , con salidas excepcionales incluidas
                    fh.SalidaMetodo(idSesion, actor, Recurso.Actual);
                }
                catch (Exception ex)
                {
                    fh.SalidaMetodoExcepcional(idSesion, actor, ex, "", Recurso.Actual);
                    throw;
                }
            }
            return ee;
        }


        public ContenedorDescriptorImpresoraDN BajaContenedorDescriptorImpresora(PrincipalDN actor, string idSesion, ContenedorDescriptorImpresoraDN impresora)
        {
            ContenedorDescriptorImpresoraDN imp = null;
            MetodoFachadaHelper fh = new MetodoFachadaHelper();
            using (new CajonHiloLN(base.mRec))
            {
                try
                {
                    //'1º guardar log de inicio
                    fh.EntradaMetodo(idSesion, actor, Recurso.Actual);

                    //'2º verificacion de permisos por rol de usuario
                    actor.Autorizado();

                    //'-----------------------------------------------------------------------------
                    //'3º creacion de la ln y ejecucion del metodo

                    GestorSalidaImpresionLN ln = new GestorSalidaImpresionLN();
                    imp = ln.BajaContenedorDescriptorImpresora(impresora);
                    //'-----------------------------------------------------------------------------

                    //'4º guardar log de fin de metodo , con salidas excepcionales incluidas
                    fh.SalidaMetodo(idSesion, actor, Recurso.Actual);
                }
                catch (Exception ex)
                {
                    fh.SalidaMetodoExcepcional(idSesion, actor, ex, "", Recurso.Actual);
                    throw;
                }
                return imp;
            }
        }

        public ContenedorDescriptorImpresoraDN AltaContenedorDescriptorImpresora(PrincipalDN actor, string idSesion,ContenedorDescriptorImpresoraDN impresora)
        {
            ContenedorDescriptorImpresoraDN imp = null;
            MetodoFachadaHelper fh = new MetodoFachadaHelper();
            using (new CajonHiloLN(base.mRec))
            {
                try
                {
                    //'1º guardar log de inicio
                    fh.EntradaMetodo(idSesion, actor, Recurso.Actual);

                    //'2º verificacion de permisos por rol de usuario
                    actor.Autorizado();

                    //'-----------------------------------------------------------------------------
                    //'3º creacion de la ln y ejecucion del metodo

                    GestorSalidaImpresionLN ln = new GestorSalidaImpresionLN();
                    imp = ln.AltaContenedorDescriptorImpresora(impresora);
                    //'-----------------------------------------------------------------------------

                    //'4º guardar log de fin de metodo , con salidas excepcionales incluidas
                    fh.SalidaMetodo(idSesion, actor, Recurso.Actual);
                }
                catch (Exception ex)
                {
                    fh.SalidaMetodoExcepcional(idSesion, actor, ex, "", Recurso.Actual);
                    throw;
                }
                return imp;
            }
        }

    }
}
