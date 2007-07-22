<Serializable()> _
Public Class TruncarOperadorDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IOperadorDN

    Public Sub New()
        Me.mNombre = "truncar"
    End Sub

    ''' <summary>
    ''' método que devuelve el resultado de una operación de truncar un valor
    ''' </summary>
    ''' <param name="valor1">Valor a truncar</param>
    ''' <param name="valor2">Número de dígitos de precisión para truncar</param>
    ''' <returns>valor resultante de la operación truncar el valor1 con los decimales indicados por valor2</returns>
    ''' <remarks></remarks>
    Public Function Ejecutar(ByVal valor1 As Object, ByVal valor2 As Object) As Object Implements IOperadorDN.Ejecutar
        Dim aux1 As Double
        Dim aux2 As Integer

        If Not Double.TryParse(valor1, aux1) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("El valor a truncar debe ser un número entero")
        End If

        If Not Integer.TryParse(valor2, aux2) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("El valor de los decimales a truncar debe ser un número entero")
        End If

        aux2 = Math.Pow(10, aux2)

        Return (Math.Truncate(aux1 * aux2)) / aux2

    End Function

End Class
