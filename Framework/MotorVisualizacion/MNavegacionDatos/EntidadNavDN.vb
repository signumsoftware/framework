<Serializable()> Public Class EntidadNavDN
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mVinculoClase As Framework.TiposYReflexion.DN.VinculoClaseDN

    Public Sub New()
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente
    End Sub


    Public Sub New(ByVal pTipo As Type)
        Me.CambiarValorRef(Of Framework.TiposYReflexion.DN.VinculoClaseDN)(New Framework.TiposYReflexion.DN.VinculoClaseDN(pTipo), mVinculoClase)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub
    Public Property VinculoClase() As Framework.TiposYReflexion.DN.VinculoClaseDN
        Get
            Return Me.mVinculoClase
        End Get
        Set(ByVal value As Framework.TiposYReflexion.DN.VinculoClaseDN)

        End Set
    End Property

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mVinculoClase Is Nothing Then

            pMensaje = " mVinculoClase Is Nothing"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente

        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function
End Class




<Serializable()> Public Class ColEntidadNavDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of EntidadNavDN)

End Class