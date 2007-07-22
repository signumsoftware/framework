Imports Framework.DatosNegocio

<Serializable()> _
Public Class CausaDN
    Inherits EntidadDN

#Region "Atributos"
    Protected mValorCausa As String
#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal valorCausa As String)
        Dim mensaje As String
        mensaje = ""

        If Not ValidarCausa(mensaje, valorCausa) Then
            Throw New ApplicationExceptionDN(mensaje)
        End If

        CambiarValorVal(Of String)(valorCausa, mValorCausa)

        modificarEstado = EstadoDatosDN.Modificado
    End Sub

#End Region

#Region "Propiedades"
    Public ReadOnly Property Causa() As String
        Get
            Return mValorCausa
        End Get
    End Property
#End Region

#Region "Métodos de validación"

    Private Function ValidarCausa(ByRef mensaje As String, ByVal valorCausa As String) As Boolean
        If String.IsNullOrEmpty(Causa) Then
            mensaje = "El valor asignado a la causa no puede ser nulo"
            Return False
        End If

        Return True
    End Function

#End Region

End Class
