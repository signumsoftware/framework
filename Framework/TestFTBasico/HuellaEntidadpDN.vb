
Imports Framework.DatosNegocio

Public Class HuellaEntidadpDN
    Inherits Framework.DatosNegocio.HEDN


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

        Dim ep As EntidadpDN = pEntidad

        Me.Valor = ep.Valor
    End Sub
End Class



Public Class ContenedoraHEDNContenidaDN
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mHEDN As Framework.DatosNegocio.HEDN

    <RelacionPropCampoAtribute("mHEDN")> _
    Public Property HuellaEntidadp() As Framework.DatosNegocio.HEDN

        Get
            Return mHEDN
        End Get

        Set(ByVal value As Framework.DatosNegocio.HEDN)
            CambiarValorRef(Of Framework.DatosNegocio.HEDN)(value, mHEDN)

        End Set
    End Property




End Class

Public Class ContenedoraHuellaDN
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mHuellaEntidadp As HuellaEntidadpDN

    <RelacionPropCampoAtribute("mHuellaEntidadp")> _
    Public Property HuellaEntidadp() As HuellaEntidadpDN

        Get
            Return mHuellaEntidadp
        End Get

        Set(ByVal value As HuellaEntidadpDN)
            CambiarValorRef(Of HuellaEntidadpDN)(value, mHuellaEntidadp)

        End Set
    End Property




End Class