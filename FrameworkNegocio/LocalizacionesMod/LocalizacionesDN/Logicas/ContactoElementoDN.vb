#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

<Serializable()> _
Public Class ContactoGenericoDN
    Inherits EntidadDN
    Implements IDatoContactoDN

#Region "Atributos"

    Protected mTipo As String
    Protected mValor As String
    Protected mComentario As String

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

#End Region

#Region "Propiedades"

    Public Property Tipo() As String Implements IDatoContactoDN.Tipo
        Get
            Return mTipo
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mTipo)
        End Set
    End Property

    Public Property Valor() As String Implements IDatoContactoDN.Valor
        Get
            Return mValor
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mValor)
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

    
End Class
