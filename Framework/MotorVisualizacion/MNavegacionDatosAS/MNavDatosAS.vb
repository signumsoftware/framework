Imports MNavegacionDatosDN
Imports Framework.Utilidades

Public Class MNavDatosAS
    Inherits Framework.AS.BaseAS

    Public Function RecuperarEntidadNavDN(ByVal pTipo As System.Type) As EntidadNavDN

        Dim ws As MNavDatosWS.MNavDatosWS
        Dim datos As Byte()

        datos = Serializador.Serializar(pTipo)

        ' crear y redirigir a la url del servicio
        ws = New MNavDatosWS.MNavDatosWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Dim resultado As Byte() = ws.RecuperarEntidadNavDN(datos)

        Return Serializador.DesSerializar(resultado)

    End Function


    Public Function RecuperarRelaciones(ByVal pTipo As System.Type) As ColRelacionEntidadesNavDN


        Dim ws As MNavDatosWS.MNavDatosWS
        Dim datos As Byte()

        datos = Serializador.Serializar(pTipo)

        ' crear y redirigir a la url del servicio
        ws = New MNavDatosWS.MNavDatosWS
        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Dim resultado As Byte() = ws.RecuperarRelaciones(datos)

        Return Serializador.DesSerializar(resultado)

    End Function
End Class
