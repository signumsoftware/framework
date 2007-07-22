Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports Framework.Usuarios.DN

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class GestionPagosWS
    Inherits System.Web.Services.WebService

    <WebMethod(True)> _
      Public Function CargarAgrupacionID(ByVal pAgrupApunteImpDDN As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.GestionPagos.FS.PagosFS2(Nothing, recurso)

        Dim AgrupApunteImpDDN As FN.GestionPagos.DN.AgrupApunteImpDDN = Framework.Utilidades.Serializador.DesSerializar(pAgrupApunteImpDDN)


        AgrupApunteImpDDN = fachada.CargarAgrupacionID(actor, Me.Session.SessionID, AgrupApunteImpDDN)

        Return Framework.Utilidades.Serializador.Serializar(AgrupApunteImpDDN)

    End Function

    <WebMethod(True)> _
      Public Function CrearAgrupacionID(ByVal pParEntFiscalGenericaParamDN As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.GestionPagos.FS.PagosFS2(Nothing, recurso)
        Dim ParEntFiscalGenericaParam As FN.GestionPagos.DN.ParEntFiscalGenericaParamDN = Framework.Utilidades.Serializador.DesSerializar(pParEntFiscalGenericaParamDN)


        ParEntFiscalGenericaParam = fachada.CrearAgrupacionID(actor, Me.Session.SessionID, ParEntFiscalGenericaParam)

        Return Framework.Utilidades.Serializador.Serializar(ParEntFiscalGenericaParam)

    End Function

    <WebMethod(True)> _
      Public Function BuscarImportesDebidosLibres(ByVal pParEntFiscalGenericaParamDN As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.GestionPagos.FS.PagosFS2(Nothing, recurso)
        Dim ParEntFiscalGenericaParam As FN.GestionPagos.DN.ParEntFiscalGenericaParamDN = Framework.Utilidades.Serializador.DesSerializar(pParEntFiscalGenericaParamDN)


        Dim respuesta As Data.DataSet = fachada.BuscarImportesDebidosLibres(actor, Me.Session.SessionID, ParEntFiscalGenericaParam)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function
    <WebMethod(True)> _
      Public Function CompensarPago(ByVal pPagoCompensador As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.GestionPagos.FS.PagosFS2(Nothing, recurso)
        Dim pagoCompensador As FN.GestionPagos.DN.PagoDN = Framework.Utilidades.Serializador.DesSerializar(pPagoCompensador)


        Dim respuesta As FN.GestionPagos.DN.PagoDN = fachada.CompensarPago(actor, Me.Session.SessionID, pagoCompensador, Nothing)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function


    <WebMethod(True)> _
  Public Function AnularPago(ByVal pPago As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.GestionPagos.FS.PagosFS2(Nothing, recurso)
        Dim pago As FN.GestionPagos.DN.PagoDN = Framework.Utilidades.Serializador.DesSerializar(pPago)


        Dim respuesta As FN.GestionPagos.DN.PagoDN = fachada.AnularPago(actor, Me.Session.SessionID, pago)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function


    <WebMethod(True)> _
  Public Function LiquidarPago(ByVal pPago As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.GestionPagos.FS.PagosFS2(Nothing, recurso)
        Dim pago As FN.GestionPagos.DN.PagoDN = Framework.Utilidades.Serializador.DesSerializar(pPago)


        Dim respuesta As FN.GestionPagos.DN.PagoDN = fachada.LiquidarPago(actor, Me.Session.SessionID, pago)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function

    <WebMethod(True)> _
  Public Function EfectuarPago(ByVal pPago As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.GestionPagos.FS.PagosFS2(Nothing, recurso)
        Dim pago As FN.GestionPagos.DN.PagoDN = Framework.Utilidades.Serializador.DesSerializar(pPago)


        Dim respuesta As FN.GestionPagos.DN.PagoDN = fachada.EfectuarPago(actor, Me.Session.SessionID, pago)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function
    <WebMethod(True)> _
Public Function EfectuarYLiquidarPago(ByVal pPago As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.GestionPagos.FS.PagosFS2(Nothing, recurso)
        Dim pago As FN.GestionPagos.DN.PagoDN = Framework.Utilidades.Serializador.DesSerializar(pPago)


        Dim respuesta As FN.GestionPagos.DN.ColLiquidacionPagoDN = fachada.EfectuarYLiquidar(actor, Me.Session.SessionID, pago)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function

    <WebMethod(True)> _
Public Function CrearPagoAgrupadorProvisional(ByVal pPago As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.GestionPagos.FS.PagosFS2(Nothing, recurso)
        Dim pago As FN.GestionPagos.DN.PagoDN = Framework.Utilidades.Serializador.DesSerializar(pPago)


        Dim respuesta As FN.GestionPagos.DN.PagoDN = fachada.CrearPagoAgrupadorProvisional(actor, Me.Session.SessionID, pago)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function



    <WebMethod(True)> _
Public Function AnularPagosNoEmitidosYCrearPagoAgrupador(ByVal pPagoAgrupador As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.GestionPagos.FS.PagosFS2(Nothing, recurso)
        Dim PagoAgrupador As FN.GestionPagos.DN.PagoDN = Framework.Utilidades.Serializador.DesSerializar(pPagoAgrupador)


        Dim respuesta As FN.GestionPagos.DN.PagoDN = fachada.AnularPagosNoEmitidosYCrearPagoAgrupador(actor, Me.Session.SessionID, PagoAgrupador)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function

End Class
