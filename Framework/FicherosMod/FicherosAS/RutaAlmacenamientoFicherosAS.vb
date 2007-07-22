Imports Framework.Ficheros.FicherosDN
Public Class RutaAlmacenamientoFicherosAS
    Inherits Framework.AS.BaseAS

#Region "Métodos"

    Public Function RecuperarListadoRutas() As IList(Of RutaAlmacenamientoFicherosDN)
        Dim respuesta As IList(Of RutaAlmacenamientoFicherosDN)
        Dim paqueteRespuesta As Byte()
        Dim servicio As FicherosWS.FicherosWS

        ' crear y redirigir a la url del servicio
        servicio = New FicherosWS.FicherosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqueteRespuesta = servicio.RecuperarListadoRutas()
        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function GuardarRutaAlmacenamientoF(ByVal rutaAlmacenamiento As RutaAlmacenamientoFicherosDN) As RutaAlmacenamientoFicherosDN
        Dim respuesta As RutaAlmacenamientoFicherosDN
        Dim paqueteRespuesta As Byte()
        Dim servicio As FicherosWS.FicherosWS
        Dim rutaByte As Byte()

        ' crear y redirigir a la url del servicio
        servicio = New FicherosWS.FicherosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        rutaByte = Framework.Utilidades.Serializador.Serializar(rutaAlmacenamiento)

        paqueteRespuesta = servicio.GuardarRutaAlmacenamientoF(rutaByte)

        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function CerrarRaf(ByVal rutaAlmacenamiento As RutaAlmacenamientoFicherosDN) As RutaAlmacenamientoFicherosDN
        Dim servicio As FicherosWS.FicherosWS
        Dim rutaByte As Byte()
        Dim paqueteRespuesta As Byte()

        ' crear y redirigir a la url del servicio
        servicio = New FicherosWS.FicherosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        rutaByte = Framework.Utilidades.Serializador.Serializar(rutaAlmacenamiento)

        paqueteRespuesta = servicio.CerrarRaf(rutaByte)

        rutaAlmacenamiento = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return rutaAlmacenamiento

    End Function

#End Region

End Class
