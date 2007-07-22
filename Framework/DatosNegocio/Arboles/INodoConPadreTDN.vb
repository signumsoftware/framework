Namespace Arboles
    Public Interface INodoConPadreTDN(Of T)
        Inherits IEntidadDN

#Region "Propieadades"
        Property PadreNcP() As INodoConPadreTDN(Of T)
#End Region

#Region "Metodos"
        Function NodoContenedor(ByVal phijo As INodoConPadreTDN(Of T)) As INodoConPadreTDN(Of T)

        Function Profundidad() As Int64
        Function RaizArbol() As INodoConPadreTDN(Of T)
#End Region

    End Interface

End Namespace
