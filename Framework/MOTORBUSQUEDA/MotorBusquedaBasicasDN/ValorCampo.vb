
<Serializable()> _
 Public Class ValorCampo

    Public NombreCampo As String
    Public Valor As String
    Public Operador As OperadoresAritmeticos
    Public Eliminable As Boolean = True
End Class


Public Enum OperadoresAritmeticos
    igual
    distinto
    mayor
    menor
    mayor_igual
    menor_igual
    contener_texto
End Enum