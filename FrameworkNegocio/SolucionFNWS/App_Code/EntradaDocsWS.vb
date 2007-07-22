Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Usuarios.DN
Imports Framework.Usuarios.FS
Imports Framework.Usuarios.LN
Imports FN.Ficheros.FicherosDN

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class Service
    Inherits System.Web.Services.WebService


    <WebMethod(True)> _
    Public Function RecuperarOperacionActivaPorEntidadNegocio(ByVal pIDEntidadNegocio As String, ByVal pmnensaje As String) As Byte()

        Dim recurso As RecursoLN
        Dim principal As PrincipalDN

        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)



        Dim respuesta As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.RecuperarOperacionAPostProcesar(Me.Session.SessionID.ToString, principal, Nothing, pIDEntidadNegocio)


        Dim contenedorrespuesta As New AmvDocumentosDN.ContenedorRecuperadorOperacionActiva
        contenedorrespuesta.Operacion = respuesta
        If respuesta Is Nothing Then
            contenedorrespuesta.mensaje = "No hay ninguna operación activa para recuperar o no está autorizado para hacerlo."
        End If


        Return Framework.Utilidades.Serializador.Serializar(contenedorrespuesta)


    End Function

    'Public Function RecuperarUnicos(ByVal pLsitIEntidadDN As Byte()) As Byte()
    '    Dim recurso As RecursoLN
    '    Dim principal As PrincipalDN = Nothing

    '    recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
    '    '   principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion

    '    Dim parametro1 As New Collections.Generic.List(Of Framework.DatosNegocio.IEntidadDN)
    '    Dim parametro2 As IList


    '    parametro2 = Framework.Utilidades.Serializador.DesSerializar(pLsitIEntidadDN)
    '    parametro1.AddRange(parametro2)

    '    ' guardar con unificacion



    '    ' devolver los resultados


    '    'Dim fs As MotorBusquedaFS.GestorBusquedaFS
    '    'fs = New MotorBusquedaFS.GestorBusquedaFS(Nothing, recurso)
    '    'respuesta = fs.RecuperarEstructuraVista(Me.Session.SessionID.ToString, principal, pParametroCargaEstructura)

    '    Return Framework.Utilidades.Serializador.Serializar(parametro2)


    'End Function

    <WebMethod(True)> _
      Public Function ProcesarColComandoOperacion(ByVal pColComandoOperacionDN As Byte()) As Byte()

        Dim recurso As RecursoLN
        Dim principal As PrincipalDN = Nothing

        Dim col As AmvDocumentosDN.ColComandoOperacionDN

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion

        principal.Autorizado()


        col = Framework.Utilidades.Serializador.DesSerializar(pColComandoOperacionDN)

        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        fs.ProcesarColComandoOperacion(Me.Session.SessionID.ToString, principal, col)

        Return Framework.Utilidades.Serializador.Serializar(col)



    End Function




    <WebMethod(False)> _
      Public Sub FicheroIncidentado(ByVal pDatosIdentidad As Byte(), ByVal pDatosFicheroIncidentado As Byte())
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN
        Dim dia As DatosIdentidadDN
        Dim miDatosFicheroIncidentado As New AmvDocumentosDN.DatosFicheroIncidentado

        ' tomar el recurso
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)


        ' optener el principal dado que es un metodo sin acceso a sesion
        dia = Framework.Utilidades.Serializador.DesSerializar(pDatosIdentidad)
        Dim usuln As UsuariosLN
        usuln = New UsuariosLN(Nothing, recurso)
        principal = usuln.ObtenerPrincipal(dia)


        'Desempaquetamos los datos 
        miDatosFicheroIncidentado = Framework.Utilidades.Serializador.DesSerializar(pDatosFicheroIncidentado)



        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        fs.RegistrarFicheroIncidentado("", principal, miDatosFicheroIncidentado)


    End Sub

    <WebMethod(False)> _
    Public Sub AltaDocumento(ByVal pDatosIdentidad As Byte(), ByVal pFicheroParaAlta As Byte())
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN
        Dim dia As DatosIdentidadDN
        Dim fpa As New AmvDocumentosDN.FicheroParaAlta

        ' tomar el recurso
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)




        ' optener el principal dado que es un metodo sin acceso a sesion
        dia = Framework.Utilidades.Serializador.DesSerializar(pDatosIdentidad)
        'Dim usuln As UsuariosLN.UsuariosLN
        'usuln = New UsuariosLN.UsuariosLN(Nothing, recurso)
        'principal = usuln.ObtenerPrincipal(dia)


        Dim usfs As UsuarioFS
        usfs = New UsuarioFS(Nothing, recurso)
        principal = usfs.ObtenerPrincipal(dia, Nothing, "")



        'Desempaquetamos los datos 
        fpa = Framework.Utilidades.Serializador.DesSerializar(pFicheroParaAlta)



        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        fs.AltaDocumento("", principal, fpa)


    End Sub


    <WebMethod(False)> _
     Public Function RecuperarColTipoCanal() As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN = Nothing

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim respuesta As AmvDocumentosDN.ColTipoCanalDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.RecuperarColTipoCanal("", principal)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(False)> _
      Public Function RecuperarNumDocPendientesClasificaryPostClasificacion() As Data.DataSet
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN = Nothing

        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim respuesta As Data.DataSet
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.RecuperarNumDocPendientesClasificaryPostClasificacion("", principal, Nothing)

        Return respuesta


    End Function

    <WebMethod(False)> _
     Public Function RecuperarArbolTiposEntNegocio() As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN = Nothing

        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        ' principal = ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)



        Dim respuesta As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.RecuperarArbolTiposEntNegocio("", principal)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(True)> _
     Public Function GuardarArbolTiposEntNegocio(ByVal pCabeceraNodoTipoEntNegoio As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN = Nothing

        Dim CabeceraNodoTipoEntNegoio As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN

        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        ' principal = ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        CabeceraNodoTipoEntNegoio = Framework.Utilidades.Serializador.DesSerializar(pCabeceraNodoTipoEntNegoio)

        Dim respuesta As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.GuardarArbolTiposEntNegocio(Me.Session.SessionID.ToString, principal, CabeceraNodoTipoEntNegoio)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(True)> _
     Public Function RecuperarOperacionAProcesarClasificarEntrada(ByVal pIdTipoCanal As String) As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN

        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Dim tipoEntNegocio As AmvDocumentosDN.TipoEntNegoioDN
        'tipoEntNegocio=Framework.Utilidades.Serializador.DesSerializar(
        Dim respuesta As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.RecuperarOperacionAProcesar(Me.Session.SessionID.ToString, principal, pIdTipoCanal)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(True)> _
     Public Function RecuperarOperacionAProcesarPostClasificar(ByVal pTipoEntNegoioDN As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN

        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim pTipoEnti As AmvDocumentosDN.TipoEntNegoioDN
        pTipoEnti = Framework.Utilidades.Serializador.DesSerializar(pTipoEntNegoioDN)


        Dim respuesta As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.RecuperarOperacionAPostProcesar(Me.Session.SessionID.ToString, principal, pTipoEnti, "")

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(True)> _
       Public Function RecuperarOperacionEnCursoPara() As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN

        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        Dim respuesta As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.RecuperarOperacionEnCursoPara(Me.Session.SessionID.ToString, principal)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(True)> _
     Public Function IncidentarOperacionEnRelacionENFichero(ByVal pOperacionEnRelacionENFichero As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN
        Dim operacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        operacion = Framework.Utilidades.Serializador.DesSerializar(pOperacionEnRelacionENFichero)

        Dim respuesta As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.IncidentarOperacion(Me.Session.SessionID.ToString, principal, operacion)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)

    End Function

    <WebMethod(True)> _
     Public Function AnularOperacionEnRelacionENFichero(ByVal pOperacionEnRelacionENFichero As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN
        Dim operacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        operacion = Framework.Utilidades.Serializador.DesSerializar(pOperacionEnRelacionENFichero)

        Dim respuesta As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.AnularOperacion(Me.Session.SessionID.ToString, principal, operacion)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(True)> _
     Public Function RechazarOperacionEnRelacionENFichero(ByVal pOperacionEnRelacionENFichero As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN
        Dim operacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        operacion = Framework.Utilidades.Serializador.DesSerializar(pOperacionEnRelacionENFichero)

        Dim respuesta As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.RechazarOperacion(Me.Session.SessionID.ToString, principal, operacion)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(True)> _
     Public Function ClasificarOperacionEnRelacionENFichero(ByVal pOperacionEnRelacionENFichero As Byte(), ByVal pColEntNegocioDN As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN
        Dim operacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim colEntidades As AmvDocumentosDN.ColEntNegocioDN

        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        operacion = Framework.Utilidades.Serializador.DesSerializar(pOperacionEnRelacionENFichero)
        colEntidades = Framework.Utilidades.Serializador.DesSerializar(pColEntNegocioDN)


        Dim respuesta As AmvDocumentosDN.ColOperacionEnRelacionENFicheroDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.ClasificarOperacion(Me.Session.SessionID.ToString, principal, operacion, colEntidades)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(True)> _
     Public Function ClasificarYCerrarOperacionEnRelacionENFichero(ByVal pOperacionEnRelacionENFichero As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN
        Dim operacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        operacion = Framework.Utilidades.Serializador.DesSerializar(pOperacionEnRelacionENFichero)

        Dim respuesta As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.ClasificarYCerrarOperacion(Me.Session.SessionID.ToString, principal, operacion)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(True)> _
     Public Function GuardarOperacionEnRelacionENFichero(ByVal pOperacionEnRelacionENFichero As Byte()) As Byte()
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN
        Dim operacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        '     principal = CType(WSHelper.ControladorSesionLN.ComprobarUsuario(Me), PrincipalDN)  ' verificacion de sesion
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        operacion = Framework.Utilidades.Serializador.DesSerializar(pOperacionEnRelacionENFichero)

        Dim respuesta As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.GuardarOperacion(Me.Session.SessionID.ToString, principal, operacion)

        Return Framework.Utilidades.Serializador.Serializar(respuesta)


    End Function

    <WebMethod(False)> _
     Public Function AutorizadoConfigurarClienteSonda(ByVal pDatosIdentidad As Byte()) As Boolean
        Dim recurso As RecursoLN
        Dim principal As PrincipalDN
        Dim DatosIdentidad As DatosIdentidadDN

        '  principal = ControladorSesionLN.ComprobarUsuario(Me) ' verificacion de sesion
        recurso = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)


        DatosIdentidad = Framework.Utilidades.Serializador.DesSerializar(pDatosIdentidad)


        Dim respuesta As Boolean
        Dim fs As GDocEntrantesFS.EntradaDocsFS
        fs = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        respuesta = fs.AutorizadoConfigurarClienteSonda("", DatosIdentidad)

        Return respuesta


    End Function

    <WebMethod(True)> _
     Public Function RecuperarOperacionxID(ByVal idOperacion As String) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As GDocEntrantesFS.EntradaDocsFS
        Dim principal As PrincipalDN
        Dim operacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim respuesta As Byte()

        'Verificacion de sesión
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        fl = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
        operacion = fl.RecuperarOperacionxID(idOperacion, principal, Me.Session.SessionID())

        respuesta = Framework.Utilidades.Serializador.Serializar(operacion)

        Return respuesta

    End Function

    <WebMethod(True)> _
    Public Function RecuperarRelacionEnFicheroXID(ByVal id As String) As Byte()
        Dim principal As PrincipalDN
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fachada As GDocEntrantesFS.EntradaDocsFS
        Dim relacionenfichero As AmvDocumentosDN.RelacionENFicheroDN

        'verificamos la sesión
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        fachada = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)

        relacionenfichero = fachada.RecuperarRelacionEnFicheroXID(id, Me.Session.SessionID(), principal)

        Return Framework.Utilidades.Serializador.Serializar(relacionenfichero)


    End Function

    '<WebMethod(True)> _
    ' Public Sub EnviarMailFicheroIncidentados()
    '    Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
    '    Dim fl As GDocEntrantesFS.EntradaDocsFS
    '    Dim principal As PrincipalDN

    '    'Verificacion de sesión
    '    principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

    '    'Pedimos los datos al servidor
    '    recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

    '    fl = New GDocEntrantesFS.EntradaDocsFS(Nothing, recurso)
    '    fl.EnviarMailFicheroIncidentados(Me.Session.SessionID(), principal)

    'End Sub

End Class
