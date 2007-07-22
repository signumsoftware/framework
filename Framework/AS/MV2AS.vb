Imports Framework.Procesos.ProcesosDN

Namespace Framework.AS


    Public Class MV2AS
        Inherits Framework.AS.BaseAS

        Public Function RecuperarColTiposCompatibles(ByVal pTipo As System.Type) As IList(Of System.Type)
            Dim servicio As MV2WS.MV2WS
            Dim respuesta As Byte()
            Dim paqueteParametro As Byte()

            ' crear y redirigir a la url del servicio
            servicio = New MV2WS.MV2WS
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            paqueteParametro = Framework.Utilidades.Serializador.Serializar(pTipo)
            respuesta = servicio.RecuperarColTiposCompatibles(paqueteParametro)

            Dim huella As Framework.DatosNegocio.HEDN

            huella = Framework.Utilidades.Serializador.DesSerializar(respuesta)

            Return huella.EntidadReferida

        End Function

        '' quitar de mvs as y llevarlo al sitema basico de as
        Public Function RecuperarDNGenerico(ByVal pHuellaEntidadDN As Framework.DatosNegocio.HEDN) As Framework.DatosNegocio.IEntidadDN
            Dim servicio As MV2WS.MV2WS
            Dim respuesta As Byte()
            Dim paqueteParametro As Byte()

            ' crear y redirigir a la url del servicio
            servicio = New MV2WS.MV2WS
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            paqueteParametro = Framework.Utilidades.Serializador.Serializar(pHuellaEntidadDN)
            respuesta = servicio.RecuperarDNGenerico(paqueteParametro)

            Dim huella As Framework.DatosNegocio.HEDN

            huella = Framework.Utilidades.Serializador.DesSerializar(respuesta)

            Return huella.EntidadReferida

        End Function

        Public Function RecuperarColDNGenerico(ByVal pColIHuellaEntidadDN As Framework.DatosNegocio.ColIHuellaEntidadDN) As Framework.DatosNegocio.ColIEntidadBaseDN
            Dim servicio As MV2WS.MV2WS
            Dim respuesta As Byte()
            Dim paqueteParametro As Byte()

            ' crear y redirigir a la url del servicio
            servicio = New MV2WS.MV2WS
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            paqueteParametro = Framework.Utilidades.Serializador.Serializar(pColIHuellaEntidadDN)
            respuesta = servicio.RecuperarColDNGenerico(paqueteParametro)

            RecuperarColDNGenerico = Framework.Utilidades.Serializador.DesSerializar(respuesta)



        End Function



        Public Function GuardarDNGenerico(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN, ByVal objeto As Object, ByVal instanciaSolicitante As Object) As Framework.DatosNegocio.IEntidadBaseDN
            Dim servicio As MV2WS.MV2WS
            Dim respuesta As Byte()
            Dim paqueteParametro As Byte()

            ' crear y redirigir a la url del servicio
            servicio = New MV2WS.MV2WS
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            paqueteParametro = Framework.Utilidades.Serializador.Serializar(pEntidad)
            respuesta = servicio.GuardarDNGenerico(paqueteParametro)

            Dim Entidad As Framework.DatosNegocio.IEntidadBaseDN

            Entidad = Framework.Utilidades.Serializador.DesSerializar(respuesta)

            Return Entidad

        End Function


        Public Function GuardarDNGenerico2(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN, ByVal instanciaSolicitante As Object) As Framework.DatosNegocio.IEntidadBaseDN
            Dim servicio As MV2WS.MV2WS
            Dim respuesta As Byte()
            Dim paqueteParametro As Byte()

            ' crear y redirigir a la url del servicio
            servicio = New MV2WS.MV2WS
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            paqueteParametro = Framework.Utilidades.Serializador.Serializar(pEntidad)
            respuesta = servicio.GuardarDNGenerico(paqueteParametro)

            Dim Entidad As Framework.DatosNegocio.IEntidadBaseDN

            Entidad = Framework.Utilidades.Serializador.DesSerializar(respuesta)

            Return Entidad

        End Function

        Public Function GuardarOperacionDNGenerico(ByVal pOperacion As OperacionDN, ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) As Framework.DatosNegocio.IEntidadBaseDN
            Dim servicio As MV2WS.MV2WS
            Dim respuesta As Byte()
            Dim paqueteParametro As Byte()

            ' crear y redirigir a la url del servicio
            servicio = New MV2WS.MV2WS
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()




            paqueteParametro = Framework.Utilidades.Serializador.Serializar(pEntidad)
            respuesta = servicio.GuardarDNGenerico(paqueteParametro)

            Dim Entidad As Framework.DatosNegocio.IEntidadBaseDN

            Entidad = Framework.Utilidades.Serializador.DesSerializar(respuesta)

            Return Entidad

        End Function

    End Class


End Namespace
