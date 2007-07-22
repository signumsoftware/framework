Imports Framework.DatosNegocio

<Serializable()> _
Public Class TrabajoDN
    Inherits EntidadTemporalDN

#Region "Atrbutos"

    Protected mRelAgenteServicio As RelAgenteServicioDN
    Protected mFechaComunicacion As Date
    Protected mFechaProgramada As Date

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mRelAgenteServicio")> _
    Public Property RelAgenteServicio() As RelAgenteServicioDN
        Get
            Return mRelAgenteServicio
        End Get
        Set(ByVal value As RelAgenteServicioDN)
            CambiarValorRef(Of RelAgenteServicioDN)(value, mRelAgenteServicio)
        End Set
    End Property

    Public Property FechaComunicacion() As Date
        Get
            Return mFechaComunicacion
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaComunicacion)
        End Set
    End Property

    Public Property FechaProgramada() As Date
        Get
            Return mFechaProgramada
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaProgramada)
        End Set
    End Property

#End Region

End Class


<Serializable()> _
Public Class ColTrabajoDN
    Inherits ArrayListValidable(Of TrabajoDN)

End Class
