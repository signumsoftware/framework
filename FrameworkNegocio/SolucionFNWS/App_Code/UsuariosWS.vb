Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

Imports Framework.Usuarios.DN
Imports Framework.Usuarios.FS

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class UsuariosWS
    Inherits System.Web.Services.WebService

#Region "Métodos Principal y usuarios"

    <WebMethod(True)> _
    Public Function IniciarSesion(ByVal di As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As UsuarioFS
        Dim principal As PrincipalDN
        Dim miDI As DatosIdentidadDN

        'Verificacion de sesion: En este caso no se verifica todavía la sesión, puesto que no hay principal

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        miDI = Framework.Utilidades.Serializador.DesSerializar(di)

        'Recuperamos el principal
        fl = New UsuarioFS(Nothing, recurso)
        principal = fl.IniciarSesion(miDI, Me.Session.SessionID)

        'sólo cargamos datos si el principal es correcto, si no ha conseguido logarse
        'es nothing
        If Not principal Is Nothing Then
            ' cargar  las huella de entida

            'Se asigna el principal a la sesión
            principal = WSHelper.ControladorSesionLN.AsignarUsuario(Me, principal)
        End If


        'Empaquetamos la respuesta
        Return Framework.Utilidades.Serializador.Serializar(principal)

    End Function

    <WebMethod(True)> _
    Public Function ObtenerPrincipal(ByVal di As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As UsuarioFS
        Dim principal As PrincipalDN
        Dim miDI As DatosIdentidadDN

        'Verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        miDI = Framework.Utilidades.Serializador.DesSerializar(di)

        'Recuperamos el principal
        fl = New UsuarioFS(Nothing, recurso)
        principal = fl.ObtenerPrincipal(miDI, principal, Me.Session.SessionID)

        'Empaquetamos la respuesta
        Return Framework.Utilidades.Serializador.Serializar(principal)
    End Function

    <WebMethod(True)> _
    Public Function ObtenerPrincipalPorID(ByVal id As String) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As UsuarioFS
        Dim principal As PrincipalDN

        'Verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Recuperamos 
        fl = New UsuarioFS(Nothing, recurso)
        principal = fl.ObtenerPrincipal(id, principal, Me.Session.SessionID)

        'Empaquetamos la respuesta
        Return Framework.Utilidades.Serializador.Serializar(principal)
    End Function

    <WebMethod(True)> _
    Public Function RecuperarListadoUsuarios() As Data.DataSet
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As UsuarioFS
        Dim principal As PrincipalDN
        Dim dtsRespuesta As Data.DataSet

        'Verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Recuperamos el dataset
        fl = New UsuarioFS(Nothing, recurso)
        dtsRespuesta = fl.RecuperarListadoUsuarios(principal, Me.Session.SessionID)

        Return dtsRespuesta

    End Function

    <WebMethod(True)> _
    Public Function GuardarPrincipal(ByVal principal As Byte(), ByVal di As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As UsuarioFS
        Dim actor As PrincipalDN
        Dim miPrincipal As PrincipalDN
        Dim miDI As DatosIdentidadDN
        Dim paqueteRespuesta As Byte()

        'Verificacion de sesion
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Desempaquetamos los parámetros de entrada
        miPrincipal = Framework.Utilidades.Serializador.DesSerializar(principal)
        miDI = Framework.Utilidades.Serializador.DesSerializar(di)

        fl = New UsuarioFS(Nothing, recurso)
        miPrincipal = fl.GuardarPrincipal(miPrincipal, miDI, actor, Me.Session.SessionID)

        paqueteRespuesta = Framework.Utilidades.Serializador.Serializar(miPrincipal)

        Return paqueteRespuesta

    End Function

    <WebMethod(True)> _
    Public Function GuardarPrincipalSinDI(ByVal principal As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As UsuarioFS
        Dim actor As PrincipalDN
        Dim miPrincipal As PrincipalDN
        Dim paqueteRespuesta As Byte()

        'Verificacion de sesion
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Desempaquetamos los parámetros de entrada
        miPrincipal = Framework.Utilidades.Serializador.DesSerializar(principal)

        fl = New UsuarioFS(Nothing, recurso)
        miPrincipal = fl.GuardarPrincipal(miPrincipal, actor, Me.Session.SessionID)

        paqueteRespuesta = Framework.Utilidades.Serializador.Serializar(miPrincipal)

        Return paqueteRespuesta

    End Function


    <WebMethod(True)> _
    Public Function AltaPrincipal(ByVal principal As Byte(), ByVal di As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As UsuarioFS
        Dim actor As PrincipalDN
        Dim miPrincipal As PrincipalDN
        Dim miDI As DatosIdentidadDN
        Dim paqueteRespuesta As Byte()

        'Verificacion de sesion
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Desempaquetamos los parámetros de entrada
        miPrincipal = Framework.Utilidades.Serializador.DesSerializar(principal)
        miDI = Framework.Utilidades.Serializador.DesSerializar(di)

        fl = New UsuarioFS(Nothing, recurso)
        miPrincipal = fl.AltaPrincipal(miPrincipal, miDI, actor, Me.Session.SessionID)

        paqueteRespuesta = Framework.Utilidades.Serializador.Serializar(miPrincipal)

        Return paqueteRespuesta
    End Function

    <WebMethod(True)> _
    Public Function BajaPrincipal(ByVal principal As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As UsuarioFS
        Dim actor As PrincipalDN
        Dim miPrincipal As PrincipalDN
        Dim paqueteRespuesta As Byte()

        'Verificacion de sesion
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Desempaquetamos los parámetros de entrada
        miPrincipal = Framework.Utilidades.Serializador.DesSerializar(principal)

        fl = New UsuarioFS(Nothing, recurso)
        miPrincipal = fl.BajaPrincipal(miPrincipal, actor, Me.Session.SessionID)

        paqueteRespuesta = Framework.Utilidades.Serializador.Serializar(miPrincipal)

        Return paqueteRespuesta
    End Function

    <WebMethod(True)> _
    Public Function RecuperarPrincipalxEntidadUser(ByVal tipoEnt As Byte(), ByVal idEntidad As String) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As UsuarioFS
        Dim principal As PrincipalDN
        Dim tipo As System.Type

        'Verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        ' miDI = Framework.Utilidades.Serializador.DesSerializar(di)
        tipo = Framework.Utilidades.Serializador.DesSerializar(tipoEnt)

        'Recuperamos el principal
        fl = New UsuarioFS(Nothing, recurso)
        principal = fl.RecuperarPrincipalxEntidadUser(principal, Me.Session.SessionID, tipo, idEntidad)

        'Empaquetamos la respuesta
        Return Framework.Utilidades.Serializador.Serializar(principal)

    End Function

#End Region

#Region "Métodos Roles, Casos de uso y Métodos de sistema"

    <WebMethod(True)> _
    Public Function RecuperarColRoles() As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As RolFS
        Dim principal As PrincipalDN
        Dim colRoles As ColRolDN

        'Verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Recuperamos 
        fl = New RolFS(Nothing, recurso)
        colRoles = fl.RecuperarColRoles(principal, Me.Session.SessionID)

        'Empaquetamos la respuesta
        Return Framework.Utilidades.Serializador.Serializar(colRoles)

    End Function

    <WebMethod(True)> _
    Public Function RecuperarListaCasosUso() As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As CasosUsoFS
        Dim principal As PrincipalDN
        Dim listaCasosUso As IList

        'Verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Recuperamos 
        fl = New CasosUsoFS(Nothing, recurso)
        listaCasosUso = fl.RecuperarListaCasosUso(principal, Me.Session.SessionID)

        'Empaquetamos la respuesta
        Return Framework.Utilidades.Serializador.Serializar(listaCasosUso)

    End Function

    <WebMethod(True)> _
    Public Function RecuperarMetodos() As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As MetodoSistemaFS
        Dim principal As PrincipalDN
        Dim listaMetodos As IList

        'Verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Recuperamos 
        fl = New MetodoSistemaFS(Nothing, recurso)
        listaMetodos = fl.RecuperarMetodos(principal, Me.Session.SessionID)

        'Empaquetamos la respuesta
        Return Framework.Utilidades.Serializador.Serializar(listaMetodos)

    End Function

    <WebMethod(True)> _
    Public Function GuardarCasoUso(ByVal casoUso As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As CasosUsoFS
        Dim principal As PrincipalDN
        Dim miCasoUso As CasosUsoDN

        'Verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Recuperamos 
        fl = New CasosUsoFS(Nothing, recurso)
        miCasoUso = Framework.Utilidades.Serializador.DesSerializar(casoUso)
        miCasoUso = fl.GuardarCasoUso(miCasoUso, principal, Me.Session.SessionID)

        'Empaquetamos la respuesta
        Return Framework.Utilidades.Serializador.Serializar(miCasoUso)

    End Function

    <WebMethod(True)> _
    Public Function GuardarRol(ByVal rol As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As RolFS
        Dim principal As PrincipalDN
        Dim miRol As RolDN

        'Verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Recuperamos 
        fl = New RolFS(Nothing, recurso)
        miRol = Framework.Utilidades.Serializador.DesSerializar(rol)
        miRol = fl.GuardarRol(miRol, principal, Me.Session.SessionID)

        'Empaquetamos la respuesta
        Return Framework.Utilidades.Serializador.Serializar(miRol)

    End Function

#End Region

End Class
