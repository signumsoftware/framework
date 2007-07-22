Imports Framework.DatosNegocio

Public Interface IRiesgoDN
    Inherits IEntidadDN

    ReadOnly Property EstadoConsistentePoliza() As Boolean
    Function RiesgoValidoPoliza() As Boolean
    'Function ClonarRiesgo() As IRiesgoDN

End Interface
