
<Serializable()> _
Public Class MultRedondeoOperadorDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IOperadorDN

    Public Sub New()
        Me.mNombre = "Multiplicar Redondeo a 2"
    End Sub

    Public Function Ejecutar(ByVal valor1 As Object, ByVal valor2 As Object) As Object Implements IOperadorDN.Ejecutar
        Return Math.Round(valor1 * valor2, 2, MidpointRounding.AwayFromZero)
    End Function

End Class
