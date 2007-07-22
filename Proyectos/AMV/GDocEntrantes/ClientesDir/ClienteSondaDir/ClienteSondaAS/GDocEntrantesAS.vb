Imports Framework.AS

Imports Framework.Utilidades

Public Class GDocEntrantesAS
    Inherits Framework.AS.BaseAS

    Public Function AutorizadoConfigurarClienteSonda(ByVal pDi As Framework.Usuarios.DN.DatosIdentidadDN) As Boolean

        Dim pas As GDocEntrantesWS.GDocEntrantesWS
        Dim datos As Byte()



        datos = Serializador.Serializar(pDi)




        ' crear y redirigir a la url del servicio
        pas = New GDocEntrantesWS.GDocEntrantesWS
        pas.Url = RedireccionURL(pas.Url)
        pas.CookieContainer = ContenedorSessionAS.contenedorSessionC()



        Return pas.AutorizadoConfigurarClienteSonda(datos)


    End Function

    Public Function RecuperarArbolTiposEntNegocio() As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN

        Dim pas As GDocEntrantesWS.GDocEntrantesWS
        Dim datos As Byte()

        pas = New GDocEntrantesWS.GDocEntrantesWS

        ' crear y redirigir a la url del servicio
        pas = New GDocEntrantesWS.GDocEntrantesWS
        pas.Url = RedireccionURL(pas.Url)
        pas.CookieContainer = ContenedorSessionAS.contenedorSessionC()



        datos = pas.RecuperarArbolTiposEntNegocio

        Return Serializador.DesSerializar(datos)

    End Function

    Public Function AltaDocumento(ByVal pDatosIdentidad As Framework.Usuarios.DN.DatosIdentidadDN, ByVal dae As AmvDocumentosDN.FicheroParaAlta) As Boolean

        Dim pas As GDocEntrantesWS.GDocEntrantesWS
        Dim datosDatosIdentidad, datosParametro As Byte()

        Try


            'Dim dae As New AmvDocumentosDN.FicheroParaAlta
            'dae.HuellaFichero = pHuellaFichero
            'dae.TipoEntidad = pTipoEntidad

            datosParametro = Serializador.Serializar(dae)
            datosDatosIdentidad = Serializador.Serializar(pDatosIdentidad)

        Catch ex As Exception
            Throw New SerializacionASException("error al serializar ", ex)

        End Try



        Try
            ' crear y redirigir a la url del servicio
            pas = New GDocEntrantesWS.GDocEntrantesWS
            pas.Url = RedireccionURL(pas.Url)
            pas.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Catch ex As Exception
            Throw New CreacionServicioASException("Error al instanciar el servicio", ex)
        End Try

        Try


            pas.AltaDocumento(datosDatosIdentidad, datosParametro)

        Catch ex As System.Web.Services.Protocols.SoapException

            If ex.Message.Contains("System.Web.HttpException: Maximum request length exceeded") Then
                Throw New TamañoExcedidoASException("tamaño excedido", ex)
            End If
            Throw
        Catch ex As System.Net.WebException


            ' If ex.Message.Contains("Unable to connect to the remote server") Then
            Throw New ServidorNoEncontradoASException("No se encuentra el servidor remoto", ex)
            ' End If



        End Try


    End Function

    Public Function RecuperarTiposCanal() As AmvDocumentosDN.ColTipoCanalDN
        Dim pas As GDocEntrantesWS.GDocEntrantesWS
        Dim datos As Byte()


        ' crear y redirigir a la url del servicio
        pas = New GDocEntrantesWS.GDocEntrantesWS
        pas.Url = RedireccionURL(pas.Url)
        pas.CookieContainer = ContenedorSessionAS.contenedorSessionC()



        datos = pas.RecuperarColTipoCanal()

        Return Serializador.DesSerializar(datos)

    End Function

    Public Sub FicheroIncidentado(ByVal pDatosIdentidad As Framework.Usuarios.DN.DatosIdentidadDN, ByVal pDatosFicheroIncidentado As AmvDocumentosDN.DatosFicheroIncidentado)

        Dim pas As GDocEntrantesWS.GDocEntrantesWS

        Dim datosDatosIdentidad, datosParametro As Byte()
        Try

            datosParametro = Serializador.Serializar(pDatosFicheroIncidentado)
            datosDatosIdentidad = Serializador.Serializar(pDatosIdentidad)




            ' crear y redirigir a la url del servicio
            pas = New GDocEntrantesWS.GDocEntrantesWS
            pas.Url = RedireccionURL(pas.Url)
            pas.CookieContainer = ContenedorSessionAS.contenedorSessionC()
            pas.FicheroIncidentado(datosDatosIdentidad, datosParametro)


        Catch ex As Exception
            Throw New SerializacionASException("error al serializar ", ex)

        End Try


    End Sub



End Class

<Serializable()> _
Public Class ASException
    Inherits ApplicationException

 

    Public Sub New(ByVal mensaje As String, ByVal pInnerException As Exception)
        MyBase.New(mensaje, pInnerException)
    End Sub

End Class

<Serializable()> _
Public Class TamañoExcedidoASException
    Inherits ASException


    Public Sub New(ByVal mensaje As String, ByVal pInnerException As Exception)
        MyBase.New(mensaje, pInnerException)
    End Sub

End Class


<Serializable()> _
Public Class ConexionConServidorInterrumpidaASException
    Inherits ASException


    Public Sub New(ByVal mensaje As String, ByVal pInnerException As Exception)
        MyBase.New(mensaje, pInnerException)
    End Sub

End Class

<Serializable()> _
Public Class ServidorNoEncontradoASException
    Inherits ASException


    Public Sub New(ByVal mensaje As String, ByVal pInnerException As Exception)
        MyBase.New(mensaje, pInnerException)
    End Sub

End Class

<Serializable()> _
Public Class SerializacionASException
    Inherits ASException


    Public Sub New(ByVal mensaje As String, ByVal pInnerException As Exception)
        MyBase.New(mensaje, pInnerException)
    End Sub

End Class

<Serializable()> _
Public Class CreacionServicioASException
    Inherits ASException


    Public Sub New(ByVal mensaje As String, ByVal pInnerException As Exception)
        MyBase.New(mensaje, pInnerException)
    End Sub

End Class