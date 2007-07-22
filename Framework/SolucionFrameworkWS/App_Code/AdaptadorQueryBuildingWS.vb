Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class AdaptadorQueryBuildingWS
    Inherits System.Web.Services.WebService


    <WebMethod()> _
    Public Function GenerarInforme(ByVal AdaptadorIQB As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'desempaquetamos
        Dim AIQB As Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.AdaptadorInformesQueryBuildingDN = Framework.Utilidades.Serializador.DesSerializar(AdaptadorIQB)

        'invocamos la FS
        Dim fs As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.FS.AdaptadorInformesQueryBuildingFS(Nothing, recurso)
        Return fs.GenerarInforme_Archivo(Session.SessionID, actor, AIQB)
    End Function

    <WebMethod()> _
    Public Function GenerarEsquemaXMLEnArchivo(ByVal AdaptadorIQB As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'desempaquetamos
        Dim AIQB As Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.AdaptadorInformesQueryBuildingDN = Framework.Utilidades.Serializador.DesSerializar(AdaptadorIQB)

        'invocamos la FS
        Dim fs As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.FS.AdaptadorInformesQueryBuildingFS(Nothing, recurso)
        Return fs.GenerarEsquemaXMLEnArchivo_Archivo(Session.SessionID, actor, AIQB)
    End Function

    <WebMethod()> _
    Public Function GenerarEsquemaXML(ByVal AdaptadorIQB As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'desempaquetamos
        Dim AIQB As Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.AdaptadorInformesQueryBuildingDN = Framework.Utilidades.Serializador.DesSerializar(AdaptadorIQB)

        'invocamos la FS
        Dim fs As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.FS.AdaptadorInformesQueryBuildingFS(Nothing, recurso)
        Dim doc As System.Xml.XmlDocument = fs.GenerarEsquemaXML(Session.SessionID, actor, AIQB)
        Return Framework.Utilidades.Serializador.Serializar(doc)
    End Function

End Class

