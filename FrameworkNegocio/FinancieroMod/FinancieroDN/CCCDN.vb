Imports Framework.DatosNegocio

<Serializable()> _
Public Class CCCDN
    Inherits EntidadDN
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
            CambiarValorVal(Of String)(value, mCodigo)
        End Set
    End Property

    Public ReadOnly Property CodigoEntidadBancaria() As String
        Get
            If Not String.IsNullOrEmpty(mCodigo) AndAlso mCodigo.Length = 20 Then
                Return mCodigo.Substring(0, 4)
            Else
                Return String.Empty
            End If
        End Get
    End Property

    Public ReadOnly Property CodigoOficina() As String
        Get
            If Not String.IsNullOrEmpty(mCodigo) AndAlso mCodigo.Length = 20 Then
                Return mCodigo.Substring(4, 4)
            Else
                Return String.Empty
            End If
        End Get
    End Property

    Public ReadOnly Property CodigoDigitosControl() As String
        Get
            If Not String.IsNullOrEmpty(mCodigo) AndAlso mCodigo.Length = 20 Then
                Return mCodigo.Substring(8, 2)
            Else
                Return String.Empty
            End If
        End Get
    End Property

    Public ReadOnly Property CodigoCuenta() As String
        Get
            If Not String.IsNullOrEmpty(mCodigo) AndAlso mCodigo.Length = 20 Then
                Return mCodigo.Substring(10)
            Else
                Return String.Empty
            End If
        End Get
    End Property

#End Region

#Region "Métodos IValidador"

    Public Function Validacion(ByRef mensaje As String, ByVal pValor As Object) As Boolean Implements Framework.DatosNegocio.IValidador.Validacion
        Dim bancoOficina As String
        Dim control As String
        Dim cuenta As String
        Dim codCuenta As String
        Dim cuentaValidar As CCCDN

        If TypeOf (pValor) Is CCCDN Then
            cuentaValidar = CType(pValor, CCCDN)
        Else
            Throw New ApplicationExceptionDN("El objeto no es del tipo CCCDN")
        End If

        If cuentaValidar Is Nothing Then
            mensaje = "Objeto no asignado"
            Return False
        End If

        codCuenta = cuentaValidar.Codigo


        If String.IsNullOrEmpty(codCuenta) Then
            Return True
        End If

        If codCuenta.Length <> 20 OrElse Not IsNumeric(codCuenta) Then
            mensaje = "El código cuenta cliente  debe estar compuesto de 20 dígitos numéricos"
            Return False
        End If

        bancoOficina = codCuenta.Substring(0, 8)
        control = codCuenta.Substring(8, 2)
        cuenta = codCuenta.Substring(10)

        If CalcularDigitoControl(bancoOficina) <> Integer.Parse(control(0)) Then
            mensaje = "El código para el banco y la oficina no coincide con el dígito de control"
            Return False
        End If

        If CalcularDigitoControl(cuenta) <> Integer.Parse(control(1)) Then
            mensaje = "El código para cuenta no coincide con el dígito de control"
            Return False
        End If

        Return True

    End Function

    Private Function CalcularDigitoControl(ByVal cadena As String) As Integer
        Dim resultadoPesos As Integer
        Dim digito As Integer
        Dim arrayPesos As New ArrayList()

        If String.IsNullOrEmpty(cadena) OrElse cadena.Length < 8 OrElse Not IsNumeric(cadena) Then
            Return -1
        End If

        arrayPesos.Add(6)
        arrayPesos.Add(3)
        arrayPesos.Add(7)
        arrayPesos.Add(9)
        arrayPesos.Add(10)
        arrayPesos.Add(5)
        arrayPesos.Add(8)
        arrayPesos.Add(4)
        arrayPesos.Add(2)
        arrayPesos.Add(1)

        For i As Integer = 0 To cadena.Length - 1
            resultadoPesos = resultadoPesos + arrayPesos(i) * Integer.Parse(cadena((cadena.Length - 1) - i))
        Next

        resultadoPesos = resultadoPesos Mod 11

        digito = 11 - resultadoPesos

        If digito = 10 Then digito = 1
        If digito = 11 Then digito = 0

        Return digito

    End Function

    Public Function Formula() As String Implements Framework.DatosNegocio.IValidador.Formula
        Throw New NotImplementedException("El método Formula no está implementado")
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not Validacion(pMensaje, Me) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Overrides Function ToString() As String
        Dim codBanco As String = CodigoEntidadBancaria
        Dim codOficina As String = CodigoOficina
        Dim codDC As String = CodigoDigitosControl
        Dim codCuenta As String = CodigoCuenta

        If String.IsNullOrEmpty(codBanco) OrElse String.IsNullOrEmpty(codDC) OrElse String.IsNullOrEmpty(codCuenta) OrElse String.IsNullOrEmpty(codOficina) Then
            Return String.Empty
        End If

        Return codBanco & "-" & codOficina & "-" & codDC & "-" & codCuenta

    End Function

