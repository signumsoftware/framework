<Serializable()> _
Public Class ConstatesConfigurablesSegurosDN

    Inherits Framework.DatosNegocio.EntidadTemporalDN


    Protected mValorMalificacionSiniestros As Double
    Protected mValorBonificacionSiniestros As Double

    Public Property ValorBonificacionSiniestros() As Double

        Get
            Return mValorBonificacionSiniestros
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorBonificacionSiniestros)

        End Set
    End Property


    Public Property ValorMalificacionSiniestros() As Double

        Get
            Return mValorMalificacionSiniestros
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorMalificacionSiniestros)

        End Set
    End Property


End Class

<Serializable()> _
Public Class ColConstatesConfigurablesSegurosDN
    Inherits Framework.DatosNegocio.ArrayListValidableEntTemp(Of ConstatesConfigurablesSegurosDN)

End Class


