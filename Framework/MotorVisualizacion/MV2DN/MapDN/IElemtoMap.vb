Public Interface IElemtoMap
    Property Ico() As String
    Property NombreVis() As String
    Property Editable() As Boolean
    ' Property ColComandoMap() As ColComandoMapDN

End Interface


Public Interface IElemtoMapContenedor
    Inherits IElemtoMap
    Property InstanciaContenedora() As IElemtoMap
    Property ColIElemtoMapContenedor() As List(Of IElemtoMapContenedor)
    Property ColIElemtoMap() As List(Of IElemtoMap)
    Function EliminarElementoMap(ByVal pElementoMap As IElemtoMap) As Boolean
    Function AñadirElementoMap(ByVal pElementoMap As IElemtoMap) As Boolean
    Function AñadirElementoMapEnRelacion(ByVal pElementoMap As IElemtoMap, ByVal pElementoMapRelacioando As IElemtoMap, ByVal pPosicion As Posicion) As Boolean

End Interface
Public Enum Posicion
    Antes
    Despues
End Enum