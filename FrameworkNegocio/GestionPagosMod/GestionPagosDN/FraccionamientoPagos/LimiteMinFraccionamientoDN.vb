Imports Framework.DatosNegocio

<Serializable()> _
Public Class LimiteMinFraccionamientoDN
    Inherits EntidadTemporalDN

#Region "Atributos"

    Protected mFraccionamiento As FraccionamientoDN
    Protected mValorMinimoFrac As Double

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mFraccionamiento")> _
    Public Property Fraccionamiento() As FraccionamientoDN
        Get
            Return mFraccionamiento
        End Get
        Set(ByVal value As FraccionamientoDN)
            CambiarValorRef(Of FraccionamientoDN)(value, mFraccionamiento)
        End Set
    End Property

    Public Property ValorMinimoFrac() As Double
        Get
            Return mValorMinimoFrac
        End Get
        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorMinimoFrac)
        End Set
    End Property

#End Region

End Class


<Serializable()> _
Public Class ColLimiteMinFraccionamientoDN
    Inherits ArrayListValidable(Of LimiteMinFraccionamientoDN)

End Class