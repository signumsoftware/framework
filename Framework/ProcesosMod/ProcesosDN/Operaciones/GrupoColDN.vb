
<Serializable()> _
Public Class GrupoColDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"
    Protected mValor As IList
#End Region

#Region "Propiedades"

    Public Property Valor() As IList
        Get
            Return mValor
        End Get
        Set(ByVal value As IList)
            CambiarValorCol(Of IList)(value, mValor)
        End Set
    End Property

#End Region

End Class

<Serializable()> _
Public Class GrupoDTSDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"
    Protected mValor As DataSet
#End Region

#Region "Propiedades"

    Public Property Valor() As DataSet
        Get
            Return mValor
        End Get
        Set(ByVal value As DataSet)
            CambiarValorVal(Of DataSet)(value, mValor)
        End Set
    End Property

#End Region

End Class