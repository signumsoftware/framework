Public Class Conversores

    ''' <summary>
    ''' Convierte la cantidad en pulgadas en milímetros
    ''' </summary>
    ''' <param name="pulgadas">la cantidad a convertir</param>
    ''' <returns>la cantidad convertida</returns>
    Public Shared Function PulgadasAMilimetros(ByVal pulgadas As Double) As Double
        Return pulgadas * 25.4
    End Function

    ''' <summary>
    ''' Convierte las pulgadas en centímetros
    ''' </summary>
    ''' <param name="pulgadas">la cantidad a convertir</param>
    ''' <returns>la cantidad convertida</returns>
    Public Shared Function PulgadasACentimetros(ByVal pulgadas As Double) As Double
        Return pulgadas * 2.54
    End Function

    ''' <summary>
    ''' Convierte los milímetros en pulgadas
    ''' </summary>
    ''' <param name="milimetros">la cantidad a convertir</param>
    ''' <returns>la cantidad convertida</returns>
    Public Shared Function MilimetrosAPulgadas(ByVal milimetros As Double) As Double
        Return milimetros * 0.03937
    End Function

    ''' <summary>
    ''' Convierte los centímetros en pulgadas
    ''' </summary>
    ''' <param name="centimetros">la cantidad a convertir</param>
    ''' <returns>la cantidad convertida</returns>
    Public Shared Function CentimetrosAPulgadas(ByVal centimetros As Double) As Double
        Return centimetros * 0.00328
    End Function

End Class
