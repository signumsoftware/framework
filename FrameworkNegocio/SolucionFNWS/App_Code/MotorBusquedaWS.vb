Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

Imports Framework.LogicaNegocios.Transacciones
Imports MotorBusquedaBasicasDN


<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class MotorBusquedaWS
     Inherits System.Web.Services.WebService
    <WebMethod(True)> _
 Public Function RecuperarTiposQueImplementan(ByVal pNombreCompletoClase As String, ByVal NombrePropiedad As String) As Byte()
        Dim recurso As RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN = Nothing

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        '   principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion

        Dim respuesta As Framework.TiposYReflexion.DN.ColVinculoClaseDN


        Dim fs As MotorBusquedaFS.GestorBusquedaFS
        fs = New MotorBusquedaFS.GestorBusquedaFS(Nothing, recurso)
        respuesta = fs.RecuperarTiposQueImplementan(Me.Session.SessionID.ToString, principal, pNombreCompletoClase, NombrePropiedad)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(True)> _
     Public Function RecuperarEstructuraVista(ByVal pParametroCargaEstructuraDN As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN = Nothing

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        '   principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion

        Dim respuesta As MotorBusquedaDN.EstructuraVistaDN
        Dim pParametroCargaEstructura As ParametroCargaEstructuraDN
        pParametroCargaEstructura = Framework.Utilidades.Serializador.DesSerializar(pParametroCargaEstructuraDN)

        Dim fs As MotorBusquedaFS.GestorBusquedaFS
        fs = New MotorBusquedaFS.GestorBusquedaFS(Nothing, recurso)
        respuesta = fs.RecuperarEstructuraVista(Me.Session.SessionID.ToString, principal, pParametroCargaEstructura)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function

    <WebMethod(True)> _
    Public Function RecuperarDatos(ByVal pFiltroDN As Byte()) As Data.DataSet
        Dim recurso As RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN = Nothing

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        'principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion

        Dim respuesta As Data.DataSet
        Dim pFiltro As MotorBusquedaDN.FiltroDN
        pFiltro = Framework.Utilidades.Serializador.DesSerializar(pFiltroDN)

        Dim fs As MotorBusquedaFS.GestorBusquedaFS
        fs = New MotorBusquedaFS.GestorBusquedaFS(Nothing, recurso)
        respuesta = fs.RecuperarDatos(Me.Session.SessionID.ToString, principal, pFiltro)

        Return respuesta

    End Function

End Class
