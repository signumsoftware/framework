Imports Framework.DatosNegocio

<Serializable()> Public Class IBANDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IValidador

#Region "Atributos"

    Protected mCodigo As String

#End Region

#Region "Propiedades"

    Public Property Codigo() As String
        Get
            Return mCodigo
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value.ToUpper(), mCodigo)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not Validacion(pMensaje, Me) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    'TODO: Añadir método shared para generar un objeto IBAN a partir de un CCCDN (de españa)

#End Region

    Public Function Formula() As String Implements Framework.DatosNegocio.IValidador.Formula
        Throw New NotImplementedException("Método no implementado")
    End Function

    Public Function Validacion(ByRef mensaje As String, ByVal pValor As Object) As Boolean Implements Framework.DatosNegocio.IValidador.Validacion
        Dim codIBAN As String
        Dim cuentaValidar As IBANDN
        Dim digitosPais As String
        Dim digitosControl As String
        Dim digitosCuenta As String

        If TypeOf (pvalor) Is IBANDN Then
            cuentaValidar = CType(pValor, IBANDN)
        Else
            Throw New ApplicationExceptionDN("El objeto no es del tipo IBANDN")
        End If

        If cuentaValidar Is Nothing Then
            mensaje = "Objeto no asignado"
            Return False
        End If

        codIBAN = cuentaValidar.Codigo

        If String.IsNullOrEmpty(codIBAN) Then
            Return True
        End If

        If codIBAN.Length > 34 Then
            mensaje = "El código IBAN no puede ser nulo, ni puede tener más de 34 caracteres alfanuméricos"
            Return False
        End If

        digitosPais = ConvertirCadena(codIBAN.Substring(0, 2))
        If digitosPais.Length < 4 Then
            mensaje = "El código del país no es correcto"
            Return False
        End If

        digitosControl = codIBAN.Substring(2, 2)
        If Not IsNumeric(digitosControl) Then
            mensaje = "Los dígitos de control no son correctos"
            Return False
        End If

        digitosCuenta = ConvertirCadena(codIBAN.Substring(4, codIBAN.Length - 4))

        codIBAN = digitosCuenta & digitosPais & digitosControl

        If CalcularResultadoIBAN(codIBAN) <> 1 Then
            mensaje = "El código IBAN no es correcto"
            Return False
        End If

        Return True

    End Function

    Private Function ConvertirCadena(ByVal cadena As String) As String
        Dim resultado As String = cadena

        resultado = resultado.Replace("A", 10)
        resultado = resultado.Replace("B", 11)
        resultado = resultado.Replace("C", 12)
        resultado = resultado.Replace("D", 13)
        resultado = resultado.Replace("E", 14)
        resultado = resultado.Replace("F", 15)

        resultado = resultado.Replace("G", 16)
        resultado = resultado.Replace("H", 17)
        resultado = resultado.Replace("I", 18)
        resultado = resultado.Replace("J", 19)
        resultado = resultado.Replace("K", 20)
        resultado = resultado.Replace("L", 21)

        resultado = resultado.Replace("M", 22)
        resultado = resultado.Replace("N", 23)
        resultado = resultado.Replace("O", 24)
        resultado = resultado.Replace("P", 25)
        resultado = resultado.Replace("Q", 26)
        resultado = resultado.Replace("R", 27)

        resultado = resultado.Replace("S", 28)
        resultado = resultado.Replace("T", 29)
        resultado = resultado.Replace("U", 30)
        resultado = resultado.Replace("V", 31)
        resultado = resultado.Replace("W", 32)
        resultado = resultado.Replace("X", 33)

        resultado = resultado.Replace("Y", 34)
        resultado = resultado.Replace("Z", 35)

        Return resultado

    End Function

    Private Function CalcularResultadoIBAN(ByVal cadena As String) As Integer
        Dim resultado As Integer
        Dim cadenaAux As String = ""
        Dim cadenaRestante As String = cadena
        Dim restoStr As String = ""

        If Not IsNumeric(cadena) Then
            Return -1
        End If

        Do
            cadenaAux = restoStr & cadenaRestante

            If cadenaAux.Length > 9 Then
                cadenaRestante = cadenaAux.Substring(9)
                cadenaAux = cadenaAux.Substring(0, 9)
            Else
                cadenaRestante = ""
            End If

            resultado = Integer.Parse(cadenaAux) Mod 97
            restoStr = resultado.ToString()

        Loop While (cadenaRestante <> "")

        Return resultado

    End Function

