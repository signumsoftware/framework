Imports Framework.DatosNegocio

<Serializable()> _
Public Class ComisionDN
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mFraccionable As Boolean

    Public Property Fraccionable() As Boolean

        Get
            Return mFraccionable
        End Get

        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mFraccionable)

        End Set
    End Property

End Class

<Serializable()> _
Public Class ColComisionDN
    Inherits ArrayListValidable(Of ComisionDN)

End Class