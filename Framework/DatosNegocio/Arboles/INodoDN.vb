Namespace Arboles
    Public Interface INodoDN
        Inherits IEntidadDN, Arboles.INodoConHijosDN, Arboles.INodoConPadreDN

#Region "Propieadades"
        Property Hijos() As IColNodos
        Property Padre() As INodoDN
        Property ColHojas() As IList

#End Region

#Region "Metodos"
        Function ProfundidadMaximaArbol() As Int64
        Function ContenidoEnArbol(ByVal hijo As INodoDN) As Boolean
#End Region

    End Interface




    Public Interface INodoConHijosDN
        Inherits IEntidadDN

#Region "Propieadades"
        Property HijosNcH() As IColNodos
#End Region

#Region "Metodos"
        Function NodoContenedorHijo(ByVal hijo As INodoDN) As INodoDN
        Function ContieneHijo(ByVal hijo As INodoDN) As Boolean
        Sub AñadirHijo(ByVal hijo As INodoDN)
        Sub AñadirHijo(ByVal hijos As ColNodosDN)
        Function EliminarHijo(ByVal hijo As INodoDN) As IList
        Function ProfundidadMaxDescendenia() As Int16
        Function RecuperarNodoXGUID(ByVal pGUID As String) As INodoDN


#End Region

    End Interface





    Public Interface INodoConPadreDN
        Inherits IEntidadDN

#Region "Propieadades"
        Property PadreNcP() As INodoConPadreDN
#End Region

#Region "Metodos"
        Function Profundidad() As Int64
        Function RaizArbol() As INodoDN
#End Region

    End Interface
End Namespace
