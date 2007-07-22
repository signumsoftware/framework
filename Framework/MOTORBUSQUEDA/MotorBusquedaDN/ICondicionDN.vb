
Public Interface ICondicionDN
    Property Factor1() As ICondicionDN
    Property Factor2() As ICondicionDN
    Property OperadorRelacional() As OperadoresRelacionales
    Function evaluacion() As Boolean
End Interface



<Serializable()> _
Public Class ColICondicionDN

    Inherits List(Of ICondicionDN)


End Class