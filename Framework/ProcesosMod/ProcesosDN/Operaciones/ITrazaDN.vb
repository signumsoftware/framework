Public Interface ITrazaDN
    Inherits Framework.DatosNegocio.IEntidadDN
    Sub TrazarEntidad(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN)
End Interface


Public Class ColITrazaDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of ITrazaDN)
End Class