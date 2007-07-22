Imports Framework.DatosNegocio
<Serializable()> Public Class PlazoEfectoDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN



    Protected mModalidadDePago As ModalidadPago
    Protected mPlazoEjecucion As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias


    Public Sub New()
        CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)(New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias, mPlazoEjecucion)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Property ModalidadDePago() As ModalidadPago

        Get
            Return mModalidadDePago
        End Get

        Set(ByVal value As ModalidadPago)
            CambiarValorVal(Of ModalidadPago)(value, mModalidadDePago)

        End Set
    End Property






    ''' <summary>
    ''' este valor es usado para crear la "fecha de efecto esperada" junto con la feccha de emision 
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <RelacionPropCampoAtribute("mPlazoEjecucion")> _
    Public Property PlazoEjecucion() As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias

        Get
            Return mPlazoEjecucion
        End Get

        Set(ByVal value As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)
            CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)(value, mPlazoEjecucion)

        End Set
    End Property







End Class





<Serializable()> _
Public Class ColPlazoEfectoDN
    Inherits ArrayListValidable(Of PlazoEfectoDN)

End Class




