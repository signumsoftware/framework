<Serializable()> _
Public Class MultiplicacionOperadorDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IOperadorDN
    Public Sub New()
        Me.mNombre = "Multiplicar"
    End Sub
    Public Function Ejecutar(ByVal valor1 As Object, ByVal valor2 As Object) As Object Implements IOperadorDN.Ejecutar
        Dim op1 As Decimal = valor1
        Dim op2 As Decimal = valor2
        Dim resultado As Decimal = op1 * op2

        Return resultado
    End Function
End Class
