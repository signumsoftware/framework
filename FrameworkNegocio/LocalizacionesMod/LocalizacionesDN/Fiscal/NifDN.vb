#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

<Serializable()> Public Class NifDN
    Inherits EntidadDN
    Implements IIdentificacionFiscal
    Protected mCodigo As String

#Region "Constructores"
    Public Sub New()
        MyBase.New()
        Me.ModificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal pCodigo As String)
        MyBase.New()
        Dim mensaje As String

        If (ValCodigo(pCodigo, mensaje) = False) Then
            Throw New ApplicationException(mensaje)
        End If
        Me.CambiarValorVal(pCodigo, mCodigo)
        Me.ModificarEstado = EstadoDatosDN.SinModificar
    End Sub
#End Region

#Region "Propiedades"
    Public Property Codigo() As String Implements IIdentificacionFiscal.Codigo
        Get
            Return mCodigo
        End Get

        Set(ByVal Value As String)



            Dim mensaje As String

            If (ValCodigo(Value, mensaje) = False) Then
                Throw New ApplicationException(mensaje)
            End If

            Me.CambiarValorVal(Value, mCodigo)
        End Set
    End Property

    Public ReadOnly Property EstadoModificacion() As Framework.DatosNegocio.EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property
#End Region

#Region "Metodos Validacion"
    Public Function ValCodigo(ByVal pCodigo As String, ByRef pMensaje As String) As Boolean Implements IIdentificacionFiscal.ValCodigo
        Return ValidaNif(pCodigo, pMensaje)
    End Function

    Public Shared Function RecuperarNifAleatorio() As String
        Dim na As New Random

        Dim Codigo As String = na.Next(1, 99999999)
        Dim ausencias As Int16 = 8 - Codigo.Length
        For a As Integer = 1 To ausencias
            Codigo = "0" & Codigo
        Next



        Codigo += RecuperarLetra(Codigo & "a")
        Return Codigo
    End Function

    Public Shared Function RecuperarLetra(ByVal pCodigo As String) As String
        Dim mNif As String

        Dim mNumeros As String
        Dim pLetras As String
        Dim mCaracterInicio As String


        Try
            pLetras = "TRWAGMYFPDXBNJZSQVHLCKE"

            mNif = Trim(pCodigo).ToUpper

            If (mNif = String.Empty) Then
                Return Nothing

            Else
                mCaracterInicio = pCodigo.Substring(0, 1)
                'Comprobamos si es numérico para distinguir entre NIE y NIF
                If IsNumeric(mCaracterInicio) Then

                    'Si quisieramos admitir DNI's con longitud 8, sólo tenemos que descomentar lo comentado
                    If Len(mNif) = 9 Then 'Or Len(mNif) = 8 Then


                        mNumeros = Left(mNif, 8)

                        If IsNumeric(mNumeros) Then

                            Return Microsoft.VisualBasic.Mid(pLetras, 1 + (mNumeros Mod 23), 1)
                        Else
                            Throw New Framework.DatosNegocio.ApplicationExceptionDN("La parte numérica del DNI/NIF es incorrecta")
                        End If

                    Else
                        Throw New Framework.DatosNegocio.ApplicationExceptionDN("La parte numérica del DNI/NIF es incorrecta")


                    End If

                    Else
                    Throw New Framework.DatosNegocio.ApplicationExceptionDN("La longitud del DNI/NIF es incorrecta, debe ser 9 y es: " & Len(mNif))


                    End If

         

                End If



        Catch ex As Exception
            Throw ex
        End Try
    End Function

    Public Shared Function ValidaNif(ByVal pCodigo As String, ByRef pMensaje As String) As Boolean
        Dim mNif As String
        Dim mLetra As String
        Dim mNumeros As String
        Dim pLetras As String
        Dim mCaracterInicio As String


        Try
            pLetras = "TRWAGMYFPDXBNJZSQVHLCKE"

            mNif = Trim(pCodigo).ToUpper

            If (mNif = String.Empty) Then
                Return True

            Else
                mCaracterInicio = pCodigo.Substring(0, 1)
                'Comprobamos si es numérico para distinguir entre NIE y NIF
                If IsNumeric(mCaracterInicio) Then

                    'Si quisieramos admitir DNI's con longitud 8, sólo tenemos que descomentar lo comentado
                    If Len(mNif) = 9 Then 'Or Len(mNif) = 8 Then

                        mLetra = Right(mNif, 1)
                        mNumeros = Left(mNif, 8)

                        If IsNumeric(mNumeros) Then
                            If mLetra = Microsoft.VisualBasic.Mid(pLetras, 1 + (mNumeros Mod 23), 1) Then
                                Return True

                            Else
                                pMensaje = "La letra del DNI/NIF no cumple con la regla de validación, la letra debería ser: " & Microsoft.VisualBasic.Mid(pLetras, 1 + (mNumeros Mod 23), 1)

                                Return False
                            End If

                        Else
                            pMensaje = "La parte numérica del DNI/NIF es incorrecta"

                            Return False
                        End If

                    Else
                        pMensaje = "La longitud del DNI/NIF es incorrecta, debe ser 9 y es: " & Len(mNif)

                        Return False
                    End If

                Else
                    'Al no ser numerico el primer caracter, se entiende que es un NIE y devolvemos TRUE
                    If mCaracterInicio = "X" Then
                        Return True
                    Else
                        Return False
                    End If


                End If

            End If

        Catch ex As Exception
            Throw ex
        End Try
    End Function
