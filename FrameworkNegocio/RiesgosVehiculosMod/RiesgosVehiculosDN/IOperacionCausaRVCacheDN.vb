
Imports Framework.DatosNegocio
Public Interface IOperacionCausaRVCacheDN
    Inherits IOperacionCache
    Property Fraccionable() As Boolean
    Property GUIDTarifa() As String
    Property Aplicado() As Boolean
    Property TipoOperador() As String
    Property ValorresultadoOpr() As Double
    Property GUIDCobertura() As String
    ReadOnly Property GUIDsCausas() As String
    ReadOnly Property ColHeCausas() As Framework.DatosNegocio.ColHEDN

    Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN)
End Interface

Public Interface IOperacionCache
    Inherits Framework.DatosNegocio.IEntidadDN
    Property GUIDReferida() As String
End Interface


<Serializable()> _
Public Class ColIOperacionCausaRVCacheDN
    Inherits ArrayListValidable(Of IOperacionCausaRVCacheDN)



    Public Function CalcuarTotalValorresultadoOpr() As Decimal

        Dim total As Decimal
        For Each entidad As IOperacionCausaRVCacheDN In Me
            total += entidad.ValorresultadoOpr
        Next
        Return total

    End Function

End Class




