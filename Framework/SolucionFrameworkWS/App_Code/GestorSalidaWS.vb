Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports Framework.GestorSalida.DN

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class GestorSalidaWS
     Inherits System.Web.Services.WebService

    <WebMethod(True)> _
    Public Function EnviarDocumentoSalida(ByVal pDocumentosalida As Byte()) As String
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'desempaquetamos
        Dim documentoSalida As Framework.GestorSalida.DN.DocumentoSalida = Framework.Utilidades.Serializador.DesSerializar(pDocumentosalida)

        'invocamos la FS
        Dim fs As New Framework.GestorSalida.FS.GestorSalidaFS(Nothing, recurso)
        Return fs.InsertarDocumentoSalidaEnCola(actor, Me.Session.SessionID, documentoSalida)
    End Function

    <WebMethod(True)> _
    Public Function RecuperarDocumentoSalidaPorTicket(ByVal ticket As String) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'invocamos la fs
        Dim fs As New Framework.GestorSalida.FS.GestorSalidaFS(Nothing, recurso)
        Dim respuesta As DocumentoSalida = fs.RecuperarDocumentoSalidaPorTicket(actor, Me.Session.SessionID, ticket)
        Return Framework.Utilidades.Serializador.Serializar(respuesta)
    End Function

    <WebMethod(True)> _
    Public Function RecuperarEstadoEnvioPorTicket(ByVal ticket As String) As Framework.GestorSalida.DN.EstadoEnvio
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'invocamos la fs
        Dim fs As New Framework.GestorSalida.FS.GestorSalidaFS(Nothing, recurso)
        Return fs.RecuperarEstadoEnvioPorTicket(actor, Me.Session.SessionID, ticket)
    End Function

    <WebMethod(True)> _
    Public Function BajaContenedorDescriptorImpresora(ByVal impresora As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'invocamos la fs
        Dim fs As New Framework.GestorSalida.FS.GestorSalidaFS(Nothing, recurso)
        Dim imp As ContenedorDescriptorImpresoraDN = Framework.Utilidades.Serializador.DesSerializar(impresora)
        imp = fs.BajaContenedorDescriptorImpresora(actor, Me.Session.SessionID, imp)

        Return Framework.Utilidades.Serializador.Serializar(imp)
    End Function

    <WebMethod(True)> _
    Public Function AltaContenedorDescriptorImpresora(ByVal impresora As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'invocamos la fs
        Dim fs As New Framework.GestorSalida.FS.GestorSalidaFS(Nothing, recurso)
        Dim imp As ContenedorDescriptorImpresoraDN = Framework.Utilidades.Serializador.DesSerializar(impresora)
        imp = fs.AltaContenedorDescriptorImpresora(actor, Me.Session.SessionID, imp)

        Return Framework.Utilidades.Serializador.Serializar(imp)
    End Function
End Class
