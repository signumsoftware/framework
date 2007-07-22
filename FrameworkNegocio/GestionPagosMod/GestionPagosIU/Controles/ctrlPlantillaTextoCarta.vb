Public Class ctrlPlantillaTextoCarta

#Region "Atributos"
    Private mPlantillaCarta As FN.GestionPagos.DN.PlantillaCartaDN
    Private mlistareemplazos As New List(Of FN.GestionPagos.DN.ReemplazosTextoCartasDN)
#End Region

#Region "incializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        mlistareemplazos = LNC.RecuperarTodosReemplazos()
        If Not mlistareemplazos Is Nothing Then
            For Each r As FN.GestionPagos.DN.ReemplazosTextoCartasDN In mlistareemplazos
                Me.lstReemplazos.Items.Add(r)
            Next
        End If

    End Sub
#End Region

#Region "Propiedades"
    Public Property PlantillaCarta() As FN.GestionPagos.DN.PlantillaCartaDN
        Get
            If IUaDN() Then
                Return Me.mPlantillaCarta
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As FN.GestionPagos.DN.PlantillaCartaDN)
            Me.mPlantillaCarta = value
            DNaIU(Me.mPlantillaCarta)
        End Set
    End Property
#End Region

#Region "Establecer y Rellenar Datos"
    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        If Me.mPlantillaCarta Is Nothing Then
            Me.txtNombre.Text = String.Empty
            Me.RichTextBox1.Text = String.Empty
        Else
            Me.txtNombre.Text = Me.mPlantillaCarta.Nombre
            'nos aseguramos de que está cargada la huella
            LNC.CargarHuella(Me.mPlantillaCarta.HuellaRTF)
            Me.RichTextBox1.Rtf = CType(Me.mPlantillaCarta.HuellaRTF.EntidadReferida, FN.GestionPagos.DN.ContenedorRTFDN).RTF
        End If
    End Sub

    Protected Overrides Function IUaDN() As Boolean
        If String.IsNullOrEmpty(Me.txtNombre.Text.Trim) Then
            Me.MensajeError = "No se ha definido un nombre para la plantilla"
            Return False
        End If

        If String.IsNullOrEmpty(Me.RichTextBox1.Rtf) Then
            Me.MensajeError = "No se ha definido el texto de la plantilla"
            Return False
        End If

        If Me.mPlantillaCarta Is Nothing Then
            Me.mPlantillaCarta = New FN.GestionPagos.DN.PlantillaCartaDN
        End If

        Me.mPlantillaCarta.Nombre = Me.txtNombre.Text

        'ponemos la huella
        Dim crtf As FN.GestionPagos.DN.ContenedorRTFDN
        If Me.mPlantillaCarta.HuellaRTF Is Nothing Then
            'si no hay huella
            crtf = New FN.GestionPagos.DN.ContenedorRTFDN(Me.RichTextBox1.Rtf)
            Me.mPlantillaCarta.HuellaRTF = New FN.GestionPagos.DN.HuellaContenedorRTFDN(crtf)
        Else
            'hay huella
            'la recargamos
            LNC.CargarHuella(Me.mPlantillaCarta.HuellaRTF)
            crtf = Me.mPlantillaCarta.HuellaRTF.EntidadReferida
        End If

        crtf.RTF = Me.RichTextBox1.Rtf

        Me.MensajeError = String.Empty
        Return True
    End Function
#End Region

#Region "Métodos"
    Private Sub cmdPrueba_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdPrueba.Click
        Try
            If String.IsNullOrEmpty(Me.RichTextBox1.Text.Trim) Then
                MessageBox.Show("No se ha definido ningún texto modelo", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If

            Dim mipaquete As New Hashtable
            mipaquete.Add("Texto", Me.RichTextBox1.Rtf)
            Me.Marco.Navegar("PruebaPlantillaCarta", Me.ParentForm, MotorIU.Motor.TipoNavegacion.Modal, mipaquete)

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub cmdAbrirArchivo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAbrirArchivo.Click
        Try
            Me.OpenFileDialog1.Filter = "Formato de Texto Enriquecido(*.rtf)|*.rtf|Formato de texto simple(*.txt)|*.txt|Todos los archivos(*.*)|*.*"
            Me.OpenFileDialog1.CheckFileExists = True
            Me.OpenFileDialog1.Multiselect = False
            Me.OpenFileDialog1.Title = "Seleccionar Archivo de texto modelo"
            Me.OpenFileDialog1.InitialDirectory = System.IO.Directory.GetCurrentDirectory
            Me.OpenFileDialog1.FileName = String.Empty
            If Me.OpenFileDialog1.ShowDialog = DialogResult.OK Then
                If System.IO.File.Exists(Me.OpenFileDialog1.FileName) Then
                    Me.RichTextBox1.LoadFile(Me.OpenFileDialog1.FileName)
                End If
            End If
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub lstReemplazos_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles lstReemplazos.DoubleClick
        Try
            If Not lstReemplazos.SelectedItem Is Nothing Then
                'ponemos el texto en el sitio seleccionado del richtextbox
                Me.RichTextBox1.SelectedText = CType(lstReemplazos.SelectedItem, FN.GestionPagos.DN.ReemplazosTextoCartasDN).TextoOriginal
            End If
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#End Region




End Class
