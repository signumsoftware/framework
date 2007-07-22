Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports Framework.LogicaNegocios.Transacciones

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class MNavDatosWS
     Inherits System.Web.Services.WebService



    <WebMethod(True)> _
    Public Function RecuperarRelaciones(ByVal pTipo As Byte()) As Byte()
        '   ColRelacionEntidadesDN()

        Dim recurso As RecursoLN
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Using New CajonHiloLN(recurso)

            Dim principal As Framework.Usuarios.DN.PrincipalDN = Nothing
            Dim miTipo As System.Type



            principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
            principal.Autorizado()


            miTipo = Framework.Utilidades.Serializador.DesSerializar(pTipo)


            ' llamar al gestor de persistencia y que de los tipos mapeados para la la propiedad
            Dim respuesta As MNavegacionDatosDN.ColRelacionEntidadesNavDN

            Dim fs As MNavegacionDatosFS.MNavegacionDatosFS
            fs = New MNavegacionDatosFS.MNavegacionDatosFS
            respuesta = fs.RecuperarRelaciones(Me.Session.SessionID.ToString, principal, miTipo)

            Return Framework.Utilidades.Serializador.Serializar(respuesta)



        End Using





    End Function

    <WebMethod(True)> _
    Public Function RecuperarEntidadNavDN(ByVal pTipo As Byte()) As Byte()
        'EntidadNavDN()




        Dim recurso As RecursoLN
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Using New CajonHiloLN(recurso)

            Dim principal As Framework.Usuarios.DN.PrincipalDN = Nothing
            Dim miTipo As System.Type



            principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
            principal.Autorizado()


            miTipo = Framework.Utilidades.Serializador.DesSerializar(pTipo)


            ' llamar al gestor de persistencia y que de los tipos mapeados para la la propiedad
            Dim respuesta As MNavegacionDatosDN.EntidadNavDN

            Dim fs As MNavegacionDatosFS.MNavegacionDatosFS
            fs = New MNavegacionDatosFS.MNavegacionDatosFS
            respuesta = fs.RecuperarEntidadNavDN(Me.Session.SessionID.ToString, principal, miTipo)

            Return Framework.Utilidades.Serializador.Serializar(respuesta)



        End Using


    End Function

End Class
