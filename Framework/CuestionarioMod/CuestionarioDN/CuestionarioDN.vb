Imports Framework.DatosNegocio

<Serializable()> Public Class CuestionarioDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

    Protected mColPreguntaDN As ColPreguntaDN


    Public Sub New()
        Me.CambiarValorRef(Of ColPreguntaDN)(New ColPreguntaDN, mColPreguntaDN)
    End Sub

    Public Property ColPreguntaDN() As ColPreguntaDN
        Get
            Return mColPreguntaDN
        End Get
        Set(ByVal value As ColPreguntaDN)
            Me.CambiarValorRef(Of ColPreguntaDN)(value, mColPreguntaDN)
        End Set
    End Property

End Class


<Serializable()> _
Public Class ColCuestionarioDN
    Inherits ArrayListValidable(Of CuestionarioDN)

    Public Function RecuperarCuestionarioxFecha(ByVal fechaEfecto As Date) As CuestionarioDN
        For Each cuest As CuestionarioDN In Me
            If cuest.Contiene(fechaEfecto) Then
                Return cuest
            End If
        Next

        Return Nothing

    End Function

End Class