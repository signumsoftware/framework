<Serializable()> _
Public Class PreguntaDN
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mCaracteristica As CaracteristicaDN
    Protected mTextoPregunta As String

    Public Property TextoPregunta() As String
        Get
            Return mTextoPregunta
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mTextoPregunta)
        End Set
    End Property

    Public Property CaracteristicaDN() As CaracteristicaDN
        Get
            Return mCaracteristica
        End Get
        Set(ByVal value As CaracteristicaDN)
            Me.CambiarValorRef(Of CaracteristicaDN)(value, mCaracteristica)
        End Set
    End Property
End Class

<Serializable()> _
Public Class ColPreguntaDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of PreguntaDN)

    Public Function RecuperarColCaracteristicas() As ColCaracteristicaDN
        Dim colC As New ColCaracteristicaDN()

        For Each prg As PreguntaDN In Me
            colC.AddUnico(prg.CaracteristicaDN)
        Next

        Return colC

    End Function

End Class