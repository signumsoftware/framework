Public Interface IValorMap
    Inherits Framework.DatosNegocio.IEntidadDN
    Property Caracteristica() As Cuestionario.CuestionarioDN.CaracteristicaDN
    Property Valor() As Object
    Function TraduceElValor(ByVal pValor As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN) As Boolean
End Interface
<Serializable()> _
Public Class ColIValorMap
    Inherits Framework.DatosNegocio.ArrayListValidable(Of IValorMap)
End Class