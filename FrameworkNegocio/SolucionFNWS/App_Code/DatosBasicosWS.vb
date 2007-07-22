Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Usuarios.DN

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class DatosBasicosWS
     Inherits System.Web.Services.WebService

    <WebMethod(True)> _
   Public Function RecuperarListaTipos(ByVal pTipo As Byte()) As Byte()

        Dim recurso As RecursoLN
        Dim principal As PrincipalDN

        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim pTipoEnti As System.Type
        pTipoEnti = Framework.Utilidades.Serializador.DesSerializar(pTipo)


        Dim respuesta As ArrayList
        Dim fs As Framework.FachadaLogica.FachadaBaseFS

        fs = New Framework.FachadaLogica.FachadaBaseFS(Nothing, recurso)
        respuesta = fs.RecuperarLista(Me.Session.SessionID.ToString, principal, pTipoEnti)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function

    <WebMethod(True)> _
   Public Function RecuperarListaTiposxListaIDs(ByVal listaIDs As Byte(), ByVal pTipo As Byte()) As Byte()

        Dim recurso As RecursoLN
        Dim principal As PrincipalDN

        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim pTipoEnti As System.Type
        Dim lista As ArrayList

        pTipoEnti = Framework.Utilidades.Serializador.DesSerializar(pTipo)
        lista = Framework.Utilidades.Serializador.DesSerializar(listaIDs)

        Dim respuesta As ArrayList
        Dim fs As Framework.FachadaLogica.FachadaBaseFS

        fs = New Framework.FachadaLogica.FachadaBaseFS(Nothing, recurso)
        respuesta = fs.RecuperarLista(Me.Session.SessionID.ToString, principal, lista, pTipoEnti)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function

    <WebMethod(True)> _
    Public Function RecuperarPorValorIDenticoEnTipo(ByVal pTipo As Byte(), ByVal pValorHash As String) As Byte()

        Dim recurso As RecursoLN
        Dim principal As PrincipalDN

        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim pTipoEnti As System.Type
        pTipoEnti = Framework.Utilidades.Serializador.DesSerializar(pTipo)

        Dim respuesta As ArrayList
        Dim fs As Framework.FachadaLogica.FachadaBaseFS

        fs = New Framework.FachadaLogica.FachadaBaseFS(Nothing, recurso)
        respuesta = fs.RecuperarPorValorIDenticoEnTipo(Me.Session.SessionID.ToString, principal, pTipoEnti, pValorHash)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(True)> _
    Public Sub GuardarListaTipos(ByVal lista As Byte())
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN
        Dim miLista As IList

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim fs As Framework.FachadaLogica.FachadaBaseFS
        fs = New Framework.FachadaLogica.FachadaBaseFS(Nothing, recurso)

        miLista = Framework.Utilidades.Serializador.DesSerializar(lista)
        fs.GuardarListaTipos(Me.Session.SessionID.ToString, principal, miLista)

    End Sub

    <WebMethod(True)> _
    Public Function RecuperarGenerico(ByVal huellaEnt As Byte()) As Object
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim he As Framework.DatosNegocio.IHuellaEntidadDN
        he = Framework.Utilidades.Serializador.DesSerializar(huellaEnt)

        Dim respuesta As Object
        Dim fs As Framework.FachadaLogica.FachadaBaseFS

        fs = New Framework.FachadaLogica.FachadaBaseFS(Nothing, recurso)
        respuesta = fs.RecuperarGenerico(Me.Session.SessionID.ToString, principal, he)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function

    <WebMethod(True)> _
    Public Function RecuperarGenericoIdTipo(ByVal idEnt As String, ByVal tipoEnt As Byte()) As Object
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim te As System.Type
        te = Framework.Utilidades.Serializador.DesSerializar(tipoEnt)

        Dim respuesta As Object
        Dim fs As Framework.FachadaLogica.FachadaBaseFS

        fs = New Framework.FachadaLogica.FachadaBaseFS(Nothing, recurso)
        respuesta = fs.RecuperarGenerico(Me.Session.SessionID.ToString, principal, idEnt, te)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function

    <WebMethod(True)> _
    Public Function GuardarDNGenerico(ByVal pIEntidadBaseDN As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN = Nothing
        Dim miIEntidadDN As Framework.DatosNegocio.IEntidadBaseDN

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        principal.Autorizado()


        miIEntidadDN = Framework.Utilidades.Serializador.DesSerializar(pIEntidadBaseDN)


        ' llamar al gestor de persistencia y que de los tipos mapeados para la la propiedad

        Dim fs As Framework.FachadaLogica.FachadaBaseFS
        fs = New Framework.FachadaLogica.FachadaBaseFS(Nothing, recurso)


        fs.GuardarGenerico(Me.Session.SessionID.ToString, principal, miIEntidadDN)

        Return Framework.Utilidades.Serializador.Serializar(miIEntidadDN)

    End Function

End Class
