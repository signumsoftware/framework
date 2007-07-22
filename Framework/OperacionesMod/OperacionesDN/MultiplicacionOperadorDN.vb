
<Serializable()> _
Public Class MultiplicacionOperadorDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IOperadorDN
    Public Sub New()
        Me.mNombre = "Multiplicar"
    End Sub
    Public Function Ejecutar(ByVal valor1 As Object, ByVal valor2 As Object) As Object Implements IOperadorDN.Ejecutar
        Return valor1 * valor2

    End Function
End Class
