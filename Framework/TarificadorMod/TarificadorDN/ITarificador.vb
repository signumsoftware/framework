Imports Framework.Operaciones.OperacionesDN
Public Interface ITarificador

    Function Tarificar(ByVal recuperador As IRecSumiValorLN, ByVal pTipoOPConf As TipoOperacionConfiguradaDN) As OperacionConfiguradaDN

End Interface
