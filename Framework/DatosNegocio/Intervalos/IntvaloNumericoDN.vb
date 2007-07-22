Imports Framework.DatosNegocio

<Serializable()> _
Public Class IntvaloNumericoDN
    Inherits EntidadDN
    Implements IIntvaloNumerico



    Protected mValInf As Double
    Protected mValSup As Double


    Public Property ValInf() As Double Implements IIntvaloNumerico.ValInf
        Get
            Return Me.mValInf
        End Get
        Set(ByVal value As Double)
            Me.CambiarValorVal(Of Double)(value, mValInf)
        End Set
    End Property

    Public Property ValSup() As Double Implements IIntvaloNumerico.ValSup
        Get
            Return Me.mValSup
        End Get
        Set(ByVal value As Double)
            Me.CambiarValorVal(Of Double)(value, mValSup)

        End Set
    End Property


    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As EstadoIntegridadDN

        If Not Me.BienFormado Then
            pMensaje = "El valor inferiro " & mValInf & " no puede superar el valor superior " & mValSup
            Return EstadoIntegridadDN.Inconsistente
        End If


        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Function Contiene(ByVal pValor As Double) As Boolean Implements IIntvaloNumerico.Contiene
        Return pValor >= Me.mValInf AndAlso pValor <= Me.mValSup
    End Function


    Public ReadOnly Property Amplitud() As Double
        Get
            Return Me.mValSup - Me.mValInf
        End Get

    End Property

    Public Function SolapadoOContenido(ByVal pIntervalo As IIntvaloNumerico) As IntSolapadosOContenido Implements IIntvaloNumerico.SolapadoOContenido


        If Me.BienFormado AndAlso pIntervalo.BienFormado Then
            Throw New ApplicationExceptionDN("alguno de los intervalos no esta bien formado")
        End If

        If Me.ValInf = pIntervalo.ValInf AndAlso Me.ValSup = pIntervalo.ValSup Then
            Return IntSolapadosOContenido.Iguales
        End If



        If Me.ValInf > pIntervalo.ValSup OrElse Me.ValSup < pIntervalo.ValInf Then
            Return IntSolapadosOContenido.Libres

        Else


            If Me.ValInf <= pIntervalo.ValInf AndAlso Me.ValSup >= pIntervalo.ValSup Then ' yo le contego
                Return IntSolapadosOContenido.Contenedor

            ElseIf Me.ValInf >= pIntervalo.ValInf AndAlso Me.ValSup <= pIntervalo.ValSup Then ' el me contine
                Return IntSolapadosOContenido.Contenido


            Else
                ' tenemos que estar solapados
                Return IntSolapadosOContenido.Solapados


            End If

        End If


    End Function

    Public Function BienFormado() As Boolean Implements IIntvaloNumerico.BienFormado
        If Me.mValInf > mValSup Then
            Return False
        Else
            Return True
        End If
    End Function
End Class






<Serializable()> _
Public Class ColIntvaloNumericoDN
    Inherits ArrayListValidable(Of IntvaloNumericoDN)



    Public Function TodosLosIntervalos(ByVal pTipo As IntSolapadosOContenido) As Boolean


        For Each int1 As IntvaloNumericoDN In Me
            For Each int2 As IntvaloNumericoDN In Me

                If int1.SolapadoOContenido(int2) <> pTipo Then
                    Return False
                End If
            Next

        Next

        Return True

    End Function


End Class




