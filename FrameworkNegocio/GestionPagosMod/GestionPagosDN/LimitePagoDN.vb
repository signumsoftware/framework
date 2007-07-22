Imports Framework.DatosNegocio

<Serializable()> _
Public Class LimitePagoDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mLimiteAviso As Single
    'Protected mLimiteValidacion As Single
    Protected mLimiteFirmaAutomatica As Single

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

#End Region

#Region "Propiedades"

    Public Property LimiteFirmaAutomatica() As Single
        Get
            Return mLimiteFirmaAutomatica
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(Of Single)(value, mLimiteFirmaAutomatica)
        End Set
    End Property

    Public Property LimiteAviso() As Single
        Get
            Return mLimiteAviso
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(Of Single)(value, mLimiteAviso)
        End Set
    End Property

    'Public Property LimiteValidacion() As Single
    '    Get
    '        Return mLimiteValidacion
    '    End Get
    '    Set(ByVal value As Single)
    '        Me.CambiarValorVal(Of Single)(value, mLimiteValidacion)

    '    End Set
    'End Property

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Return "Límite aviso: " & FormatCurrency(mLimiteAviso) & ", Límite firma auto: " & FormatCurrency(mLimiteFirmaAutomatica)
    End Function

#End Region

End Class
