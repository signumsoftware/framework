Public Class OperadorAS
    Inherits Framework.AS.BaseAS

#Region "Métodos"

    Public Sub GuardarOperador(ByVal operador As AmvDocumentosDN.OperadorDN)
        Dim servicio As OperadorWS.OperadorWS
        Dim operadorByte As Byte()

        ' crear y redirigir a la url del servicio
        servicio = New OperadorWS.OperadorWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        operadorByte = Framework.Utilidades.Serializador.Serializar(operador)

        servicio.GuardarOperador(operadorByte)

    End Sub

    Public Function RecuperarListaOperador() As IList(Of AmvDocumentosDN.OperadorDN)
        Dim servicio As OperadorWS.OperadorWS
        Dim respuesta As Byte()

        ' crear y redirigir a la url del servicio
        servicio = New OperadorWS.OperadorWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        respuesta = servicio.RecuperarListadoOperadores()

        Return Framework.Utilidades.Serializador.DesSerializar(respuesta)

    End Function

    Public Function RecuperarOperador(ByVal id As String) As AmvDocumentosDN.OperadorDN
        Dim servicio As OperadorWS.OperadorWS
        Dim respuesta As Byte()
        Dim operador As AmvDocumentosDN.OperadorDN

        ' crear y redirigir a la url del servicio
        servicio = New OperadorWS.OperadorWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        respuesta = servicio.RecuperarOperador(id)
        operador = Framework.Utilidades.Serializador.DesSerializar(respuesta)

        Return operador

    End Function

    Public Sub BajaOperador(ByVal idOperador As String)
        Dim servicio As OperadorWS.OperadorWS

        ' crear y redirigir a la url del servicio
        servicio = New OperadorWS.OperadorWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        servicio.BajaOperador(idOperador)

    End Sub

    Public Sub ReactivarOperador(ByVal idOperador As String)
        Dim servicio As OperadorWS.OperadorWS

        ' crear y redirigir a la url del servicio
        servicio = New OperadorWS.OperadorWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        servicio.ReactivarOperador(idOperador)

    End Sub

#End Region

End Class
