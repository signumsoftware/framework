Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports Framework.Usuarios.DN


<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class PolizasWS
     Inherits System.Web.Services.WebService

    <WebMethod(True)> _
      Public Function RecuperarCrearTomador(ByVal pCifNif As String) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.Seguros.Polizas.FS.PolizasFS(Nothing, recurso)


        Dim respuesta As FN.Seguros.Polizas.DN.TomadorDN = fachada.RecuperarCrearTomador(Me.Session.SessionID, actor, pCifNif)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function

End Class