#End Region


    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Me.mCodigo Is Nothing OrElse Me.mCodigo = "" Then
            pMensaje = "es necesarioy un codigo para el NifDN"
            Return EstadoIntegridadDN.Inconsistente
        Else
            If Not Me.ValCodigo(Me.mCodigo, pMensaje) Then
                Return EstadoIntegridadDN.Inconsistente

            End If

        End If
        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

End Class

<Serializable()> Public Class ValidadorNIF
    Implements IValidador

    Public Function Formula() As String Implements Framework.DatosNegocio.IValidador.Formula
    End Function

    Public Function Validacion(ByRef mensaje As String, ByVal pValor As Object) As Boolean Implements Framework.DatosNegocio.IValidador.Validacion
        Return NifDN.ValidaNif(pValor, "")
    End Function
End Class

<Serializable()> Public Class CifDN
    Inherits EntidadDN
    Implements IIdentificacionFiscal

    Protected mCodigo As String

#Region "Constructores"
    Public Sub New()
    End Sub

    Public Sub New(ByVal pCodigo As String)
        Dim mensaje As String

        If (ValCodigo(pCodigo, mensaje) = False) Then
            Throw New ApplicationException(mensaje)
        End If

        mCodigo = pCodigo.ToUpper
    End Sub
#End Region

#Region "Propiedades"
    Public Property Codigo() As String Implements IIdentificacionFiscal.Codigo
        Get
            Return Me.mCodigo
        End Get
        Set(ByVal Value As String)
            Dim mensaje As String

            If (ValCodigo(Value, mensaje) = False) Then
                Throw New ApplicationException(mensaje)
            End If
            Me.CambiarValorVal(Of String)(Value.ToUpper, mCodigo)

        End Set
    End Property
#End Region

