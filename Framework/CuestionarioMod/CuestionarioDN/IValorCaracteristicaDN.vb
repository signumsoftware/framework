
Public Interface IValorCaracteristicaDN
    Inherits Framework.DatosNegocio.IEntidadDN

    Property Caracteristica() As CaracteristicaDN
    Property Valor() As Object
    Property ValorCaracPadre() As IValorCaracteristicaDN
    Property FechaEfectoValor() As Date

    Function ClonarIValorCaracteristica() As IValorCaracteristicaDN


End Interface

Public Class colIValorCaracteristicaDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of IValorCaracteristicaDN)
End Class