Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Usuarios.DN

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class GSAMVWS
     Inherits System.Web.Services.WebService

    <WebMethod(True)> _
    Public Function GenerarPresupuestoxCuestionarioRes(ByVal cuestionarioR As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN
        Dim respuesta As Byte()
        Dim cr As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        Dim pr As FN.Seguros.Polizas.DN.PresupuestoDN

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        cr = Framework.Utilidades.Serializador.DesSerializar(cuestionarioR)

        Dim fs As GSAMV.FS.GestionSegurosAMVFS
        fs = New GSAMV.FS.GestionSegurosAMVFS(Nothing, recurso)

        pr = fs.GenerarPresupuestoxCuestionarioRes(cr, Me.Session.SessionID.ToString, principal)

        respuesta = Framework.Utilidades.Serializador.Serializar(pr)

        Return respuesta

    End Function

    <WebMethod(True)> _
    Public Function GenerarTarifaxCuestionarioRes(ByVal cuestionarioR As Byte(), ByVal tiempoTarificado As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN
        Dim respuesta As Byte()
        Dim cr As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN
        Dim tiempoT As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        cr = Framework.Utilidades.Serializador.DesSerializar(cuestionarioR)
        tiempoT = Framework.Utilidades.Serializador.DesSerializar(tiempoTarificado)

        Dim fs As GSAMV.FS.GestionSegurosAMVFS
        fs = New GSAMV.FS.GestionSegurosAMVFS(Nothing, recurso)

        tarifa = fs.GenerarTarifaxCuestionarioRes(cr, tiempoT, Me.Session.SessionID.ToString, principal)

        respuesta = Framework.Utilidades.Serializador.Serializar(tarifa)

        Return respuesta

    End Function

End Class
