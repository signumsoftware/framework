Namespace Framework.AS
    Public Class DatosBasicosAS
        Inherits Framework.AS.BaseAS

#Region "Métodos"

        Public Function ReactivarGenericoDN(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) As Framework.DatosNegocio.IEntidadBaseDN
            Dim idp As Framework.DatosNegocio.IDatoPersistenteDN = pEntidad
            idp.Baja = False
            Return GuardarDNGenerico(idp)
        End Function

        Public Function BajaGenericoDN(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) As Framework.DatosNegocio.IEntidadBaseDN
            Dim servicio As DatosBasicosWS.DatosBasicosWS
            Dim respuesta As Byte()
            Dim paqueteParametro As Byte()

            ' crear y redirigir a la url del servicio
            servicio = New DatosBasicosWS.DatosBasicosWS()
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            Dim idp As Framework.DatosNegocio.IDatoPersistenteDN = pEntidad
            idp.Baja = True

            paqueteParametro = Framework.Utilidades.Serializador.Serializar(idp)
            respuesta = servicio.BajaGenericoDN(paqueteParametro)

            Dim Entidad As Framework.DatosNegocio.IEntidadBaseDN

            Entidad = Framework.Utilidades.Serializador.DesSerializar(respuesta)

            Return Entidad

        End Function

        Public Function RecuperarPorValorIDenticoEnTipo(ByVal pTipo As System.Type, ByVal pHashValor As String) As ArrayList
            Dim servicio As DatosBasicosWS.DatosBasicosWS
            Dim respuesta As Byte()
            Dim paqueteParametro As Byte()
            Dim lista As IList

            ' crear y redirigir a la url del servicio
            servicio = New DatosBasicosWS.DatosBasicosWS()
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            paqueteParametro = Framework.Utilidades.Serializador.Serializar(pTipo)
            respuesta = servicio.RecuperarPorValorIDenticoEnTipo(paqueteParametro, pHashValor)

            lista = Framework.Utilidades.Serializador.DesSerializar(respuesta)

            Return lista

        End Function

        Public Function RecuperarListaTipos(ByVal pTipo As System.Type) As IList
            Dim servicio As DatosBasicosWS.DatosBasicosWS
            Dim respuesta As Byte()
            Dim paqueteParametro As Byte()
            Dim lista As IList

            ' crear y redirigir a la url del servicio
            servicio = New DatosBasicosWS.DatosBasicosWS()
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            paqueteParametro = Framework.Utilidades.Serializador.Serializar(pTipo)
            respuesta = servicio.RecuperarListaTipos(paqueteParametro)

            lista = Framework.Utilidades.Serializador.DesSerializar(respuesta)

            Return lista

        End Function

        Public Function RecuperarListaTipos(ByVal listaIDs As ArrayList, ByVal pTipo As System.Type) As IList
            Dim servicio As DatosBasicosWS.DatosBasicosWS
            Dim respuesta As Byte()
            Dim paqueteParametroTipo As Byte()
            Dim paqueteParametroLista As Byte()
            Dim lista As IList

            ' crear y redirigir a la url del servicio
            servicio = New DatosBasicosWS.DatosBasicosWS()
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            paqueteParametroTipo = Framework.Utilidades.Serializador.Serializar(pTipo)
            paqueteParametroLista = Framework.Utilidades.Serializador.Serializar(listaIDs)

            respuesta = servicio.RecuperarListaTiposxListaIDs(paqueteParametroLista, paqueteParametroTipo)

            lista = Framework.Utilidades.Serializador.DesSerializar(respuesta)

            Return lista

        End Function

        Public Sub GuardarListaTipos(ByVal lista As IList)
            Dim servicio As DatosBasicosWS.DatosBasicosWS
            Dim paqueteParametro As Byte()

            ' crear y redirigir a la url del servicio
            servicio = New DatosBasicosWS.DatosBasicosWS()
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            paqueteParametro = Framework.Utilidades.Serializador.Serializar(lista)
            servicio.GuardarListaTipos(paqueteParametro)

        End Sub

        Public Function RecuperarGenerico(ByVal huellaEnt As Framework.DatosNegocio.IHuellaEntidadDN) As Object
            Dim servicio As DatosBasicosWS.DatosBasicosWS
            Dim respuesta As Byte()
            Dim paqueteParametro As Byte()
            Dim obj As Object

            ' crear y redirigir a la url del servicio
            servicio = New DatosBasicosWS.DatosBasicosWS()
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            paqueteParametro = Framework.Utilidades.Serializador.Serializar(huellaEnt)
            respuesta = servicio.RecuperarGenerico(paqueteParametro)

            obj = Framework.Utilidades.Serializador.DesSerializar(respuesta)

            Return obj

        End Function

        Public Function RecuperarGenerico(ByVal idEnt As String, ByVal tipoEnt As System.Type) As Object
            Dim servicio As DatosBasicosWS.DatosBasicosWS
            Dim respuesta As Byte()
            Dim paqueteParametro As Byte()
            Dim obj As Object

            ' crear y redirigir a la url del servicio
            servicio = New DatosBasicosWS.DatosBasicosWS()
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            paqueteParametro = Framework.Utilidades.Serializador.Serializar(tipoEnt)
            respuesta = servicio.RecuperarGenericoIdTipo(idEnt, paqueteParametro)

            obj = Framework.Utilidades.Serializador.DesSerializar(respuesta)

            Return obj

        End Function

        Public Function GuardarDNGenerico(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) As Framework.DatosNegocio.IEntidadBaseDN
            Dim servicio As DatosBasicosWS.DatosBasicosWS
            Dim respuesta As Byte()
            Dim paqueteParametro As Byte()

            ' crear y redirigir a la url del servicio
            servicio = New DatosBasicosWS.DatosBasicosWS()
            servicio.Url = RedireccionURL(servicio.Url)
            servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

            paqueteParametro = Framework.Utilidades.Serializador.Serializar(pEntidad)
            respuesta = servicio.GuardarDNGenerico(paqueteParametro)

            Dim Entidad As Framework.DatosNegocio.IEntidadBaseDN

            Entidad = Framework.Utilidades.Serializador.DesSerializar(respuesta)

            Return Entidad

        End Function

#End Region

    End Class
End Namespace

