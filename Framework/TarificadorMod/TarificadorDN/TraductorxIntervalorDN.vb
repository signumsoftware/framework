
<Serializable()> _
Public Class TraductorxIntervNumDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements ITraductorDN


    Protected mColValorIntervalNumMapDN As ColValorIntervalNumMapDN


    Public Sub New()
        MyBase.New()
        Me.CambiarValorRef(Of ColValorIntervalNumMapDN)(New ColValorIntervalNumMapDN, mColValorIntervalNumMapDN)
    End Sub

    Public Overridable Function TraducirValor(ByVal pvalor As Object) As Object Implements ITraductorDN.TraducirValor


        Dim valc As Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        If TypeOf pvalor Is Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN Then
            valc = pvalor
        Else
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("El tipo de valor del elemento a traducir debe ser numérico")
            valc = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
            valc.ValorNumerico = pvalor
            ' valc.Caracteristica = Me.mColValorIntervalNumMapDN.Item(0).CaracteristicaDN

        End If

        'Dim colValorIntervalorsEnFecha As ColValorIntervalNumMapDN = mColValorIntervalNumMapDN.Recuperar(valc.ValorNumerico, valc.Caracteristica, valc.FechaEfectoValor)

        'If colValorIntervalorsEnFecha.Count = 1 Then
        '    Return colValorIntervalorsEnFecha.Item(0).Valor
        'ElseIf colValorIntervalorsEnFecha.Count > 1 Then

        'End If

        For Each valmap As ValorIntervalNumMapDN In Me.ColValorIntervalNumMap
            If valmap.TraduceElValor(valc) Then
                Return valmap.Valor
            End If
        Next

        Return Nothing

    End Function


    Public Property ColValorIntervalNumMap() As ColValorIntervalNumMapDN
        Get
            Return Me.mColValorIntervalNumMapDN
        End Get
        Set(ByVal value As ColValorIntervalNumMapDN)
            Me.CambiarValorRef(Of ColValorIntervalNumMapDN)(value, mColValorIntervalNumMapDN)

        End Set
    End Property






End Class
