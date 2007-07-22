Public Interface IIntvaloNumerico
    Property ValInf() As Double
    Property ValSup() As Double
    Function Contiene(ByVal pValor As Double) As Boolean
    Function SolapadoOContenido(ByVal pIntervalo As IIntvaloNumerico) As IntSolapadosOContenido
    Function BienFormado() As Boolean
End Interface



Public Enum IntSolapadosOContenido
    Libres
    Iguales
    Solapados
    Contenido
    Contenedor
End Enum