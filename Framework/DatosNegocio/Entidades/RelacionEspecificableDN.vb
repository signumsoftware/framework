Public Class RelacionEspecificableDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.DatosNegocio.IRelacionPorcentual

    Protected mNombreTipoRelacion As String
    Protected mPorcentajeRelacion As Double
    Protected mEntidadReferida As IEntidadDN
    Protected mEntidadReferidora As IEntidadDN

    Public Sub New()
        Me.modificarEstado = EstadoDatosDN.Inconsistente
    End Sub
    Public Sub New(ByVal pNombreTipoRelacion As String)
        mNombreTipoRelacion = pNombreTipoRelacion
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Overridable Property EntidadReferida() As IEntidadDN Implements IRelacionPorcentual.EntidadReferida
        Get

        End Get
        Set(ByVal value As IEntidadDN)

        End Set
    End Property

    Public Overridable Property EntidadReferidora() As IEntidadDN Implements IRelacionPorcentual.EntidadReferidora
        Get

        End Get
        Set(ByVal value As IEntidadDN)

        End Set
    End Property

    Public Overridable ReadOnly Property NombreTipoRelacion() As String Implements IRelacionPorcentual.NombreTipoRelacion
        Get
            Return mNombreTipoRelacion
        End Get
    End Property

    Public Overridable Property PorcentajeRelacion() As Double Implements IRelacionPorcentual.PorcentajeRelacion
        Get

        End Get
        Set(ByVal value As Double)

        End Set
    End Property

    Protected Overridable Function ValEntidadReferidora(ByRef mensaje As String, ByVal pEntidadReferidora As IEntidadDN) As Boolean

    End Function

End Class


<Serializable()> Public Class ColRelacionEspecificableDN
    Inherits ArrayListValidable(Of RelacionEspecificableDN)

    ' metodos de coleccion
    '
End Class
