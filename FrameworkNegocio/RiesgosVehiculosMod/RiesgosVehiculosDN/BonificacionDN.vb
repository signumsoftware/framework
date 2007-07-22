Imports Framework.DatosNegocio

<Serializable()> _
Public Class BonificacionDN
    Inherits EntidadDN

    Protected mNoRequerido As Boolean

    Public Property NoRequerido() As Boolean
        Get
            Return mNoRequerido
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mNoRequerido)
        End Set
    End Property

End Class


<Serializable()> _
Public Class ColBonificacionDN
    Inherits ArrayListValidable(Of BonificacionDN)

End Class