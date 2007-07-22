Imports Framework.DatosNegocio

Public Interface IDestinoDN
    Inherits IEntidadDN

    ReadOnly Property Direccion() As String
    ReadOnly Property Canal() As String

End Interface

<Serializable()> _
Public Class ColIDestinos
    Inherits ArrayListValidable(Of IDestinoDN)

End Class