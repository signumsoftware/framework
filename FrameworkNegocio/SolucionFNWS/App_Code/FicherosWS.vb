Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports Framework.Ficheros.FicherosDN
Imports System.Collections.Generic


<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class FicherosWS
    Inherits System.Web.Services.WebService

    <WebMethod(True)> _
    Public Function ObtenerCajonDocumentosRelacionados(ByVal pGUID As String) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        ''desempaquetamos
        'Dim AIQB As Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.AdaptadorInformesQueryBuildingDN = Framework.Utilidades.Serializador.DesSerializar(AdaptadorIQB)

        'invocamos la FS
        Dim fs As New Framework.Ficheros.FicherosFS.FicherosFS(Nothing, recurso)
        Dim col As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN = fs.ObtenerCajonDocumentosRelacionados(pGUID, actor, Session.SessionID)
        Return Framework.Utilidades.Serializador.Serializar(col)
    End Function

    <WebMethod(True)> _
    Public Function RecuperarListadoRutas() As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'invocamos la FS
        Dim fs As New Framework.Ficheros.FicherosFS.RutaAlmacenamientoFicheroFS(Nothing, recurso)
        Dim lista As IList(Of RutaAlmacenamientoFicherosDN) = fs.RecuperarListadoRutas(actor, Session.SessionID)
        Return Framework.Utilidades.Serializador.Serializar(lista)
    End Function

    <WebMethod(True)> _
    Public Function GuardarRutaAlmacenamientoF(ByVal RutaAlmacenamientoF As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'invocamos la FS
        Dim fs As New Framework.Ficheros.FicherosFS.RutaAlmacenamientoFicheroFS(Nothing, recurso)
        Dim rutaf As RutaAlmacenamientoFicherosDN = Framework.Utilidades.Serializador.DesSerializar(RutaAlmacenamientoF)
        Dim respuesta As RutaAlmacenamientoFicherosDN = fs.GuardarRutaAlmacenamientoF(rutaf, actor, Me.Session.SessionID)
        Return Framework.Utilidades.Serializador.Serializar(respuesta)
    End Function

    <WebMethod(True)> _
    Public Function CerrarRaf(ByVal RutaAlmacenamiento As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'invocamos la FS
        Dim fs As New Framework.Ficheros.FicherosFS.RutaAlmacenamientoFicheroFS(Nothing, recurso)
        Dim rutaf As RutaAlmacenamientoFicherosDN = Framework.Utilidades.Serializador.DesSerializar(RutaAlmacenamiento)
        Dim respuesta As RutaAlmacenamientoFicherosDN = fs.CerrarRaf(rutaf, actor, Me.Session.SessionID)
        Return Framework.Utilidades.Serializador.Serializar(respuesta)
    End Function
    <WebMethod(True)> _
       Public Function RecuperarColTipoFichero() As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'invocamos la FS
        Dim fs As New Framework.Ficheros.FicherosFS.FicherosFS(Nothing, recurso)
        Dim respuesta As Framework.Ficheros.FicherosDN.ColTipoFicheroDN = fs.RecuperarColTipoFichero(actor, Me.Session.SessionID)
        Return Framework.Utilidades.Serializador.Serializar(respuesta)
    End Function



    <WebMethod(True)> _
          Public Function VincularCajonDocumento() As System.Data.DataSet
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim actor As Framework.Usuarios.DN.PrincipalDN

        'verificar sesión
        actor = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'obtener el recurso
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'invocamos la FS
        Dim fs As New Framework.Ficheros.FicherosFS.FicherosFS(Nothing, recurso)
        VincularCajonDocumento = fs.VincularCajonDocumento(actor, Me.Session.SessionID)
    End Function
End Class