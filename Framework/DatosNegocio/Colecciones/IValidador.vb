''' <summary>Esta interface permite definir objetos que validan otros objetos.</summary>
Public Interface IValidador

#Region "Metodos"
    ''' <summary>Indica si un objeto es valido o no.</summary>
    ''' <param name="pValor" type="Object">
    ''' Objeto del que vamos a comprobar si su tipo es aceptado por este validador.
    ''' </param>
    ''' <returns>Si el objeto es valido o no.</returns>
    Function Validacion(ByRef mensaje As String, ByVal pValor As Object) As Boolean

    ''' <summary>Devuelve la formula de validacion que usa este validador.</summary>
    ''' <remarks>Se utiliza para saber si dos validadores son iguales (realizan la misma validacion.</remarks>
    Function Formula() As String
#End Region

End Interface


