Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

Imports Framework.Procesos.ProcesosDN
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Usuarios.DN

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class ProcesosWS
    Inherits System.Web.Services.WebService

    <WebMethod(True)> _
    Public Function RecuperarEjecutorCliente(ByVal nombreCliente As String) As Byte()
        Dim respuesta As Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN = Nothing

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        principal.Autorizado()


        ' llamar al gestor de persistencia y que de los tipos mapeados para la la propiedad

        Dim fs As Framework.Procesos.ProcesosFS.OperacionesFS
        fs = New Framework.Procesos.ProcesosFS.OperacionesFS(Nothing, recurso)
        respuesta = fs.RecuperarEjecutorCliente(Me.Session.SessionID.ToString, principal, nombreCliente)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)
    End Function


    '    Public Function EjecutarOperacion(ByVal pTransicionRealizadaOOperacionRealizada As Byte(), ByVal pIEntidadBaseDN As Byte(), ByVal pIEntidadBaseDN As Byte()) As Byte()
    <WebMethod(True)> _
    Public Function EjecutarOperacion(ByVal pParametroOperacionPr As Byte()) As Byte()

        Dim opr As Framework.Procesos.ProcesosDN.OperacionRealizadaDN
        Dim TransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN = Nothing
        Dim miIEntidadDN As Framework.DatosNegocio.IEntidadBaseDN
        Dim misParametros As Object
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        principal.Autorizado()



        Dim miparametro As Framework.Procesos.ProcesosDN.ParametroOperacionPr
        miparametro = Framework.Utilidades.Serializador.DesSerializar(pParametroOperacionPr)
        opr = miparametro.OperacionRealizada
        TransicionRealizada = miparametro.TransicionRealizada
        miIEntidadDN = miparametro.IEntidadDN
        misParametros = miparametro.Parametros


        'Dim parametroMultiple As Object = Framework.Utilidades.Serializador.DesSerializar(pTransicionRealizadaOOperacionRealizada)

        'If TypeOf parametroMultiple Is Framework.Procesos.ProcesosDN.OperacionRealizadaDN Then
        '    opr = parametroMultiple

        'ElseIf TypeOf parametroMultiple Is Framework.Procesos.ProcesosDN.TransicionRealizadaDN Then
        '    TransicionRealizada = parametroMultiple
        'Else
        '    Throw New ApplicationException("tipo incompatible")
        'End If






        ' llamar al gestor de persistencia y que de los tipos mapeados para la la propieda
        Dim objetoRespuesta As Framework.DatosNegocio.IEntidadBaseDN

        Dim fs As Framework.Procesos.ProcesosFS.OperacionesFS
        fs = New Framework.Procesos.ProcesosFS.OperacionesFS(Nothing, recurso)

        If Not opr Is Nothing Then
            objetoRespuesta = fs.EjecutarOperacion(Me.Session.SessionID.ToString, principal, miIEntidadDN, opr, misParametros)
        Else
            objetoRespuesta = fs.EjecutarOperacion(Me.Session.SessionID.ToString, principal, miIEntidadDN, TransicionRealizada, misParametros)
        End If



        Return Framework.Utilidades.Serializador.Serializar(objetoRespuesta)
        ' Return Framework.Utilidades.Serializador.Serializar(miIEntidadDN)

    End Function

    <WebMethod(True)> _
    Public Function RecuperarTransicionesAutorizadasSobre(ByVal pHuellaEntidadDN As Byte()) As Byte()

        Dim recurso As RecursoLN
        Dim principal As PrincipalDN = Nothing
        Dim miHuellaEntidadDN As Framework.DatosNegocio.HEDN
        Dim respuesta As Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        principal.Autorizado()


        miHuellaEntidadDN = Framework.Utilidades.Serializador.DesSerializar(pHuellaEntidadDN)


        ' llamar al gestor de persistencia y que de los tipos mapeados para la la propiedad

        Dim fs As Framework.Procesos.ProcesosFS.OperacionesFS
        fs = New Framework.Procesos.ProcesosFS.OperacionesFS(Nothing, recurso)
        respuesta = fs.RecuperarTransicionesAutorizadasSobre(Me.Session.SessionID.ToString, principal, miHuellaEntidadDN)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function

    <WebMethod(True)> _
    Public Function RecuperarTransicionesDeInicio(ByVal pTipoDN As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN = Nothing
        Dim tipo As System.Type
        Dim respuesta As Framework.Procesos.ProcesosDN.ColTransicionDN

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        principal.Autorizado()


        tipo = Framework.Utilidades.Serializador.DesSerializar(pTipoDN)


        ' llamar al gestor de persistencia y que de los tipos mapeados para la la propiedad

        Dim fs As Framework.Procesos.ProcesosFS.OperacionesFS
        fs = New Framework.Procesos.ProcesosFS.OperacionesFS(Nothing, recurso)
        respuesta = fs.RecuperarTransicionesDeInicio(Me.Session.SessionID.ToString, principal, tipo)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

End Class
