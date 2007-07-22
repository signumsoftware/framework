Public Interface IFormateador
    Function Formatear(ByVal cadena As String) As String
End Interface

''' <summary>
''' Formatea una cadena con este aspecto: "999.999.999,00 €"
''' el número de decimales es paramterizable
''' </summary>
Public Class FormateadorMonedaEurosConSimbolo
    Inherits FormateadorMoneda

    ''' <summary>
    ''' establece por defecto el nº de decimales a 2
    ''' </summary>
    Public Sub New()
        MyBase.New(2)
    End Sub

    Public Sub New(ByVal pNumeroDecimales As Integer)
        MyBase.New(pNumeroDecimales)
    End Sub

    Public Overrides Function Formatear(ByVal cadena As String) As String
        Return MyBase.Formatear(cadena) & " €"
    End Function

    Public Shared Function FormatearRapido(ByVal cadena As String) As String
        Dim miformateador As New FormateadorMonedaEurosConSimbolo
        Return miformateador.Formatear(cadena)
    End Function

End Class

''' <summary>
'''formate una cadena str con este aspecto: "999.999.999.999,00"
'''el número de decimales es parametrizable (0 a n)
''' </summary>
''' <remarks></remarks>
Public Class FormateadorMoneda

    Implements IFormateador

#Region "campos"
    Protected mNumeroDecimales As Int32
    Protected mEstilo As String
#End Region

#Region "constructor"
    Public Sub New()
        'si hacemos el constructor vacío, entendemos q nº decimales es 0
        NumeroDecimales = 0
    End Sub

    Public Sub New(ByVal pNumeroDecimales As Int32)
        'establecemos el nº decimales q nos pasan
        NumeroDecimales = pNumeroDecimales

    End Sub
#End Region

#Region "propiedades"
    Public Property NumeroDecimales() As Int32
        Get
            Return mNumeroDecimales
        End Get
        Set(ByVal Value As Int32)
            'establecemos el nº decimales q hay
            mNumeroDecimales = Value
            'según los decimales q haya, establecemos el formato q tendrá
            mEstilo = "N"
            'si hay decimales, agregamos el nº de posiciones decimales establecidas
            mEstilo += Convert.ToString(Value)
        End Set
    End Property
#End Region

#Region "métodos"
    Public Overridable Function Formatear(ByVal cadena As String) As String Implements IFormateador.Formatear
        Try
            If Not String.IsNullOrEmpty(cadena) Then
                'si es numérico
                If IsNumeric(cadena) Then
                    'devolvemos el formato en función de cómo se haya construido (según las propiedades)
                    Return String.Format("{0:" & mEstilo & "}", Double.Parse(cadena))
                Else
                    'si no es numérico, lanzamos excepción
                    Throw New ApplicationException("La cadena que se intenta formatear no es numérica")
                End If
            Else
                'si no hay nada, devolvemos 0 formateado
                Return Me.Formatear(CStr(0))
            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Function
#End Region

End Class


Public Class ConvertirEurosAPesetas
    'devuelve una cantidad en euros convertida a pesetas y formateada por monedas
    Implements IFormateador

#Region "Constructor"
    Public Sub New()

    End Sub
#End Region

#Region "Métodos"
    Public Function Fotmatear(ByVal cadena As String) As String Implements IFormateador.Formatear
        Dim formateadormonedas As FormateadorMoneda

        Try
            If Not IsNumeric(cadena) Then
                Throw New ApplicationException("La cadena que se intenta convertir no es un número")
            End If

            formateadormonedas = New FormateadorMoneda
            formateadormonedas.NumeroDecimales = 0 'como son pesetas, lo ponemos sin decimales

            'para evitar que de un error si hay -1
            If cadena >= 0 Then
                Return formateadormonedas.Formatear((Double.Parse("0" & cadena) * 166.386)) & " Pts"
            Else
                Return formateadormonedas.Formatear((Double.Parse(cadena) * 166.386)) & " Pts"
            End If


        Catch ex As Exception
            Throw ex
        End Try
    End Function
#End Region
End Class