End Class

'<Serializable()> _
'Public Class ValidadorIBANDN
'    Implements IValidador

'    Public Function Formula() As String Implements Framework.DatosNegocio.IValidador.Formula
'        Throw New NotImplementedException("Método no implementado")
'    End Function

'    Public Function Validacion(ByRef mensaje As String, ByVal pValor As Object) As Boolean Implements Framework.DatosNegocio.IValidador.Validacion
'        Dim codIBAN As String
'        Dim cuentaValidar As IBANDN
'        Dim digitosPais As String
'        Dim digitosControl As String
'        Dim digitosCuenta As String

'        If TypeOf (pvalor) Is IBANDN Then
'            cuentaValidar = CType(pValor, IBANDN)
'        Else
'            Throw New ApplicationExceptionDN("El objeto no es del tipo IBANDN")
'        End If

'        If cuentaValidar Is Nothing Then
'            mensaje = "Objeto no asignado"
'            Return False
'        End If

'        codIBAN = cuentaValidar.Codigo

'        If String.IsNullOrEmpty(codIBAN) OrElse codIBAN.Length > 34 Then
'            mensaje = "El código IBAN no puede ser nulo, ni puede tener más de 34 caracteres alfanuméricos"
'            Return False
'        End If

'        digitosPais = ConvertirCadena(codIBAN.Substring(0, 2))
'        If digitosPais.Length < 4 Then
'            mensaje = "El código del país no es correcto"
'            Return False
'        End If

'        digitosControl = codIBAN.Substring(2, 2)
'        If Not IsNumeric(digitosControl) Then
'            mensaje = "Los dígitos de control no son correctos"
'            Return False
'        End If

'        digitosCuenta = ConvertirCadena(codIBAN.Substring(4, codIBAN.Length - 4))

'        codIBAN = digitosCuenta & digitosPais & digitosControl

'        If CalcularResultadoIBAN(codIBAN) <> 1 Then
'            mensaje = "El código IBAN no es correcto"
'            Return False
'        End If

'        Return True

'    End Function

'    Private Function ConvertirCadena(ByVal cadena As String) As String
'        Dim resultado As String = cadena

'        resultado = resultado.Replace("A", 10)
'        resultado = resultado.Replace("B", 11)
'        resultado = resultado.Replace("C", 12)
'        resultado = resultado.Replace("D", 13)
'        resultado = resultado.Replace("E", 14)
'        resultado = resultado.Replace("F", 15)

'        resultado = resultado.Replace("G", 16)
'        resultado = resultado.Replace("H", 17)
'        resultado = resultado.Replace("I", 18)
'        resultado = resultado.Replace("J", 19)
'        resultado = resultado.Replace("K", 20)
'        resultado = resultado.Replace("L", 21)

'        resultado = resultado.Replace("M", 22)
'        resultado = resultado.Replace("N", 23)
'        resultado = resultado.Replace("O", 24)
'        resultado = resultado.Replace("P", 25)
'        resultado = resultado.Replace("Q", 26)
'        resultado = resultado.Replace("R", 27)

'        resultado = resultado.Replace("S", 28)
'        resultado = resultado.Replace("T", 29)
'        resultado = resultado.Replace("U", 30)
'        resultado = resultado.Replace("V", 31)
'        resultado = resultado.Replace("W", 32)
'        resultado = resultado.Replace("X", 33)

'        resultado = resultado.Replace("Y", 34)
'        resultado = resultado.Replace("Z", 35)

'        Return resultado

'    End Function

'    Private Function CalcularResultadoIBAN(ByVal cadena As String) As Integer
'        Dim resultado As Integer
'        Dim cadenaAux As String = ""
'        Dim cadenaRestante As String = cadena
'        Dim restoStr As String = ""

'        If Not IsNumeric(cadena) Then
'            Return -1
'        End If

'        Do
'            cadenaAux = restoStr & cadenaRestante

'            If cadenaAux.Length > 9 Then
'                cadenaRestante = cadenaAux.Substring(9)
'                cadenaAux = cadenaAux.Substring(0, 9)
'            Else
'                cadenaRestante = ""
'            End If

'            resultado = Integer.Parse(cadenaAux) Mod 97
'            restoStr = resultado.ToString()

'        Loop While (cadenaRestante <> "")

'        Return resultado

'    End Function

'End Class