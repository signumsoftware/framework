Imports System.IO

'de momoento lo comentamos porque los sustituye el de Auxiu.Formateadores
'
'Public Class ConvertidorTextoNum

'    Private Shared Pointer1 As Integer = 0
'    Private Shared Pointer2 As Integer = 0

'#Region "Métodos que convierten el texto a número"

'    Private Shared Function ObtenerUnidades(ByVal unidades As Integer) As String
'        Select Case Val(unidades)
'            Case 1 : ObtenerUnidades = "Un"
'            Case 2 : ObtenerUnidades = "Dos"
'            Case 3 : ObtenerUnidades = "Tres"
'            Case 4 : ObtenerUnidades = "Cuatro"
'            Case 5 : ObtenerUnidades = "Cinco"
'            Case 6 : ObtenerUnidades = "Séis"
'            Case 7 : ObtenerUnidades = "Siete"
'            Case 8 : ObtenerUnidades = "Ocho"
'            Case 9 : ObtenerUnidades = "Nueve"
'            Case Else : ObtenerUnidades = ""
'        End Select
'    End Function

'    Private Shared Function ObtenerDecenas(ByVal decenas As Integer) As String
'        Dim Result As String

'        Result = ""           'anula el valor temporal de la funcion
'        If Val(Microsoft.VisualBasic.Left(decenas, 1)) = 1 Then   ' si el valor esta entre 10-19
'            Select Case Val(decenas)
'                Case 10 : Result = "Diez"
'                Case 11 : Result = "Once"
'                Case 12 : Result = "Doce"
'                Case 13 : Result = "Trece"
'                Case 14 : Result = "Catorce"
'                Case 15 : Result = "Quince"
'                Case 16 : Result = "Dieciseis"
'                Case 17 : Result = "Diecisiete"
'                Case 18 : Result = "Dieciocho"
'                Case 19 : Result = "Diecinueve"
'                Case Else
'            End Select
'        Else    ' Si el valor esta entre 20-99
'            Select Case Val(Microsoft.VisualBasic.Left(decenas, 1))
'                Case 2 : Result = "Veinte "
'                Case 3 : Result = "Treinta "
'                Case 4 : Result = "Cuarenta "
'                Case 5 : Result = "Cincuenta "
'                Case 6 : Result = "Sesenta "
'                Case 7 : Result = "Setenta "
'                Case 8 : Result = "Ochenta "
'                Case 9 : Result = "Noventa "
'                Case Else
'            End Select

'            Result = Result & ObtenerUnidades(Microsoft.VisualBasic.Right(decenas, 1))
'        End If

'        Return Result
'    End Function

'    Private Shared Function ObtenerCentenas(ByVal centenas As String) As String
'        Dim resultado As String
'        Dim quinien As String
'        Dim quinien2 As String

'        If Val(centenas) = 0 Then Exit Function

'        centenas = Microsoft.VisualBasic.Right("000" & centenas, 3)

'        'Convierte el lugar de las centenas
'        quinien2 = " Cientos "
'        If Mid(centenas, 1, 1) <> "0" Then
'            quinien = ObtenerUnidades(Mid(centenas, 1, 1))
'            If quinien = "Cinco" Then
'                quinien = "Quinientos "
'                quinien2 = ""
'            End If
'            If quinien = "Un" Then
'                quinien = ""
'                quinien2 = "Ciento "
'            End If
'            If quinien = "Nueve" Then
'                quinien = "Nove"
'                quinien2 = "cientos "
'            End If
'            If quinien = "Siete" Then
'                quinien = "Sete"
'                quinien2 = "cientos "
'            End If
'            resultado = quinien & quinien2 ' aca le agrega al numero la palabra
'        End If

'        'Convierte el lugar de los miles
'        If Mid(centenas, 2, 1) <> "0" Then
'            quinien = ObtenerDecenas(Mid(centenas, 2))
'            If Pointer1 = 0 Then
'                Pointer1 = 1
'                quinien = Replace(quinien, " ", " y ")
'            End If

'            resultado = resultado & quinien
'        Else
'            quinien = ObtenerUnidades(Mid(centenas, 3))
'            resultado = resultado & quinien
'        End If

'        Return resultado
'    End Function

'    Public Shared Function RecuperarTextoNumero(ByVal numero As String) As String
'        Dim otroNumero As String = numero
'        Dim euros, centimos, temp As String
'        Dim decimalPlace, count As Integer

'        Dim Place(9) As String
'        Place(2) = " Mil "
'        Place(3) = " Millones "
'        Place(4) = " Billones "
'        Place(5) = " Trillones "

'        ' String representa la cantidad
'        numero = Trim(Str(numero))

'        ' el lugar de la posicion decimal ) si ninguno
'        decimalPlace = InStr(numero, ".")

'        'Convierte Centavos and set MyNumber a la cantidad en dolares
'        If decimalPlace > 0 Then
'            otroNumero = Microsoft.VisualBasic.Left(numero, decimalPlace - 1)
'            centimos = ObtenerDecenas(Microsoft.VisualBasic.Left(Mid(numero, decimalPlace + 1) & "00", 2))
'            numero = Trim(Microsoft.VisualBasic.Left(numero, decimalPlace - 1))
'        Else
'            otroNumero = numero
'        End If

'        Dim enta, hacer As String
'        Dim BuscaEspacio As Integer
'        enta = CStr(numero)
'        hacer = ""
'        If enta.Length = 2 And (Microsoft.VisualBasic.Right(enta, 1) = "0") Then
'            hacer = "cero"
'        End If

'        count = 1
'        Do While numero <> ""
'            temp = ObtenerCentenas(Microsoft.VisualBasic.Right(numero, 3))
'            If temp <> "" Then euros = temp & Place(count) & euros
'            If Len(numero) > 3 Then
'                If Len(otroNumero) = 4 And Microsoft.VisualBasic.Left(otroNumero, 1) = "1" Then
'                    Pointer2 = 1
'                End If
'                numero = Microsoft.VisualBasic.Left(numero, Len(numero) - 3)
'            Else
'                numero = ""
'            End If
'            count = count + 1
'        Loop

'        If Pointer2 = 1 Then
'            euros = Microsoft.VisualBasic.Right(euros, Len(euros) - 3)
'        End If

'        If hacer = "cero" Then
'            BuscaEspacio = InStr(euros, " ")
'            euros = Microsoft.VisualBasic.Left(euros, BuscaEspacio - 1)
'        End If


'        Select Case euros
'            Case ""
'                euros = "Ningún Euro"
'            Case "One"
'                euros = "Un Euro"
'            Case Else
'                euros = euros & " Euros"
'        End Select


'        centimos = Replace(centimos, " ", " y ")
'        Select Case centimos
'            Case ""
'                centimos = " y Ningún Centimo"
'            Case Else
'                centimos = " con " & centimos & " Céntimos"
'        End Select

'        Return euros.ToLower() & centimos.ToLower()

'    End Function

'#End Region

'End Class

