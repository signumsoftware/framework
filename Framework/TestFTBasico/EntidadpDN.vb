Imports Framework.DatosNegocio
Public Class EntidadpDN
    Inherits Framework.DatosNegocio.EntidadDN




    Protected mValor As Double

    Public Property Valor() As Double

        Get
            Return mValor
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValor)

        End Set
    End Property




End Class




<Serializable()> _
Public Class ColEntidadpDN
    Inherits ArrayListValidable(Of EntidadpDN)

End Class





Public Class MuchosEntidadpDN
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mColMuchasEntidadp As ColEntidadpDN

    Public Sub New()
        Me.CambiarValorRef(Of ColEntidadpDN)(New ColEntidadpDN, mColMuchasEntidadp)
    End Sub





    <RelacionPropCampoAtribute("mColMuchasEntidadp")> _
    Public Property ColMuchasEntidadp() As ColEntidadpDN

        Get
            Return mColMuchasEntidadp
        End Get

        Set(ByVal value As ColEntidadpDN)
            CambiarValorRef(Of ColEntidadpDN)(value, mColMuchasEntidadp)

        End Set
    End Property







End Class