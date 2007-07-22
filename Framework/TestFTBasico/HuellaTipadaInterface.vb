Imports Framework.DatosNegocio
Public Class HuellaTipadaInterfaceA
    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of InterfaceA)



End Class



Public Class ContenedoraHuellaTipadaInterfaceA
    Inherits Framework.DatosNegocio.EntidadDN


    Protected mHuellaTipadaInterfacea As HuellaTipadaInterfaceA

    <RelacionPropCampoAtribute("mHuellaTipadaInterfacea")> _
    Public Property HuellaTipadaInterfacea() As HuellaTipadaInterfaceA

        Get
            Return mHuellaTipadaInterfacea
        End Get

        Set(ByVal value As HuellaTipadaInterfaceA)
            CambiarValorRef(Of HuellaTipadaInterfaceA)(value, mHuellaTipadaInterfacea)

        End Set
    End Property





End Class



Public Interface InterfaceA
    Inherits Framework.DatosNegocio.IEntidadDN
    Property valor() As Int16
End Interface



Public Class implemtaC1InterfaceA
    Inherits Framework.DatosNegocio.EntidadDN
    Implements InterfaceA
    Protected mvalor As Short

    Public Property valor() As Short Implements InterfaceA.valor
        Get
            Return mvalor
        End Get
        Set(ByVal value As Short)
            Me.CambiarValorVal(Of Short)(value, mvalor)
        End Set
    End Property
End Class

Public Class implemtaC2InterfaceA
    Inherits Framework.DatosNegocio.EntidadDN
    Implements InterfaceA
    Protected mvalor As Short

    Public Property valor() As Short Implements InterfaceA.valor
        Get
            Return mvalor
        End Get
        Set(ByVal value As Short)
            Me.CambiarValorVal(Of Short)(value, mvalor)
        End Set
    End Property
End Class