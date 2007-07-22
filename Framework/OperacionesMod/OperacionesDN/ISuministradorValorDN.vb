
Public Interface ISuministradorValorDN

    Property IRecSumiValorLN() As IRecSumiValorLN
    ReadOnly Property ValorCacheado() As Object
    Function GetValor() As Object
    Function RecuperarOrden() As Integer

End Interface
