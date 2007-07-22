Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

Imports Framework.Usuarios.DN
Imports FN.Localizaciones.DN



<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class LocalizacionesWS
     Inherits System.Web.Services.WebService

    <WebMethod(True)> _
    Public Function RecuperarLocalidadPorCodigoPostal(ByVal pCodigoPostal As String) As Byte()

        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim fachada As New FN.Localizaciones.FS.LocalizacionesFS(Nothing, recurso)

        Dim localidades As ColLocalidadDN = fachada.RecuperarLocalidadPorCodigoPostal(pCodigoPostal, Me.Session.SessionID, actor)

        Return Framework.Utilidades.Serializador.Serializar(localidades)

    End Function

End Class
