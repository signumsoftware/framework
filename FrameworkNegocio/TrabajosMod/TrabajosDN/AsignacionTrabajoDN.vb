Imports Framework.DatosNegocio

<Serializable()> _
Public Class AsignacionTrabajoDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mTrabajo As TrabajoDN
    Protected mEntidadAsignada As IEntidadDN

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mTrabajo")> _
    Public Property Trabajo() As TrabajoDN
        Get
            Return mTrabajo
        End Get
        Set(ByVal value As TrabajoDN)
            CambiarValorRef(Of TrabajoDN)(value, mTrabajo)
        End Set
    End Property

    <RelacionPropCampoAtribute("mEntidadAsignada")> _
    Public Property EntidadAsignada() As IEntidadDN
        Get
            Return mEntidadAsignada
        End Get
        Set(ByVal value As IEntidadDN)
            CambiarValorRef(Of IEntidadDN)(value, mEntidadAsignada)
        End Set
    End Property

#End Region

End Class
