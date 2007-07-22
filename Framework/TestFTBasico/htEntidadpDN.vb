Imports Framework.DatosNegocio

Public Class HtEntidadpDN
    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of EntidadpDN)
    Protected mValor As Double

    Public Property Valor() As Double

        Get
            Return mValor
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValor)

        End Set
    End Property


    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN)
        MyBase.AsignarEntidadReferida(pEntidad)
        Dim miEntidad As EntidadpDN = pEntidad

        Me.Valor = miEntidad.Valor
    End Sub
End Class


Public Class ContenedoraHtEntidadpDN
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mHtEntidadp As HtEntidadpDN

    <RelacionPropCampoAtribute("mHtEntidadp")> _
    Public Property HuellaEntidadp() As HtEntidadpDN

        Get
            Return mHtEntidadp
        End Get

        Set(ByVal value As HtEntidadpDN)
            CambiarValorRef(Of HtEntidadpDN)(value, mHtEntidadp)

        End Set
    End Property




End Class