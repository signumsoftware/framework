Imports FN.GestionPagos.DN


Public Class ctrlImagen

#Region "atributos"
    Private mContenedorImagen As ContenedorImagenDN

#End Region

#Region "propiedades"
    Public Property ContenedorImagen() As ContenedorImagenDN
        Get
            If IUaDN() Then
                Return Me.mContenedorImagen
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As ContenedorImagenDN)
            Me.mContenedorImagen = value
            DNaIU(Me.mContenedorImagen)
        End Set
    End Property
#End Region

#Region "establecer y rellenar datos"
    Protected Overrides Function IUaDN() As Boolean
        Try
            If Me.PictureBox1.Image Is Nothing Then
                Me.MensajeError = "No se ha seleccionado ninguna imagen"
                Return False
            End If

            If Me.mContenedorImagen Is Nothing Then
                Me.mContenedorImagen = New ContenedorImagenDN
            End If

            Me.mContenedorImagen.Imagen = Me.PictureBox1.Image
            Me.mContenedorImagen.Nombre = Me.txtNombre.Text
            Single.TryParse(Me.txtImagenX.Text, Me.mContenedorImagen.ImagenX)
            Single.TryParse(Me.txtImagenY.Text, Me.mContenedorImagen.ImagenY)
            Me.mContenedorImagen.AplicarDesviacion = Me.chkDesviacion.Checked

            Return True
        Catch ex As Exception
            Me.MensajeError = ex.Message
            Return False
        End Try
    End Function


    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        If Me.mContenedorImagen Is Nothing Then
            Me.PictureBox1.Image = Nothing
            Me.txtNombre.Text = String.Empty
            Me.txtImagenX.Text = String.Empty
            Me.txtImagenY.Text = String.Empty
            Me.chkDesviacion.Checked = False
        Else
            Me.PictureBox1.Image = Me.mContenedorImagen.Imagen
            Me.txtNombre.Text = Me.mContenedorImagen.Nombre
            Me.txtImagenY.Text = Me.mContenedorImagen.ImagenY.ToString
            Me.txtImagenX.Text = Me.mContenedorImagen.ImagenX.ToString
            Me.chkDesviacion.Checked = Me.mContenedorImagen.AplicarDesviacion
        End If
    End Sub

    Private Sub cmdImagen_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdImagen.Click
        Try
            If Me.OpenFileDialog1.ShowDialog = DialogResult.OK Then
                Me.PictureBox1.Image = System.Drawing.Bitmap.FromFile(Me.OpenFileDialog1.FileName)
            End If
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#End Region


End Class