#Region "Metodos Validacion"
    Public Function ValCodigo(ByVal pCodigo As String, ByRef pMensaje As String) As Boolean Implements IIdentificacionFiscal.ValCodigo
        Return ValidaCif(pCodigo, pMensaje)
    End Function

    Public Shared Function ValidaCif(ByVal pCodigo As String, ByRef pMensaje As String) As Boolean
        Dim LetrasI As String
        Dim LetrasJ As String
        Dim TipoSociedad As String
        Dim DigitoControlNumero As Integer
        Dim DigitoControlLetra As String
        Dim Numeros As String
        Dim mCif As String
        Dim SumaPares As Integer
        Dim Impar1 As Integer
        Dim Impar2 As Integer
        Dim Impar3 As Integer
        Dim Impar4 As Integer
        Dim Impar1tmp As Integer
        Dim Impar2tmp As Integer
        Dim Impar3tmp As Integer
        Dim Impar4tmp As Integer
        Dim SumaImpares As Integer
        Dim SumaTotal As Integer
        Dim CompDigitoControl As Integer
        LetrasI = "ABCDEFGHI"
        LetrasJ = "ABCDEFGHJ"

        mCif = Trim(pCodigo).ToUpper

        If Len(mCif) = 9 Then

            TipoSociedad = Left(mCif, 1)

            If Not IsNumeric(TipoSociedad) Then

                If IsNumeric(Right(mCif, 1)) Then
                    DigitoControlNumero = CInt(Right(mCif, 1))
                Else
                    DigitoControlLetra = Right(mCif, 1)
                End If

                Numeros = Microsoft.VisualBasic.Mid(mCif, 2, 7)
                If Not IsNumeric(Numeros) Then
                    pMensaje = "letras en la franja numerica"
                    Return False
                End If
                SumaPares = CInt(Microsoft.VisualBasic.Mid(Numeros, 2, 1)) + CInt(Microsoft.VisualBasic.Mid(Numeros, 4, 1)) + CInt(Microsoft.VisualBasic.Mid(Numeros, 6, 1))

                Impar1tmp = CInt(Microsoft.VisualBasic.Mid(Numeros, 1, 1)) * 2
                If Len(Impar1tmp.ToString) = 2 Then
                    Impar1 = CInt(Left(Impar1tmp.ToString, 1)) + CInt(Right(Impar1tmp.ToString, 1))
                Else
                    Impar1 = Impar1tmp
                End If

                Impar2tmp = CInt(Microsoft.VisualBasic.Mid(Numeros, 3, 1)) * 2
                If Len(Impar2tmp.ToString) = 2 Then
                    Impar2 = CInt(Left(Impar2tmp.ToString, 1)) + CInt(Right(Impar2tmp.ToString, 1))
                Else
                    Impar2 = Impar2tmp
                End If

                Impar3tmp = CInt(Microsoft.VisualBasic.Mid(Numeros, 5, 1)) * 2
                If Len(Impar3tmp.ToString) = 2 Then
                    Impar3 = CInt(Left(Impar3tmp.ToString, 1)) + CInt(Right(Impar3tmp.ToString, 1))
                Else
                    Impar3 = Impar3tmp
                End If

                Impar4tmp = CInt(Microsoft.VisualBasic.Mid(Numeros, 7, 1)) * 2
                If Len(Impar4tmp.ToString) = 2 Then
                    Impar4 = CInt(Left(Impar4tmp.ToString, 1)) + CInt(Right(Impar4tmp.ToString, 1))
                Else
                    Impar4 = Impar4tmp
                End If

                SumaImpares = Impar1 + Impar2 + Impar3 + Impar4

                SumaTotal = SumaPares + SumaImpares

                CompDigitoControl = 10 - CInt(Right(SumaTotal.ToString, 1))

                If DigitoControlNumero = CompDigitoControl Or DigitoControlLetra = Microsoft.VisualBasic.Mid(LetrasJ, CompDigitoControl, 1) Or DigitoControlLetra = Microsoft.VisualBasic.Mid(LetrasI, CompDigitoControl, 1) Then
                    Return True
                Else
                    pMensaje = "El Digito de Control del CIF introducido no se corresponde con el resultado de su función de validación"
                    Return False
                End If

            Else
                pMensaje = "El caracter que nos indica el tipo de sociedad no puede ser un número"
                Return False
            End If
        Else
            pMensaje = "Longitud incorrecta de CIF. La longitud debe ser 9 y es: " & Len(mCif).ToString
            Return False
        End If
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Me.mCodigo Is Nothing OrElse Me.mCodigo = "" Then
            pMensaje = "es necesarioy un codigo para el CifDN"
            Return EstadoIntegridadDN.Inconsistente
        Else
            If Not Me.ValCodigo(Me.mCodigo, pMensaje) Then
                Return EstadoIntegridadDN.Inconsistente

            End If

        End If
        Return MyBase.EstadoIntegridad(pMensaje)
    End Function
#End Region

End Class

<Serializable()> Public Class ValidadorCIF
    Implements IValidador


    Public Function Formula() As String Implements Framework.DatosNegocio.IValidador.Formula
        MsgBox("No implmentado")
    End Function

    Public Function Validacion(ByRef mensaje As String, ByVal pValor As Object) As Boolean Implements Framework.DatosNegocio.IValidador.Validacion
        Return CifDN.ValidaCif(pValor, mensaje)
    End Function
End Class
