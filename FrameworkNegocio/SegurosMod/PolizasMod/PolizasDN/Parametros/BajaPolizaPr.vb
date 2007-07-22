<Serializable()> Public Class BajaPolizaPr
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mFechaBajaPropuesta As Date
    Protected mpr As PeriodoRenovacionPolizaDN

    Public Property pr() As PeriodoRenovacionPolizaDN
        Get
            Return mpr
        End Get
        Set(ByVal value As PeriodoRenovacionPolizaDN)
            Me.CambiarValorRef(Of PeriodoRenovacionPolizaDN)(value, mpr)
        End Set
    End Property

    Public Property FechaBajaPropuesta() As Date
        Get
            Return mFechaBajaPropuesta
        End Get
        Set(ByVal value As Date)
            Me.CambiarValorVal(Of Date)(value, mFechaBajaPropuesta)

        End Set
    End Property
End Class
