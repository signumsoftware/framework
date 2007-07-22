Imports Framework.DatosNegocio
<Serializable()> _
Public Class LiquidadorConcretoOrigenIDMapDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

    'Protected mIOrigen As IOrigenIImporteDebidoDN
    Protected mVCOrigenImpdev As Framework.TiposYReflexion.DN.VinculoClaseDN
    Protected mVCLiquidadorConcreto As Framework.TiposYReflexion.DN.VinculoClaseDN

    'Public Property IOrigen() As IOrigenIImporteDebidoDN
    '    Get
    '        Return (Me.mIOrigen)
    '    End Get
    '    Set(ByVal value As IOrigenIImporteDebidoDN)
    '        Me.CambiarValorRef(Of IOrigenIImporteDebidoDN)(value, Me.mIOrigen)
    '    End Set
    'End Property

    Public Property VCLiquidadorConcreto() As Framework.TiposYReflexion.DN.VinculoClaseDN
        Get
            Return Me.mVCLiquidadorConcreto
        End Get
        Set(ByVal value As Framework.TiposYReflexion.DN.VinculoClaseDN)
            Me.CambiarValorRef(Of Framework.TiposYReflexion.DN.VinculoClaseDN)(value, Me.mVCLiquidadorConcreto)
        End Set
    End Property

    Public Property VCOrigenImpdev() As Framework.TiposYReflexion.DN.VinculoClaseDN
        Get
            Return Me.mVCOrigenImpdev
        End Get
        Set(ByVal value As Framework.TiposYReflexion.DN.VinculoClaseDN)
            Me.CambiarValorRef(Of Framework.TiposYReflexion.DN.VinculoClaseDN)(value, Me.mVCOrigenImpdev)
        End Set
    End Property

End Class




<Serializable()> _
Public Class ColLiquidadorConcretoOrigenIDMapDN
    Inherits ArrayListValidable(Of LiquidadorConcretoOrigenIDMapDN)

End Class




