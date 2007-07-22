Public Class CajonDocumentoAS
    Inherits Framework.AS.BaseAS

    Public Function ObtenerCajonDocumentosRelacionados(ByVal pGUID As String) As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
        Dim respuesta As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
        Dim paqueteRespuesta As Byte()
        Dim servicio As FicherosWS.FicherosWS

        ' crear y redirigir a la url del servicio
        servicio = New FicherosWS.FicherosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqueteRespuesta = servicio.ObtenerCajonDocumentosRelacionados(pGUID)
        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta
    End Function


    Public Function VincularCajonDocumento() As System.Data.DataSet

        Dim servicio As FicherosWS.FicherosWS

        ' crear y redirigir a la url del servicio
        servicio = New FicherosWS.FicherosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        VincularCajonDocumento = servicio.VincularCajonDocumento()


    End Function

End Class
