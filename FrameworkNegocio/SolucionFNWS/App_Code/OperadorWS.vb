Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

Imports Framework.Usuarios.DN

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class OperadorWS
     Inherits System.Web.Services.WebService

    <WebMethod(True)> _
    Public Sub GuardarOperador(ByVal operador As Byte())
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As GDocEntrantesFS.OperadorFS
        Dim principal As PrincipalDN
        Dim miOperador As AmvDocumentosDN.OperadorDN

        'Verificacion de sesión
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Recuperamos el dataset
        fl = New GDocEntrantesFS.OperadorFS(Nothing, recurso)
        miOperador = Framework.Utilidades.Serializador.DesSerializar(operador)

        fl.GuardarOperador(miOperador, principal, Me.Session.SessionID)

    End Sub

    <WebMethod(True)> _
    Public Function RecuperarListadoOperadores() As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As GDocEntrantesFS.OperadorFS
        Dim principal As PrincipalDN
        Dim listaOperadores As IList

        'Verificacion de sesión
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Recuperamos el dataset
        fl = New GDocEntrantesFS.OperadorFS(Nothing, recurso)
        listaOperadores = fl.RecuperarListaOperador(principal, Me.Session.SessionID)

        Return Framework.Utilidades.Serializador.Serializar(listaOperadores)

    End Function

    <WebMethod(True)> _
    Public Function RecuperarOperador(ByVal id As String) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As GDocEntrantesFS.OperadorFS
        Dim principal As PrincipalDN
        Dim operador As AmvDocumentosDN.OperadorDN
        Dim respuesta As Byte()

        'Verificacion de sesión
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        fl = New GDocEntrantesFS.OperadorFS(Nothing, recurso)
        operador = fl.RecuperarOperador(id, principal, Me.Session.SessionID())

        respuesta = Framework.Utilidades.Serializador.Serializar(operador)

        Return respuesta

    End Function

    <WebMethod(True)> _
    Public Sub BajaOperador(ByVal idOperador As String)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As GDocEntrantesFS.OperadorFS
        Dim principal As PrincipalDN

        'Verificacion de sesión
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        fl = New GDocEntrantesFS.OperadorFS(Nothing, recurso)
        fl.BajaOperador(idOperador, principal, Me.Session.SessionID())

    End Sub

    <WebMethod(True)> _
    Public Sub ReactivarOperador(ByVal idOperador As String)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As GDocEntrantesFS.OperadorFS
        Dim principal As PrincipalDN

        'Verificacion de sesión
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        fl = New GDocEntrantesFS.OperadorFS(Nothing, recurso)
        fl.ReactivarOperador(idOperador, principal, Me.Session.SessionID())

    End Sub

End Class
