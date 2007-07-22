Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

Imports Framework.Utilidades

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class PagosWS
    Inherits System.Web.Services.WebService

    <WebMethod(True)> _
    Public Function GuardarTalonDoc(ByVal pTDoc As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim fs As New FN.GestionPagos.FS.PagosFS(Nothing, recurso)

        Return Framework.Utilidades.Serializador.Serializar(fs.GuardarTalonDoc(principal, Me.Session.SessionID, Framework.Utilidades.Serializador.DesSerializar(pTDoc)))
    End Function

    <WebMethod(True)> _
    Public Function GuardarTalonDN(ByVal pTalonDN As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim fs As New FN.GestionPagos.FS.PagosFS(Nothing, recurso)
        Return Framework.Utilidades.Serializador.Serializar(fs.GuardarTalonDN(principal, Me.Session.SessionID, Framework.Utilidades.Serializador.DesSerializar(pTalonDN)))
    End Function

    <WebMethod(True)> _
    Public Function CargarPagos(ByVal pDtsGestionPagos As Byte(), ByVal tipoOrigen As Byte(), ByVal operacion As Byte()) As Data.DataSet
        Dim recurso As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN
        Dim tipoEO As FN.GestionPagos.DN.TipoEntidadOrigenDN
        Dim op As Framework.Procesos.ProcesosDN.OperacionDN

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim miDts As FN.GestionPagos.DN.dtsGestionPagos = Framework.Utilidades.Serializador.DesSerializar(pDtsGestionPagos)
        tipoEO = Framework.Utilidades.Serializador.DesSerializar(tipoOrigen)
        op = Framework.Utilidades.Serializador.DesSerializar(operacion)

        Dim fs As New FN.GestionPagos.FS.PagosFS(Nothing, recurso)
        Return fs.CargarPagos(principal, Me.Session.SessionID, miDts, tipoEO, op)

    End Function

    <WebMethod(True)> _
    Public Function AltaModificacionProveedores(ByVal pDtsGestionPagos As Byte()) As Data.DataSet
        Dim recurso As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim miDts As FN.GestionPagos.DN.dtsGestionPagos = Framework.Utilidades.Serializador.DesSerializar(pDtsGestionPagos)

        Dim fs As New FN.GestionPagos.FS.PagosFS(Nothing, recurso)
        Return fs.AltaModificacionProveedores(principal, Me.Session.SessionID, miDts)
    End Function

    <WebMethod(True)> _
    Public Function GuardarConfiguracionImpresion(ByVal pConfiguracionImp As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim fs As New FN.GestionPagos.FS.PagosFS(Nothing, recurso)

        Dim mici As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN = fs.GuardarConfiguracionImpresion(principal, Me.Session.SessionID, Serializador.DesSerializar(pConfiguracionImp))

        Return Serializador.Serializar(mici)
    End Function

    <WebMethod(True)> _
    Public Function GuardarPlantillaCarta(ByVal pPlantillaCarta As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim fs As New FN.GestionPagos.FS.PagosFS(Nothing, recurso)

        Dim miplantilla As FN.GestionPagos.DN.PlantillaCartaDN = Serializador.DesSerializar(pPlantillaCarta)

        miplantilla = fs.GuardarPlantillaCarta(principal, Me.Session.SessionID.ToString, miplantilla)

        Return Serializador.Serializar(miplantilla)
    End Function

    <WebMethod(True)> _
    Public Function RecuperarTalonDN(ByVal pId As String) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim fs As New FN.GestionPagos.FS.PagosFS(Nothing, recurso)

        Dim miTalon As FN.GestionPagos.DN.TalonDN = fs.RecuperarTalonDN(principal, Me.Session.SessionID.ToString, pId)

        Return Serializador.Serializar(miTalon)
    End Function

    <WebMethod(True)> _
    Public Function RecuperarFicherosTransferenciasActivos() As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim fs As New FN.GestionPagos.FS.PagosFS(Nothing, recurso)

        Dim colFT As FN.GestionPagos.DN.ColFicheroTransferenciaDN = fs.RecuperarFicherosTransferenciasActivos(principal, Me.Session.SessionID.ToString)

        Return Serializador.Serializar(colFT)

    End Function

End Class
