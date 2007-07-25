<Serializable()> _
Public Class SumValOperMapDN
    Inherits Framework.DatosNegocio.EntidadDN


    Public Property Operacion() As Framework.TiposYReflexion.DN.VinculoClaseDN
        Get

        End Get
        Set(ByVal value As Framework.TiposYReflexion.DN.VinculoClaseDN)

        End Set
    End Property




    Public Property SuministradorValor() As Framework.TiposYReflexion.DN.VinculoClaseDN
        Get

        End Get
        Set(ByVal value As Framework.TiposYReflexion.DN.VinculoClaseDN)

        End Set
    End Property



    Public Property PosicionOperando() As PosicionOperando
        Get

        End Get
        Set(ByVal value As PosicionOperando)

        End Set
    End Property
End Class

<Serializable()> _
Public Class ColSumValOperMapDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of SumValOperMapDN)
End Class