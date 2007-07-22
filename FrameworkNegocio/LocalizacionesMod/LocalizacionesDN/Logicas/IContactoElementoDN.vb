
''' <summary>
''' Esta interface proporciona los diferentes tipos de contacto
''' </summary>
''' <remarks>
''' tipo
''' Valor
''' </remarks>
Public Interface IDatoContactoDN
    Inherits Framework.DatosNegocio.IEntidadDN

    Property Tipo() As String
    Property Valor() As String
    Property Comentario() As String

End Interface


<Serializable()> Public Class ColIDatosContactoDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of IDatoContactoDN)

End Class