Imports Framework.DatosNegocio

<Serializable()> _
Public Class CaracteristicaDN
    Inherits Framework.DatosNegocio.EntidadDN



    Private mTipoCaracteristica As TipoCaracteristica





    Protected mPadre As CaracteristicaDN

    <RelacionPropCampoAtribute("mPadre")> _
    Public Property Padre() As CaracteristicaDN

        Get
            Return mPadre
        End Get

        Set(ByVal value As CaracteristicaDN)
            CambiarValorRef(Of CaracteristicaDN)(value, mPadre)

        End Set
    End Property



    Public Property TipoCaracteristica() As TipoCaracteristica

        Get
            Return mTipoCaracteristica
        End Get

        Set(ByVal value As TipoCaracteristica)
            CambiarValorVal(Of TipoCaracteristica)(value, mTipoCaracteristica)
        End Set
    End Property







End Class




<Serializable()> _
Public Class ColCaracteristicaDN
    Inherits ArrayListValidable(Of CaracteristicaDN)

End Class




