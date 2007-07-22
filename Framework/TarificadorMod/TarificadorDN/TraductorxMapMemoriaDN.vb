
Public Class TraductorxMapMemoriaDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements ITraductorDN
    Protected mColValorModMap As ColValorModMapDN



    Public Sub New()
        MyBase.New()
        Me.CambiarValorRef(Of ColValorModMapDN)(New ColValorModMapDN, mColValorModMap)
    End Sub

    Public Property ColValorModMap() As ColValorModMapDN
        Get
            Return Me.mColValorModMap
        End Get
        Set(ByVal value As ColValorModMapDN)
            Me.CambiarValorRef(Of ColValorModMapDN)(value, mColValorModMap)
        End Set
    End Property

    Public Function TraducirValor(ByVal valor As Object) As Object Implements ITraductorDN.TraducirValor
        Dim valc As New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        valc.ValorNumerico = valor
        valc.Caracteristica = Me.mColValorModMap.Item(0).Caracteristica


        For Each valmap As ValorModMapDN In Me.mColValorModMap
            If valmap.TraduceElValor(valc) Then
                Return valmap.Valor
            End If
        Next

        Return Nothing
    End Function

End Class
