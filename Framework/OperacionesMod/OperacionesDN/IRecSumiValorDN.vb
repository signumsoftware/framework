''' <summary>
''' interface encargada de
''' 1º recuperar el siministrador de valor dado una operación y el operando a recuperar
''' 2º suministrar las fuentes de datos a los isuministradores de valor
''' </summary>
''' <remarks></remarks>
Public Interface IRecSumiValorLN
    Function getSuministradorValor(ByVal pOperacion As OperacionesDN.IOperacionSimpleDN, ByVal posicion As OperacionesDN.PosicionOperando) As OperacionesDN.ISuministradorValorDN
    Property DataSoucers() As IList 'las fuentes de datos donde los recuperadores de valor pueden buscar los valores vase para devolver su valor a partir de una condicion
    Property DataResults() As IList 'los valores resuktado que se dean cachear
    Function CachearElemento(ByVal pElementos As Framework.DatosNegocio.IEntidadDN) As IList
End Interface
