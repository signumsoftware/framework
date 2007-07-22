Imports Framework.DatosNegocio

<Serializable()> _
Public Class CuestionarioResueltoDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mCuestionarioDN As CuestionarioDN
    Protected mColRespuestaDN As ColRespuestaDN

#End Region

#Region "Constructores"

    Public Sub New()
        Me.CambiarValorRef(Of ColRespuestaDN)(New ColRespuestaDN, mColRespuestaDN)
    End Sub

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mCuestionarioDN")> _
        Public Property CuestionarioDN() As CuestionarioDN
        Get
            Return Me.mCuestionarioDN
        End Get
        Set(ByVal value As CuestionarioDN)
            Me.CambiarValorRef(Of CuestionarioDN)(value, Me.mCuestionarioDN)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColRespuestaDN")> _
    Public Property ColRespuestaDN() As ColRespuestaDN
        Get
            Return Me.mColRespuestaDN
        End Get
        Set(ByVal value As ColRespuestaDN)
            Me.CambiarValorRef(Of ColRespuestaDN)(value, mColRespuestaDN)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Dim cadena As String = String.Empty

        If mCuestionarioDN IsNot Nothing Then
            cadena = mCuestionarioDN.ToString()
        End If

        Return cadena
    End Function

    Public Function ClonarCuestionarioRxC(ByVal cuestionario As CuestionarioDN) As CuestionarioResueltoDN
        Dim cuestionarioR As CuestionarioResueltoDN

        cuestionarioR = Me.CloneSuperficialSinIdentidad()
        cuestionarioR.mCuestionarioDN = cuestionario
        cuestionarioR.mColRespuestaDN = New ColRespuestaDN()

        If Me.mColRespuestaDN IsNot Nothing AndAlso cuestionario IsNot Nothing AndAlso cuestionario.ColPreguntaDN IsNot Nothing Then
            Dim colCaract As ColCaracteristicaDN = cuestionario.ColPreguntaDN.RecuperarColCaracteristicas()
            For Each preg As PreguntaDN In cuestionario.ColPreguntaDN
                Dim respuesta As RespuestaDN = mColRespuestaDN.RecuperarxCaracteristica(preg.CaracteristicaDN)
                If respuesta IsNot Nothing Then
                    cuestionarioR.mColRespuestaDN.AddUnico(respuesta.ClonarRespuesta())
                End If
            Next
        End If

        Return cuestionarioR
    End Function

#End Region

End Class


<Serializable()> Public Class HeCuestionarioResueltoDN
    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN)

    Public Sub New()

    End Sub

    Public Sub New(ByVal cr As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN)
        Me.AsignarEntidadReferida(cr)
    End Sub
End Class
