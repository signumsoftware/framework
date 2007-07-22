Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Procesos.ProcesosDN

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class MV2WS
    Inherits System.Web.Services.WebService



    <WebMethod(True)> _
      Public Function RecuperarCrearOperacion(ByVal vm As Byte()) As Byte()


    End Function






    <WebMethod(True)> _
      Public Function RecuperarColTiposCompatibles(ByVal pTipo As Byte()) As Byte()

        Dim recurso As RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN = Nothing
        Dim miTipo As System.Type

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        principal.Autorizado()


        miTipo = Framework.Utilidades.Serializador.DesSerializar(pTipo)


        ' llamar al gestor de persistencia y que de los tipos mapeados para la la propiedad
        Dim respuesta As Generic.IList(Of System.Type)

        Dim fs As Framework.FachadaLogica.FachadaBaseFS
        fs = New Framework.FachadaLogica.FachadaBaseFS(Nothing, recurso)
        respuesta = fs.RecuperarColTiposCompatibles(Me.Session.SessionID.ToString, principal, miTipo)

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






    <WebMethod(True)> _
      Public Function GuardarColDNGenerico(ByVal pColIHuellaEntidadDN As Byte()) As Byte()

        Dim recurso As RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN = Nothing
        Dim colHuellas As Framework.DatosNegocio.ColIHuellaEntidadDN
        Dim colEdn As Framework.DatosNegocio.ColIEntidadBaseDN


        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        principal.Autorizado()


        colHuellas = Framework.Utilidades.Serializador.DesSerializar(pColIHuellaEntidadDN)

        'Dim fs As GDocEntrantesFS.EntradaDocsFS
        'fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        'fs.ProcesarColComandoOperacion(Me.Session.SessionID.ToString, principal, col)

        Return Framework.Utilidades.Serializador.Serializar(colEdn)



    End Function

    <WebMethod(True)> _
      Public Function RecuperarDNGenerico(ByVal pHuellaEntidadDN As Byte()) As Byte()

        Dim recurso As RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN = Nothing
        Dim miHuellaEntidadDN As Framework.DatosNegocio.HEDN

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        principal.Autorizado()


        miHuellaEntidadDN = Framework.Utilidades.Serializador.DesSerializar(pHuellaEntidadDN)


        ' llamar al gestor de persistencia y que de los tipos mapeados para la la propiedad

        Dim fs As Framework.FachadaLogica.FachadaBaseFS
        fs = New Framework.FachadaLogica.FachadaBaseFS(Nothing, recurso)


        Dim respuesta As Framework.DatosNegocio.IEntidadBaseDN
        respuesta = fs.RecuperarGenerico(Me.Session.SessionID.ToString, principal, miHuellaEntidadDN)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)



    End Function
    <WebMethod(True)> _
  Public Function RecuperarColDNGenerico(ByVal pColIHuellaEntidadDN As Byte()) As Byte()

        Dim recurso As RecursoLN
        Dim principal As Framework.Usuarios.DN.PrincipalDN = Nothing
        Dim miColiHuellaEntidadDN As Framework.DatosNegocio.ColIHuellaEntidadDN

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        principal.Autorizado()


        miColiHuellaEntidadDN = Framework.Utilidades.Serializador.DesSerializar(pColIHuellaEntidadDN)


        ' llamar al gestor de persistencia y que de los tipos mapeados para la la propiedad

        Dim fs As Framework.FachadaLogica.FachadaBaseFS
        fs = New Framework.FachadaLogica.FachadaBaseFS(Nothing, recurso)


        Dim respuesta As Framework.DatosNegocio.ColIEntidadBaseDN
        respuesta = fs.RecuperarListaGenerico(Me.Session.SessionID.ToString, principal, miColiHuellaEntidadDN)


        Return Framework.Utilidades.Serializador.Serializar(respuesta)



    End Function

End Class
