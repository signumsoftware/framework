
Public Interface INodoConHijosTDN(Of T)
    Inherits IEntidadDN
#Region "Propieadades"
    Property HijosNcH() As ColINodoConHijosTDN(Of T)
#End Region

#Region "Metodos"
    Function NodoContenedor(ByVal pElemento As Object, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As INodoConHijosTDN(Of T)
    Function Contiene(ByVal pElemento As Object, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As Boolean
    Sub AñadirHijo(ByVal hijo As INodoConHijosTDN(Of T))
    Sub AñadirHijo(ByVal hijos As ColINodoConHijosTDN(Of T))
    Function Eliminar(ByVal hijo As Object, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As ArrayList
    Function ProfundidadMaxDescendenia() As Int16
    Function PodarNodosHijosNoContenedoresHijos(ByVal phijos As ColINodoConHijosTDN(Of T), ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As ColINodoConHijosTDN(Of T)

#End Region

End Interface

Public Class ColINodoConHijosTDN(Of T)
    Inherits ArrayListValidable(Of INodoConHijosTDN(Of T))
End Class