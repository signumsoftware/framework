<Serializable()> _
Public Class RespuestaDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"

    Protected mPreguntaDN As PreguntaDN
    Protected mIValorCaractDN As IValorCaracteristicaDN

#End Region

#Region "Propiedades"

    Public Property PreguntaDN() As PreguntaDN
        Get
            Return mPreguntaDN
        End Get
        Set(ByVal value As PreguntaDN)
            Me.CambiarValorRef(Of PreguntaDN)(value, mPreguntaDN)
        End Set
    End Property

    Public Property IValorCaracteristicaDN() As IValorCaracteristicaDN
        Get
            Return mIValorCaractDN
        End Get
        Set(ByVal value As IValorCaracteristicaDN)
            Me.CambiarValorRef(Of IValorCaracteristicaDN)(value, mIValorCaractDN)

        End Set
    End Property

#End Region

#Region "Métodos"

    Public Function ClonarRespuesta() As RespuestaDN
        Dim respuestaClon As RespuestaDN

        respuestaClon = Me.CloneSuperficialSinIdentidad()
        If Me.mIValorCaractDN IsNot Nothing Then
            respuestaClon.mIValorCaractDN = Me.mIValorCaractDN.ClonarIValorCaracteristica()
        End If

        Return respuestaClon
    End Function

#End Region

End Class

<Serializable()> _
Public Class ColRespuestaDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of RespuestaDN)

    Public Function RecuperarxCaracteristica(ByVal pCaracteristica As CaracteristicaDN) As RespuestaDN

        For Each res As RespuestaDN In Me
            If res.PreguntaDN.CaracteristicaDN.GUID = pCaracteristica.GUID Then
                Return res
            End If

        Next
        Return Nothing
    End Function

    Public Function RecuperarRespuestaaxPregunta(ByVal nombrePregunta As String) As RespuestaDN

        For Each resp As RespuestaDN In Me
            If resp.PreguntaDN.Nombre = nombrePregunta Then
                Return resp
            End If
        Next

        Return Nothing

    End Function


End Class