''' <summary>Esta interface permite definir objetos que pueden validarse.</summary>
Public Interface IValidable

#Region "Propiedades"
    ''' <summary>Obtiene el validador que valida este objeto.</summary>
    ReadOnly Property Validador() As IValidador
#End Region

#Region "Metodos"
    ''' <summary>
    ''' Indica si la validacion que se realiza sobre este objeto es la misma que la del validador
    ''' pasado por parametro.
    ''' </summary>
    ''' <param name="pValidador" type="IValidador">
    ''' El validador contra el que vamos a comparar.
    ''' </param>
    ''' <returns>Si la validacion que realizan los dos validadores es la misma o no.</returns>
    Function ValidacionIdentica(ByVal pValidador As IValidador) As Boolean
#End Region

End Interface
