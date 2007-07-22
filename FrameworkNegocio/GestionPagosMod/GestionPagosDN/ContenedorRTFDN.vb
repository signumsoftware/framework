<Serializable()> Public Class ContenedorRTFDN
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mArrayString As ArrayString


    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pRTF As String)
        ComprobarArrayString()
        Me.mArrayString.RTF = pRTF
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Modificado
    End Sub

    Public Property RTF() As String
        Get
            ComprobarArrayString()
            Return Me.mArrayString.RTF
        End Get
        Set(ByVal value As String)
            ComprobarArrayString()
            Me.mArrayString.RTF = value
        End Set
    End Property

    Public Property ArrayString() As ArrayString
        Get
            Return mArrayString
        End Get
        Set(ByVal value As ArrayString)
            CambiarValorRef(Of ArrayString)(value, mArrayString)
        End Set
    End Property

    Private Sub ComprobarArrayString()
        If Me.mArrayString Is Nothing Then
            Me.mArrayString = New ArrayString
        End If
    End Sub

    Public Overrides Function ToString() As String
        If Not Me.mArrayString Is Nothing Then
            Return "[RTF]" 'Me.mArrayString.RTF
        Else
            Return String.Empty
        End If
    End Function


End Class


<Serializable()> Public Class ArrayString
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mRTF As String = String.Empty

    Public Sub New()
        MyBase.New()
    End Sub

    Public Property RTF() As String
        Get
            Return Me.mRTF
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(value, Me.mRTF)
        End Set
    End Property
End Class