#End Region

End Class

'<Serializable()> _
'Public Class ValidadorCCCDN
'    Implements IValidador

'    Public Function Formula() As String Implements Framework.DatosNegocio.IValidador.Formula
'        Throw New NotImplementedException("Método no implementado")
'    End Function

'    Public Function Validacion(ByRef mensaje As String, ByVal pValor As Object) As Boolean Implements Framework.DatosNegocio.IValidador.Validacion
'        Dim bancoOficina As String
'        Dim control As String
'        Dim cuenta As String
'        Dim codCuenta As String
'        Dim cuentaValidar As CCCDN

'        If TypeOf (pValor) Is CCCDN Then
'            cuentaValidar = CType(pValor, CCCDN)
'        Else
'            Throw New ApplicationExceptionDN("El objeto no es del tipo CCCDN")
'        End If

'        If cuentaValidar Is Nothing Then
'            mensaje = "Objeto no asignado"
'            Return False
'        End If

'        codCuenta = cuentaValidar.Codigo

'        If String.IsNullOrEmpty(codCuenta) OrElse codCuenta.Length <> 20 OrElse Not IsNumeric(codCuenta) Then
'            mensaje = "El código cuenta cliente no puede ser nulo, y debe estar compuesto de 20 dígitos numéricos"
'            Return False
'        End If

'        bancoOficina = codCuenta.Substring(0, 8)
'        control = codCuenta.Substring(8, 2)
'        cuenta = codCuenta.Substring(10)

'        If CalcularDigitoControl(bancoOficina) <> Integer.Parse(control(0)) Then
'            mensaje = "El código para el banco y la oficina no coincide con el dígito de control"
'            Return False
'        End If

'        If CalcularDigitoControl(cuenta) <> Integer.Parse(control(1)) Then
'            mensaje = "El código para cuenta no coincide con el dígito de control"
'            Return False
'        End If

'        Return True

'    End Function

'    Private Function CalcularDigitoControl(ByVal cadena As String) As Integer
'        Dim resultadoPesos As Integer
'        Dim digito As Integer
'        Dim arrayPesos As New ArrayList()

'        If String.IsNullOrEmpty(cadena) OrElse cadena.Length < 8 OrElse Not IsNumeric(cadena) Then
'            Return -1
'        End If

'        arrayPesos.Add(6)
'        arrayPesos.Add(3)
'        arrayPesos.Add(7)
'        arrayPesos.Add(9)
'        arrayPesos.Add(10)
'        arrayPesos.Add(5)
'        arrayPesos.Add(8)
'        arrayPesos.Add(4)
'        arrayPesos.Add(2)
'        arrayPesos.Add(1)

'        For i As Integer = 0 To cadena.Length - 1
'            resultadoPesos = resultadoPesos + arrayPesos(i) * Integer.Parse(cadena((cadena.Length - 1) - i))
'        Next

'        resultadoPesos = resultadoPesos Mod 11

'        digito = 11 - resultadoPesos

'        If digito = 10 Then digito = 1
'        If digito = 11 Then digito = 0

'        Return digito

'    End Function

'End Class
