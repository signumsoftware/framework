Imports Framework.DatosNegocio

<Serializable()> _
Public Class NotificacionDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mDatosMensaje As DatosMensajeDN
    Protected mCausa As CausaDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal causa As CausaDN, ByVal datosMensaje As DatosMensajeDN)
        Dim mensajeVal As String
        mensajeVal = ""

        If Not ValidarCausa(mensajeVal, causa) OrElse Not ValidarDatosMensaje(mensajeVal, datosMensaje) Then
            Throw New ApplicationExceptionDN(mensajeVal)
        End If

        CambiarValorRef(Of CausaDN)(causa, mCausa)
        CambiarValorRef(Of DatosMensajeDN)(datosMensaje, mDatosMensaje)

        modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"

    Public ReadOnly Property Causa() As CausaDN
        Get
            Return mCausa
        End Get
    End Property

    Public ReadOnly Property Mensaje() As DatosMensajeDN
        Get
            Return mDatosMensaje
        End Get
    End Property

#End Region

#Region "Métodos de validación"

    Private Function ValidarCausa(ByRef mensaje As String, ByVal causa As CausaDN) As Boolean
        If causa Is Nothing Then
            mensaje = "La causa de la notificación no puede ser nula"
            Return False
        End If
        Return True
    End Function

    Private Function ValidarDatosMensaje(ByRef mensaje As String, ByVal datosMensaje As DatosMensajeDN) As Boolean
        If datosMensaje Is Nothing Then
            mensaje = "El mensaje de la notificacion no puede ser nulo"
            Return False
        End If
        Return True
    End Function

#End Region

#Region "Métodos"



#End Region

End Class
