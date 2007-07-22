Imports Framework.AS
Imports Framework.Utilidades

Public Class ClienteAS
    Inherits Framework.AS.BaseAS

    Public Function RecuperarOperacionActivaPorIdEntidad(ByVal pIDEntidadNegocio As String, ByRef mensaje As String) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim ws As GDocEntrantesWS.GDocEntrantesWS
        Dim datos As Byte()

        ws = New GDocEntrantesWS.GDocEntrantesWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        datos = ws.RecuperarOperacionActivaPorEntidadNegocio(pIDEntidadNegocio, mensaje)

        Dim micontenedor As AmvDocumentosDN.ContenedorRecuperadorOperacionActiva = Serializador.DesSerializar(datos)

        mensaje = micontenedor.mensaje

        Return micontenedor.Operacion

    End Function

    Public Function RecuperarRelacionEnFichero(ByVal pID As String) As AmvDocumentosDN.RelacionENFicheroDN
        Dim ws As GDocEntrantesWS.GDocEntrantesWS
        Dim datos As Byte()

        ws = New GDocEntrantesWS.GDocEntrantesWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        datos = ws.RecuperarRelacionEnFicheroXID(pID)

        Return Serializador.DesSerializar(datos)

    End Function

    Public Function ProcesarColComandooperacion(ByVal pColComandoOperacion As AmvDocumentosDN.ColComandoOperacionDN) As AmvDocumentosDN.ColComandoOperacionDN
        Dim pas As GDocEntrantesWS.GDocEntrantesWS
        Dim datos As Byte()

        Dim paquete As Byte() = Serializador.Serializar(pColComandoOperacion)

        pas = New GDocEntrantesWS.GDocEntrantesWS
        pas.Url = RedireccionURL(pas.Url)
        pas.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        datos = pas.ProcesarColComandoOperacion(paquete)

        Return Serializador.DesSerializar(datos)
    End Function

    Public Function RecuperarOperacionEnCursoPara() As AmvDocumentosDN.OperacionEnRelacionENFicheroDN

        Dim pas As GDocEntrantesWS.GDocEntrantesWS
        Dim datos As Byte()

        pas = New GDocEntrantesWS.GDocEntrantesWS
        pas.Url = RedireccionURL(pas.Url)
        pas.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        datos = pas.RecuperarOperacionEnCursoPara()

        Return Serializador.DesSerializar(datos)
    End Function

    Public Function RecuperarArbolTiposEntNegocio() As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN

        Dim pas As GDocEntrantesWS.GDocEntrantesWS
        Dim datos As Byte()

        pas = New GDocEntrantesWS.GDocEntrantesWS
        pas.Url = RedireccionURL(pas.Url)
        pas.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        datos = pas.RecuperarArbolTiposEntNegocio

        Return Serializador.DesSerializar(datos)

    End Function

    Public Function RecuperarOperacionAProcesarNuevos(ByVal pIdCanal As String) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim ws As GDocEntrantesWS.GDocEntrantesWS
        ' crear y redirigir a la url del servicio
        ws = New GDocEntrantesWS.GDocEntrantesWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Dim datos As Byte() = ws.RecuperarOperacionAProcesarClasificarEntrada(pIdCanal)

        Return Serializador.DesSerializar(datos)
    End Function

    Public Function RecuperarOperacionAProcesarClasificados(ByVal pTipoEntNegocio As AmvDocumentosDN.TipoEntNegoioDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim ws As GDocEntrantesWS.GDocEntrantesWS
        ' crear y redirigir a la url del servicio
        ws = New GDocEntrantesWS.GDocEntrantesWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Dim paquete As Byte() = Serializador.Serializar(pTipoEntNegocio)

        Dim datos As Byte() = ws.RecuperarOperacionAProcesarPostClasificar(paquete)

        Return Serializador.DesSerializar(datos)
    End Function




    Public Function VincularCajonDocumento(ByVal pTipoEntNegocio As AmvDocumentosDN.TipoEntNegoioDN) As DataSet
        Dim ws As FicherosWS.FicherosWS
        ' crear y redirigir a la url del servicio
        ws = New FicherosWS.FicherosWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Dim paquete As Byte() = Serializador.Serializar(pTipoEntNegocio)

        VincularCajonDocumento = ws.VincularCajonDocumento()


    End Function

    'Obsoleto
    'Public Function RecuperarOperacionAProcesar(ByVal pTrabajarCon As String) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN

    '    Dim ws As GDocEntrantesWS.GDocEntrantesWS
    '    ' crear y redirigir a la url del servicio
    '    ws = New GDocEntrantesWS.GDocEntrantesWS
    '    ws.Url = RedireccionURL(ws.Url)
    '    ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

    '    Dim datos As Byte() = Nothing

    '    Select Case pTrabajarCon
    '        Case Is = "nuevos documentos"
    '            datos = ws.RecuperarOperacionAProcesarClasificarEntrada()
    '        Case Is = "ya clasificados"
    '            datos = ws.RecuperarOperacionAProcesarPostClasificar()
    '    End Select

    '    Return Serializador.DesSerializar(datos)
    'End Function

    Public Function Clasificar(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN, ByVal pColEntNegocio As AmvDocumentosDN.ColEntNegocioDN) As AmvDocumentosDN.ColOperacionEnRelacionENFicheroDN

        Dim ws As GDocEntrantesWS.GDocEntrantesWS
        Dim paquete As Byte() = Serializador.Serializar(pOperacion)
        Dim colEntidades As Byte() = Serializador.Serializar(pColEntNegocio)

        ' crear y redirigir a la url del servicio
        ws = New GDocEntrantesWS.GDocEntrantesWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        'llamamos a clasificar
        Dim datos As Byte() = ws.ClasificarOperacionEnRelacionENFichero(paquete, colEntidades)

        Return Serializador.DesSerializar(datos)
    End Function

    Public Function ClasificarYCerrar(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim ws As GDocEntrantesWS.GDocEntrantesWS
        Dim paquete As Byte() = Serializador.Serializar(pOperacion)
        ' crear y redirigir a la url del servicio
        ws = New GDocEntrantesWS.GDocEntrantesWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        'llamamos a clasificarycerrar
        Dim datos As Byte() = ws.ClasificarYCerrarOperacionEnRelacionENFichero(paquete)

        Return Serializador.DesSerializar(datos)

    End Function

    Public Function Anular(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim ws As GDocEntrantesWS.GDocEntrantesWS
        Dim paquete As Byte() = Serializador.Serializar(pOperacion)
        ' crear y redirigir a la url del servicio
        ws = New GDocEntrantesWS.GDocEntrantesWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        'llamamos a anular
        Dim datos As Byte() = ws.AnularOperacionEnRelacionENFichero(paquete)

        Return Serializador.DesSerializar(datos)
    End Function

    Public Function Rechazar(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim ws As GDocEntrantesWS.GDocEntrantesWS
        Dim paquete As Byte() = Serializador.Serializar(pOperacion)
        ' crear y redirigir a la url del servicio
        ws = New GDocEntrantesWS.GDocEntrantesWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        'llamar a rechazar
        Dim datos As Byte() = ws.RechazarOperacionEnRelacionENFichero(paquete)

        Return Serializador.DesSerializar(datos)
    End Function

    Public Function Incidentar(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim ws As GDocEntrantesWS.GDocEntrantesWS
        Dim paquete As Byte() = Serializador.Serializar(pOperacion)
        ' crear y redirigir a la url del servicio
        ws = New GDocEntrantesWS.GDocEntrantesWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Dim datos As Byte() = ws.IncidentarOperacionEnRelacionENFichero(paquete)

        Return Serializador.DesSerializar(datos)
    End Function

    'Obsoleto
    '''' <summary>
    '''' Esta función sirve para incidentar, rechazar y anular operaciones
    '''' </summary>
    '''' <param name="pOperacion">la operación sobre la que se quiere operar</param>
    '''' <returns>la Operación ya guardada</returns>
    'Public Function GuardarOperacion(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
    '    Dim ws As GDocEntrantesWS.GDocEntrantesWS

    '    Dim paquete As Byte() = Serializador.Serializar(pOperacion)

    '    ' crear y redirigir a la url del servicio
    '    ws = New GDocEntrantesWS.GDocEntrantesWS
    '    ws.Url = RedireccionURL(ws.Url)
    '    ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

    '    Dim datos As Byte() = ws.GuardarOperacionEnRelacionENFichero(paquete)

    '    Return Serializador.DesSerializar(datos)
    'End Function

    Public Function GuardarArbol(ByVal pArbol As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN) As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        Dim ws As GDocEntrantesWS.GDocEntrantesWS
        Dim paquete As Byte() = Serializador.Serializar(pArbol)
        ' crear y redirigir a la url del servicio
        ws = New GDocEntrantesWS.GDocEntrantesWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        'guardar el árbol
        Dim datos As Byte() = ws.GuardarArbolTiposEntNegocio(paquete)

        Return Serializador.DesSerializar(datos)
    End Function

    Public Function RecuperarCanales() As AmvDocumentosDN.ColCanalEntradaDocsDN
        Dim pas As GDocEntrantesWS.GDocEntrantesWS
        Dim datos As Byte()

        pas = New GDocEntrantesWS.GDocEntrantesWS
        pas.Url = RedireccionURL(pas.Url)
        pas.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        datos = pas.RecuperarColTipoCanal()

        Return Serializador.DesSerializar(datos)
    End Function

    Public Function BalizaNumCanalesTipoEntNeg() As Data.DataSet
        Dim pas As GDocEntrantesWS.GDocEntrantesWS

        pas = New GDocEntrantesWS.GDocEntrantesWS
        pas.Url = RedireccionURL(pas.Url)
        pas.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Return pas.RecuperarNumDocPendientesClasificaryPostClasificacion()
    End Function

    Public Function RecuperarColTipoCanales() As AmvDocumentosDN.ColTipoCanalDN
        Dim pas As GDocEntrantesWS.GDocEntrantesWS

        pas = New GDocEntrantesWS.GDocEntrantesWS
        pas.Url = RedireccionURL(pas.Url)
        pas.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Dim datos As Byte() = pas.RecuperarColTipoCanal

        Return Serializador.DesSerializar(datos)
    End Function
    Public Function RecuperarColTipoFichero() As Framework.Ficheros.FicherosDN.ColTipoFicheroDN
        Dim pas As FicherosWS.FicherosWS

        pas = New FicherosWS.FicherosWS
        pas.Url = RedireccionURL(pas.Url)
        pas.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Dim datos As Byte() = pas.RecuperarColTipoFichero

        Return Serializador.DesSerializar(datos)
    End Function
    Public Function RecuperarOperacionxID(ByVal idOperacion As String) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim servicio As GDocEntrantesWS.GDocEntrantesWS
        Dim respuesta As Byte()
        Dim operacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN

        ' crear y redirigir a la url del servicio
        servicio = New GDocEntrantesWS.GDocEntrantesWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        respuesta = servicio.RecuperarOperacionxID(idOperacion)
        operacion = Framework.Utilidades.Serializador.DesSerializar(respuesta)

        Return operacion

    End Function

End Class
