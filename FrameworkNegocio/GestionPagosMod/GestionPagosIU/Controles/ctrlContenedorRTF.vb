Public Class ctrlContenedorRTF

    Private mContenedorRTF As FN.GestionPagos.DN.ContenedorRTFDN

    Public Property ContenedorRTF() As FN.GestionPagos.DN.ContenedorRTFDN
        Get
            If IUaDN() Then
                Return Me.mContenedorRTF
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As FN.GestionPagos.DN.ContenedorRTFDN)
            Me.mContenedorRTF = value
            DNaIU(Me.mContenedorRTF)
        End Set
    End Property

    Protected Overrides Function IUaDN() As Boolean
        If Me.RichTextBox1.Text.Trim = String.Empty Then
            Me.MensajeError = "No hay contenido en el cuadro de texto"
            Return False
        End If
        If Me.mContenedorRTF Is Nothing Then
            Me.mContenedorRTF = New FN.GestionPagos.DN.ContenedorRTFDN
        End If
        Me.mContenedorRTF.RTF = Me.RichTextBox1.Rtf
        Return True
    End Function

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        If Me.mContenedorRTF Is Nothing Then
            Me.RichTextBox1.Rtf = String.Empty
        Else
            If Me.mContenedorRTF.RTF.StartsWith("{\rtf1") Then
                'es rtf
                Me.RichTextBox1.Rtf = Me.mContenedorRTF.RTF
            Else
                'es texto plano
                Me.RichTextBox1.Text = Me.mContenedorRTF.RTF
            End If
        End If
    End Sub


End Class
