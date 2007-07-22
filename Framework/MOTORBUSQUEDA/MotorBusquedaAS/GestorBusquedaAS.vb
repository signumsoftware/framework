Imports Framework.Utilidades
Imports MotorBusquedaBasicasDN
Public Class GestorBusquedaAS
    Inherits Framework.AS.BaseAS


    Public Function RecuperarDatos(ByVal pFiltro As MotorBusquedaDN.FiltroDN) As DataSet

        Dim ws As MotorBusquedaWS.MotorBusquedaWS
        Dim datos As Byte()

        datos = Serializador.Serializar(pFiltro)

        ' crear y redirigir a la url del servicio
        ws = New MotorBusquedaWS.MotorBusquedaWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Return ws.RecuperarDatos(datos)

    End Function

    Public Function RecuperarEstructuraVista(ByVal pParametroCargaEstructura As ParametroCargaEstructuraDN) As MotorBusquedaDN.EstructuraVistaDN


        Dim ws As MotorBusquedaWS.MotorBusquedaWS
        Dim datos As Byte()
        Dim respuesta As Byte()

        datos = Serializador.Serializar(pParametroCargaEstructura)

        ' crear y redirigir a la url del servicio
        ws = New MotorBusquedaWS.MotorBusquedaWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        respuesta = ws.RecuperarEstructuraVista(datos)
        Return Serializador.DesSerializar(respuesta)


    End Function

    Public Function RecuperarTiposQueImplementan(ByVal pTipo As System.Type, ByVal pNombrePropiedad As String) As Framework.TiposYReflexion.DN.ColVinculoClaseDN


        Dim ws As MotorBusquedaWS.MotorBusquedaWS

        Dim respuesta As Byte()



        ' crear y redirigir a la url del servicio
        ws = New MotorBusquedaWS.MotorBusquedaWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        respuesta = ws.RecuperarTiposQueImplementan(Framework.TiposYReflexion.DN.VinculoClaseDN.RecuperarNombreEnsambladoClase(pTipo), pNombrePropiedad)
        Return Serializador.DesSerializar(respuesta)


    End Function



End Class
