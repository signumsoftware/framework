Namespace Arboles


    Public Interface INodoTDN(Of T As {IEntidadBaseDN, Class})
        Inherits IEntidadDN, INodoConHijosTDN(Of T), INodoConPadreTDN(Of T)

#Region "Propieadades"
        Property Hijos() As IArrayListValidable(Of INodoTDN(Of T))
        Property Padre() As INodoTDN(Of T)

#End Region

#Region "Metodos"
        Function ProfundidadMaximaArbol() As Int64
        Function ContenidoEnArbol(ByVal pElemento As Object, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As Boolean
        Function RecuperarColHojasConenidas() As ArrayListValidable(Of T)
#End Region

    End Interface

    <Serializable()> _
    Public Class ColINodoTDN(Of T As {IEntidadBaseDN, Class})
        Inherits ArrayListValidable(Of INodoTDN(Of T))
        Implements IColNodos

    End Class

End Namespace
