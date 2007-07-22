Imports Framework.DatosNegocio

<Serializable()> _
Public Class ModuladorDN
    Inherits Framework.DatosNegocio.EntidadDN



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
Public Class ColModuladorDN
    Inherits ArrayListValidable(Of ModuladorDN)

End Class




