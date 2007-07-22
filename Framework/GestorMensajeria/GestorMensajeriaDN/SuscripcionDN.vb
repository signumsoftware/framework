Imports Framework.DatosNegocio

<Serializable()> _
Public Class SuscripcionDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mCausa As CausaDN
    Protected mDestino As IDestinoDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal causa As CausaDN, ByVal destino As IDestinoDN)
        Dim mensaje As String
        mensaje = ""

        If Not ValidarCausa(mensaje, causa) OrElse Not ValidarDestino(mensaje, destino) Then
            Throw New ApplicationExceptionDN(mensaje)
        End If

        CambiarValorRef(Of CausaDN)(causa, mCausa)
        CambiarValorRef(Of IDestinoDN)(destino, mDestino)

        modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"

    Public ReadOnly Property Causa() As CausaDN
        Get
            Return mCausa
        End Get
    End Property

    Public ReadOnly Property Destino() As IDestinoDN
        Get
            Return mDestino
        End Get
    End Property

#End Region

#Region "Métodos de validación"

    Private Function ValidarCausa(ByRef mensaje As String, ByVal causa As CausaDN) As Boolean
        If causa Is Nothing Then
            mensaje = "La causa de la suscripción no puede ser nula"
            Return False
        End If
        Return True
    End Function

    Private Function ValidarDestino(ByRef mensaje As String, ByVal destino As IDestinoDN) As Boolean
        If destino Is Nothing Then
            mensaje = "El destino de la suscripción no puede ser nulo"
            Return False
        End If
        Return True
    End Function

#End Region

#Region "Métodos"



#End Region

End Class
