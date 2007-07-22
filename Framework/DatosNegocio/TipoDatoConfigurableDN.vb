
<Serializable()> Public Class TipoDatoConfigurableDN
    Inherits EntidadBaseDN

#Region "Atributos"
    Protected mEsNumerico As Boolean
#End Region

#Region "Propiedades"
    Public Property EsNumerico() As Boolean
        Get
            Return mEsNumerico
        End Get
        Set(ByVal value As Boolean)
            mEsNumerico = value
        End Set
    End Property
#End Region

End Class
