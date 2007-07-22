Imports Framework.DatosNegocio

<Serializable()> Public Class PaginaWebDN
    Inherits EntidadDN
    Implements IDatoContactoDN

#Region "Atributos"

    Protected mDireccionWeb As String
    Protected mComentario As String

#End Region

#Region "Propiedades"

    Public Property Tipo() As String Implements IDatoContactoDN.Tipo
        Get
            Return Me.GetType.ToString()
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Property Valor() As String Implements IDatoContactoDN.Valor
        Get
            Return mDireccionWeb
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mDireccionWeb)
        End Set
    End Property

    Public Property Comentario() As String Implements IDatoContactoDN.Comentario
        Get
            Return mComentario
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mComentario)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mDireccionWeb = String.Empty Then
            pMensaje = "La dirección de la página Web no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class
