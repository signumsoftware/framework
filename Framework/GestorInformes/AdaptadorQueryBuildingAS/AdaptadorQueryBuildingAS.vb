Public Class AdaptadorQueryBuildingAS
    Inherits Framework.AS.BaseAS

    ''' <summary>
    ''' Genera un informe a partir de un AdaptadorIQB y devuelve el array 
    ''' de bytes que contiene el archivo generado
    ''' </summary>
    ''' <param name="AdaptadorIQB"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GenerarInforme(ByVal AdaptadorIQB As Byte()) As Byte()
        Dim servicio As AdaptadorQueryBuildingWS.AdaptadorQueryBuildingWS

        'crear y redirigir el servicio
        servicio = New AdaptadorQueryBuildingWS.AdaptadorQueryBuildingWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC

        Dim paquete As Byte() = Framework.Utilidades.Serializador.Serializar(AdaptadorIQB)

        Return servicio.GenerarInforme(paquete)
    End Function

    ''' <summary>
    ''' Rellena el customxml de una plantilla a partir de un AdaptadorIQB y devuelve el
    ''' array de bytes con el contenido del fichero generado
    ''' </summary>
    ''' <param name="AdaptadorIQB"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GenerarEsquemaXMLEnArchivo(ByVal AdaptadorIQB As Byte()) As Byte()
        Dim servicio As AdaptadorQueryBuildingWS.AdaptadorQueryBuildingWS

        'crear y redirigir el servicio
        servicio = New AdaptadorQueryBuildingWS.AdaptadorQueryBuildingWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Dim paquete As Byte() = Framework.Utilidades.Serializador.Serializar(AdaptadorIQB)

        Return servicio.GenerarEsquemaXMLEnArchivo(paquete)
    End Function

    ''' <summary>
    ''' Genera el documento XML del esquema de DataSource a partir del AdaptadorIQB que recibe
    ''' </summary>
    ''' <param name="AdaptadorIQB"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GenerarEsquemaXML(ByVal AdaptadorIQB As Byte()) As Xml.XmlDocument
        Dim servicio As AdaptadorQueryBuildingWS.AdaptadorQueryBuildingWS

        'crear y redirigir el servicio
        servicio = New AdaptadorQueryBuildingWS.AdaptadorQueryBuildingWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Dim paquete As Byte() = Framework.Utilidades.Serializador.Serializar(AdaptadorIQB)

        Return CType(Framework.Utilidades.Serializador.DesSerializar(servicio.GenerarEsquemaXMLEnArchivo(paquete)), Xml.XmlDocument)
    End Function

End Class
