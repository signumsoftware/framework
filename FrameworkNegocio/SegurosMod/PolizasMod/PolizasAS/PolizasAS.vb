

Public Class PolizasAS
    Inherits Framework.AS.BaseAS

    Public Function RecuperarCrearTomador(ByVal pCifNif As String) As FN.Seguros.Polizas.DN.TomadorDN
        Dim servicio As New PolizasWS.PolizasWS()

        Dim respuesta As Byte()
        Dim presResp As FN.Seguros.Polizas.DN.TomadorDN

        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()


        respuesta = servicio.RecuperarCrearTomador(pCifNif)

        presResp = Framework.Utilidades.Serializador.DesSerializar(respuesta)

        Return presResp

    End Function
End Class
