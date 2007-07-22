Public Class LocalizacionesAS
    Inherits Framework.AS.BaseAS

    Public Function RecuperarLocalidadPorCodigoPostal(ByVal pCodigoPostal As String) As FN.Localizaciones.DN.ColLocalidadDN
        Dim servicio As LocalizacionesWS.LocalizacionesWS
        Dim respuesta As Byte()

        'crear y redirigir el servicio
        servicio = New LocalizacionesWS.LocalizacionesWS
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        'Dim paquete As Byte() = Framework.Utilidades.Serializador.Serializar(x)

        respuesta = servicio.RecuperarLocalidadPorCodigoPostal(pCodigoPostal)

        Return CType(Framework.Utilidades.Serializador.DesSerializar(respuesta), FN.Localizaciones.DN.ColLocalidadDN)
    End Function
End Class