Public Class ConversorTextoANumero

    Public Shared Function ConvertirNumeroATexto(ByVal pNumero As Double) As String

        Dim salida As String = String.Empty

        'convertimos en texto
        Dim numt As String = String.Format("{0:N}", pNumero)

        'separamos los decimales de los enteros
        Dim separador As String() = numt.Split(",")

        'el primer elemento del separador son los enteros,
        'obtenemos un array con los enteros separados en millares
        Dim enteros As String() = separador(0).Split(".")


        'definimos los nombres de las unidades
        Dim contunidadesenteros As String() = ConversorTextoANumero.DefinirUnidades(enteros)

        'convertimos en texto los enteros
        ConvertirElementos(enteros, contunidadesenteros, salida)

        'convertimos en texto los decimales, si los hay
        If separador.GetLength(0) <> 1 Then
            salida += "con "

            Dim salidadecimales As String = String.Empty

            Dim decimales As String() = separador(1).Split(".")

            'definimos los nombres de las unidades
            Dim contunidadesdecimales As String() = ConversorTextoANumero.DefinirUnidades(decimales)

            'convertimos en texto los decimales
            ConvertirElementos(decimales, contunidadesdecimales, salidadecimales)

            'como son decimales, si no hay nada devuelvo "cero"
            If salidadecimales.Trim = String.Empty Then
                salidadecimales = " " & Unidades(0)
            End If

            salida += salidadecimales
        End If

        Excepciones(salida)

        Return salida.Trim
    End Function

    Private Shared Sub Excepciones(ByRef salida As String)
        'ponemos bien las unidades de millar simples
        salida = salida.Replace("un mil ", "mil ")
        'corregimos las unidades de millón simples si no llevan 'y'
        If Not salida.Contains(" y un millones ") Then
            salida = salida.Replace(" un millones ", " un millón ")
        End If
        If Not salida.Contains(" y un billones ") Then
            salida = salida.Replace(" un billones ", " un billón ")
        End If
        'quitamos espacios repetidos
        salida = salida.Replace("  ", " ")
    End Sub

    Private Shared Function DefinirUnidades(ByVal enteros As String()) As String()
        'en función de la cantidad de elementos que haya en enteros tendremos los
        'billones, millones, unidades...
        Dim numunidades As Integer = enteros.GetLength(0) 'CInt(enteros.GetLength(0) / 3)
        'If enteros.GetLength(0) > 1 AndAlso enteros.GetLength(0) Mod 3 <> 0 Then
        '    numunidades += 1
        'End If

        Dim contunidades As String()
        ReDim contunidades(numunidades - 1)
        For i As Integer = numunidades - 1 To 0 Step -1
            Select Case i
                Case Is = 4
                    contunidades(i) = "billones"
                Case Is = 3
                    contunidades(i) = "mil"
                Case Is = 2
                    contunidades(i) = "millones"
                Case Is = 1
                    contunidades(i) = "mil"
                Case Else
                    contunidades(i) = String.Empty
            End Select
        Next

        'hacemos un array nuevo para poder ordenarlo bien
        '(va ordenado al revés)
        Dim ordenado As String()
        ReDim ordenado(contunidades.GetLength(0) - 1)

        Dim pos As Integer = contunidades.GetLength(0) - 1
        For a As Integer = 0 To contunidades.GetLength(0) - 1
            ordenado(a) = contunidades(pos)
            pos -= 1
        Next

        Return ordenado

    End Function

    Private Shared Sub ConvertirElementos(ByVal elementos As String(), ByVal contunidades As String(), ByRef salida As String)
        For a As Integer = 0 To elementos.GetLength(0) - 1
            'cojemos el primer grupo de 3
            Dim trio As String = elementos(a)

            'nos aseguramos de que sean 3 elementos en cada trío
            Do Until trio.Length = 3
                trio = String.Concat("0", trio)
            Loop

            Dim centena As Integer = CInt(trio.Substring(0, 1))
            Dim decena As Integer = CInt(trio.Substring(1, 1))
            Dim unidad As Integer = CInt(trio.Substring(2, 1))

            salida += GestorCentenasDecenasUnidades(centena, decena, unidad) & " "


            ''ahora cojemos cada elemento dentro del trio y lo convertimos a texto
            'For b As Integer = 0 To trio.Length - 1
            '    Dim numact As Integer = CInt(trio.Substring(b, 1))
            '    Select Case b
            '        Case Is = 0
            '            'salida += ConversorTextoANumero.Centenas(numact) & " "
            '            centena = numact
            '        Case Is = 1
            '            'salida += ConversorTextoANumero.Decenas(numact) & " "

            '            decena = numact
            '        Case Is = 2
            '            'If salida.Trim <> String.Empty Then
            '            '    salida += "y "
            '            'End If
            '            'salida += ConversorTextoANumero.Unidades(numact) & " "

            '            unidad = numact
            '            salida += GestorDecenasUnidades(decena, unidad) & " "
            '            unidad = 0
            '            decena = 0
            '    End Select
            'Next

            'ahora ponemos la cifra de las unidades que corresponda si hay alguna

            If centena <> 0 OrElse decena <> 0 OrElse unidad <> 0 Then
                If contunidades.GetLength(0) - 1 >= a Then
                    salida += contunidades(a) & " "
                End If
            End If

        Next
    End Sub

    Private Shared Function GestorCentenasDecenasUnidades(ByVal pCentena, ByVal pDecena, ByVal pUnidad) As String
        Dim salida As String = GestorDecenasUnidades(pDecena, pUnidad)

        If pCentena = 1 AndAlso (pCentena <> 0 OrElse pUnidad <> 0) Then
            salida = "ciento " & salida
        Else
            salida = Centenas(pCentena) & " " & salida
        End If

        Return salida

    End Function

    Private Shared Function GestorDecenasUnidades(ByVal pDecena As Integer, ByVal pUnidad As Integer) As String
        Dim salida As String = String.Empty

        Select Case pDecena
            Case Is = 1
                'se trata de once, doce...
                Select Case pUnidad
                    Case Is = 0
                        salida = Decenas(pDecena)
                    Case Is = 1
                        salida = "once"
                    Case Is = 2
                        salida = "doce"
                    Case Is = 3
                        salida = "trece"
                    Case Is = 4
                        salida = "catorce"
                    Case Is = 5
                        salida = "quince"
                    Case Is = 6
                        salida = "dieciséis"
                    Case Else
                        salida = String.Concat("dieci", Unidades(pUnidad))
                End Select
            Case Is = 2
                Select Case pUnidad
                    Case Is = 0
                        salida = Decenas(pDecena)
                    Case Is = 1
                        salida = "veintiuno"
                    Case Else
                        salida = String.Concat("veinti", Unidades(pUnidad))
                End Select
            Case Else
                'devolvemos lo normal
                If pUnidad = 0 Then
                    salida = Decenas(pDecena)
                Else
                    salida = Decenas(pDecena)

                    Dim sep As String = String.Empty
                    If salida.Trim <> String.Empty Then
                        sep = " y "
                    Else
                        sep = " "
                    End If
                    salida += sep & Unidades(pUnidad)
                End If
        End Select

        Return salida
    End Function

    Private Shared Function Unidades(ByVal pNumero As Integer) As String
        Select Case pNumero
            Case Is = 0
                Return "cero"
            Case Is = 1
                Return "un"
            Case Is = 2
                Return "dos"
            Case Is = 3
                Return "tres"
            Case Is = 4
                Return "cuatro"
            Case Is = 5
                Return "cinco"
            Case Is = 6
                Return "seis"
            Case Is = 7
                Return "siete"
            Case Is = 8
                Return "ocho"
            Case Is = 9
                Return "nueve"
        End Select
    End Function

    Private Shared Function Decenas(ByVal pNumero As Integer) As String
        Select Case pNumero
            Case Is = 0
                Return String.Empty
            Case Is = 1
                Return "diez"
            Case Is = 2
                Return "veinte"
            Case Is = 3
                Return "treinta"
            Case Is = 4
                Return "cuarenta"
            Case Is = 5
                Return "cincuenta"
            Case Is = 6
                Return "sesenta"
            Case Is = 7
                Return "setenta"
            Case Is = 8
                Return "ochenta"
            Case Is = 9
                Return "noventa"
        End Select
    End Function

    Private Shared Function Centenas(ByVal pNumero As Integer) As String
        Select Case pNumero
            Case Is = 0
                Return String.Empty
            Case Is = 1
                Return "cien"
            Case Is = 2
                Return "doscientos"
            Case Is = 3
                Return "trescientos"
            Case Is = 4
                Return "cuatrocientos"
            Case Is = 5
                Return "quinientos"
            Case Is = 6
                Return "seiscientos"
            Case Is = 7
                Return "setecientos"
            Case Is = 8
                Return "ochocientos"
            Case Is = 9
                Return "novecientos"
        End Select
    End Function

End Class



Public Class ConversorTamañoArchivos
    Private Shared colas As String() = {"Bytes", "KBytes", "MBytes", "GBytes", "TBytes", "PByte", "EByte", "ZByte", "YByte"}

    Public Shared Function ToComputerSize(ByVal value As Long)
        Dim valor As Double = CDbl(value)
        Dim i As Int32 = 0
        While valor >= 1024
            valor /= 1024.0
            i += 1
        End While
        Return valor.ToString("#,###.00") + " " + colas(i)
    End Function
End Class