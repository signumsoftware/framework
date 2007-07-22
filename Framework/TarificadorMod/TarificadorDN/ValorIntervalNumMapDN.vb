
Imports Framework.Cuestionario.CuestionarioDN

<Serializable()> _
Public Class ValorIntervalNumMapDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN
    Implements IValorMap

    Protected mIntervalo As Framework.DatosNegocio.IntvaloNumericoDN
    Protected mValor As Double
    Protected mNumMapPadre As ValorIntervalNumMapDN
    Protected mCaracteristica As Cuestionario.CuestionarioDN.CaracteristicaDN

    Public Property NumMapPadre() As ValorIntervalNumMapDN

        Get
            Return mNumMapPadre
        End Get

        Set(ByVal value As ValorIntervalNumMapDN)
            CambiarValorRef(Of ValorIntervalNumMapDN)(value, mNumMapPadre)

        End Set
    End Property

    Public Property Intervalo() As Framework.DatosNegocio.IntvaloNumericoDN
        Get
            Return Me.mIntervalo
        End Get
        Set(ByVal value As Framework.DatosNegocio.IntvaloNumericoDN)
            Me.CambiarValorRef(Of Framework.DatosNegocio.IntvaloNumericoDN)(value, Me.mIntervalo)
        End Set
    End Property

    Public Function TraduceElValor(ByVal pValor As Cuestionario.CuestionarioDN.IValorCaracteristicaDN) As Boolean Implements IValorMap.TraduceElValor
        Dim valor As Double = pValor.Valor

        '  si mi padre no puede traducir el valor yo tampoco

        If Me.mNumMapPadre Is Nothing Then
            Return mIntervalo.Contiene(valor) AndAlso Me.mPeriodo.Contiene(pValor.FechaEfectoValor)
        Else
            'problema mi padre debe recibir el valor padre de mi valor
            If mNumMapPadre.TraduceElValor(pValor.ValorCaracPadre) Then
                Return mIntervalo.Contiene(valor) AndAlso Me.mPeriodo.Contiene(pValor.FechaEfectoValor)
            Else
                Return False
            End If

        End If

    End Function

    Public Property Valor() As Object Implements IValorMap.Valor
        Get
            Return Me.mValor
        End Get
        Set(ByVal value As Object)
            Me.CambiarValorVal(Of Double)(value, Me.mValor)
        End Set
    End Property

    Public Property ValorNumerico() As Double
        Get
            Return Me.mValor
        End Get
        Set(ByVal value As Double)
            Me.CambiarValorVal(Of Double)(value, Me.mValor)
        End Set
    End Property

    Public Property Caracteristica() As Cuestionario.CuestionarioDN.CaracteristicaDN Implements IValorMap.Caracteristica
        Get
            Return mCaracteristica
        End Get
        Set(ByVal value As Cuestionario.CuestionarioDN.CaracteristicaDN)
            Me.CambiarValorRef(Of CaracteristicaDN)(value, mCaracteristica)
        End Set
    End Property

End Class


<Serializable()> _
Public Class ColValorIntervalNumMapDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of ValorIntervalNumMapDN)

    Public Function ContineValor(ByVal valor As Double) As Boolean

        For Each vn As ValorIntervalNumMapDN In Me

            If vn.Intervalo.Contiene(valor) Then
                Return True
            End If
        Next
        Return False

    End Function

    Public Function Recuperar(ByVal valor As Double, ByVal pCaracteristica As Cuestionario.CuestionarioDN.CaracteristicaDN) As ColValorIntervalNumMapDN
        Dim col As New ColValorIntervalNumMapDN

        For Each vn As ValorIntervalNumMapDN In Me
            If vn.Caracteristica.GUID = pCaracteristica.GUID AndAlso vn.Intervalo.Contiene(valor) Then
                col.Add(vn)
            End If
        Next

        Return col
    End Function

    Public Function Recuperar(ByVal valor As Double, ByVal pCaracteristica As Cuestionario.CuestionarioDN.CaracteristicaDN, ByVal pFechaEfecto As Date) As ColValorIntervalNumMapDN
        Dim col As New ColValorIntervalNumMapDN

        For Each vn As ValorIntervalNumMapDN In Me
            If vn.FI <= pFechaEfecto AndAlso (vn.FF = Date.MinValue OrElse pFechaEfecto <= vn.FF) AndAlso vn.Caracteristica.GUID = pCaracteristica.GUID AndAlso vn.Intervalo.Contiene(valor) Then
                col.Add(vn)
            End If
        Next

        Return col
    End Function

End Class