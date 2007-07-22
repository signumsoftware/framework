Public Interface IRelacionPorcentual
    Inherits Framework.DatosNegocio.IEntidadDN
    Property EntidadReferida() As IEntidadDN
    Property EntidadReferidora() As IEntidadDN
    Property PorcentajeRelacion() As Double
    ReadOnly Property NombreTipoRelacion() As String
End Interface
