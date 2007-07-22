Public Class ctrlConfiguracionPuntosImpresion

#Region "atributos"
    Private mConfiguracionImpresion As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN
    Private mFuenteSeleccionada As System.Drawing.Font
    Private mPageSettingSeleccionado As System.Drawing.Printing.PageSettings

    Private mDatatableIm As DataTable
    Private mColImagenes As FN.GestionPagos.DN.ColContenedorImagenDN
#End Region


#Region "inicializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
    End Sub
#End Region


#Region "propiedades"
    Public Property ConfiguracionImpresion() As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN
        Get
            If IUaDN() Then
                Return Me.mConfiguracionImpresion
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN)
            Me.mConfiguracionImpresion = value
            Me.DNaIU(value)
        End Set
    End Property
#End Region


#Region "establecer y rellenar datos"
    Protected Overrides Function IUaDN() As Boolean
        'comprobamos si hay fuente
        If Me.mFuenteSeleccionada Is Nothing Then
            Me.MensajeError = "Debe seleccionar una fuente"
            Return False
        End If

        If Me.mPageSettingSeleccionado Is Nothing Then
            Me.MensajeError = "Debe seleccionar la configuración de la página"
            Return False
        End If

        If String.IsNullOrEmpty(Me.txtNombre.Text.Trim) Then
            Me.MensajeError = "Debe seleccionar un nombre para la configuración"
            Return False
        End If



        'está bien

        If Me.mConfiguracionImpresion Is Nothing Then
            Me.mConfiguracionImpresion = New FN.GestionPagos.DN.ConfiguracionImpresionTalonDN
        End If

        Me.mConfiguracionImpresion.Nombre = Me.txtNombre.Text.Trim
        Me.mConfiguracionImpresion.Fuente = Me.mFuenteSeleccionada
        Me.mConfiguracionImpresion.ConfigPagina = Me.mPageSettingSeleccionado

        Me.mConfiguracionImpresion.CantidadLetrasX = ParsearASingle(Me.txtCantidadLetrasX.Text)
        Me.mConfiguracionImpresion.CantidadLetrasY = ParsearASingle(Me.txtCantidadLetrasY.Text)
        Me.mConfiguracionImpresion.CantidadX = ParsearASingle(Me.txtCantidadX.Text)
        Me.mConfiguracionImpresion.CantidadY = ParsearASingle(Me.txtCantidadY.Text)
        Me.mConfiguracionImpresion.DestinatarioX = ParsearASingle(Me.txtDestinatarioX.Text)
        Me.mConfiguracionImpresion.DestinatarioY = ParsearASingle(Me.txtDestinatarioY.Text)
        Me.mConfiguracionImpresion.FechaX = ParsearASingle(Me.txtFechaX.Text)
        Me.mConfiguracionImpresion.FechaY = ParsearASingle(Me.txtFechaY.Text)
        Me.mConfiguracionImpresion.GeneralX = ParsearASingle(Me.txtGeneralX.Text)
        Me.mConfiguracionImpresion.GeneralY = ParsearASingle(Me.txtGeneralY.Text)
        Me.mConfiguracionImpresion.ColImagenes = Me.mColImagenes

        Return True
    End Function

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        If Me.mConfiguracionImpresion Is Nothing Then
            Me.txtCantidadLetrasX.Text = "0"
            Me.txtCantidadLetrasY.Text = "0"
            Me.txtCantidadX.Text = "0"
            Me.txtCantidadY.Text = "0"
            Me.txtDestinatarioX.Text = "0"
            Me.txtDestinatarioY.Text = "0"
            Me.txtFechaX.Text = "0"
            Me.txtFechaY.Text = "0"
            Me.txtGeneralX.Text = "0"
            Me.txtGeneralY.Text = "0"
            Me.txtNombre.Text = String.Empty
            Me.mFuenteSeleccionada = Nothing
            Me.PageSetupDialog1.Reset()
            Me.mPageSettingSeleccionado = Nothing
            CargarImagenes(Nothing)
        Else
            Me.txtNombre.Text = Me.mConfiguracionImpresion.Nombre
            Me.txtCantidadLetrasX.Text = Me.mConfiguracionImpresion.CantidadLetrasX.ToString
            Me.txtCantidadLetrasY.Text = Me.mConfiguracionImpresion.CantidadLetrasY.ToString
            Me.txtCantidadX.Text = Me.mConfiguracionImpresion.CantidadX.ToString
            Me.txtCantidadY.Text = Me.mConfiguracionImpresion.CantidadY.ToString
            Me.txtDestinatarioX.Text = Me.mConfiguracionImpresion.DestinatarioX.ToString
            Me.txtDestinatarioY.Text = Me.mConfiguracionImpresion.DestinatarioY.ToString
            Me.txtFechaX.Text = Me.mConfiguracionImpresion.FechaX.ToString
            Me.txtFechaY.Text = Me.mConfiguracionImpresion.FechaY.ToString
            Me.txtGeneralX.Text = Me.mConfiguracionImpresion.GeneralX.ToString
            Me.txtGeneralY.Text = Me.mConfiguracionImpresion.GeneralY.ToString
            Me.mPageSettingSeleccionado = Me.mConfiguracionImpresion.ConfigPagina
            Me.mFuenteSeleccionada = Me.mConfiguracionImpresion.Fuente
            Me.txtFuente.Text = Me.mFuenteSeleccionada.ToString '.Name & " - " & Me.mFuenteSeleccionada.Size.ToString
            CargarImagenes(Me.mConfiguracionImpresion.ColImagenes)
        End If
    End Sub

    Private Sub CargarImagenes(ByVal pcolim As FN.GestionPagos.DN.ColContenedorImagenDN)
        'ponemos la colección en el atributo
        Me.mColImagenes = pcolim

        'creamos el datatable
        If Me.mDatatableIm Is Nothing Then
            Me.mDatatableIm = New DataTable
            Me.mDatatableIm.Columns.Add(New DataColumn("Objeto", GetType(FN.GestionPagos.DN.ContenedorImagenDN)))
            Me.mDatatableIm.Columns.Add(New DataColumn("Imagen", GetType(Image)))
            Me.mDatatableIm.Columns.Add(New DataColumn("Nombre", GetType(String)))
            Me.mDatatableIm.Columns.Add(New DataColumn("Eje X", GetType(Single)))
            Me.mDatatableIm.Columns.Add(New DataColumn("Eje Y", GetType(Single)))
            Me.mDatatableIm.Columns.Add(New DataColumn("Ajustar Desviación", GetType(Boolean)))
        Else
            Me.mDatatableIm.Clear()
        End If

        'rellenamos el datatable
        If Not pcolim Is Nothing Then
            For Each mic As FN.GestionPagos.DN.ContenedorImagenDN In pcolim
                AgregarImagenATabla(mic)
            Next
        End If

        'asignamos el datatsource
        Me.DataGridView1.DataSource = Nothing
        Me.DataGridView1.DataSource = Me.mDatatableIm

        'ocultamos la columna "objeto" si la hay
        If Me.DataGridView1.Columns.Contains("Objeto") Then
            Me.DataGridView1.Columns("Objeto").Visible = False
        End If
        'hacemos que la última columna lo rellene todo
        Me.DataGridView1.Columns(Me.DataGridView1.Columns.Count - 1).MinimumWidth = 100
        Me.DataGridView1.Columns(Me.DataGridView1.Columns.Count - 1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        'Me.DataGridView1.Columns("Ajustar Desviación").Width = 200

        'ponemos todas las imágenes como zoom
        For Each c As DataGridViewColumn In Me.DataGridView1.Columns
            If TypeOf c Is DataGridViewImageColumn Then
                CType(c, DataGridViewImageColumn).ImageLayout = DataGridViewImageCellLayout.Zoom
            End If
        Next
    End Sub
#End Region


#Region "Cambiar signo de los textbox"
    Private Sub SignoGX_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SignoGX.Click
        Try
            Me.txtGeneralX.Text = (ParsearASingle(Me.txtGeneralX.Text) * (-1)).ToString
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub SignoGY_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SignoGY.Click
        Try
            Me.txtGeneralY.Text = (ParsearASingle(Me.txtGeneralY.Text) * (-1)).ToString
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub


    Private Function ParsearASingle(ByVal pTexto As String) As Single
        Dim parsed As Single = 0
        If Not Single.TryParse(pTexto, parsed) Then
            Return 0
        End If
        Return parsed
    End Function
#End Region

#Region "reset"
    Private Sub cmdReset_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdReset.Click
        Try
            Me.txtCantidadLetrasX.Text = ConvertirEnMM(7).ToString
            Me.txtCantidadLetrasY.Text = ConvertirEnMM(24.9).ToString
            Me.txtCantidadX.Text = ConvertirEnMM(15.5).ToString
            Me.txtCantidadY.Text = ConvertirEnMM(23.8).ToString
            Me.txtDestinatarioX.Text = ConvertirEnMM(8.5).ToString
            Me.txtDestinatarioY.Text = ConvertirEnMM(24.4).ToString
            Me.txtFechaX.Text = ConvertirEnMM(9.5).ToString
            Me.txtFechaY.Text = ConvertirEnMM(25.9).ToString
            Me.txtGeneralX.Text = "0"
            Me.txtGeneralY.Text = "-9"
            Me.txtNombre.Text = String.Empty
            Me.mFuenteSeleccionada = New Font("Arial", 10)
            Me.txtFuente.Text = Me.mFuenteSeleccionada.ToString

            Dim mipagesettings As New System.Drawing.Printing.PageSettings
            mipagesettings.PaperSize = New System.Drawing.Printing.PaperSize("A4", Me.CentesimaDePulgada(21), Me.CentesimaDePulgada(29.7))
            mipagesettings.Landscape = False
            mipagesettings.Margins = New System.Drawing.Printing.Margins(Me.CentesimaDePulgada(1), Me.CentesimaDePulgada(1), Me.CentesimaDePulgada(1), Me.CentesimaDePulgada(1))
            Me.PageSetupDialog1.PageSettings = mipagesettings
            Me.mPageSettingSeleccionado = mipagesettings

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Function CentesimaDePulgada(ByVal pCentimetros As Double) As Int32
        Return (pCentimetros / 2.54) * 100
    End Function

    Private Function ConvertirEnMM(ByVal pCentimetro As Single) As Single
        Return (pCentimetro * 10)
    End Function
#End Region


    Private Sub cmdConfiguracionPagina_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdConfiguracionPagina.Click
        Try
            If Not Me.mPageSettingSeleccionado Is Nothing Then
                Me.PageSetupDialog1.PageSettings = Me.mPageSettingSeleccionado
            Else
                Dim mipagesettings As New System.Drawing.Printing.PageSettings
                mipagesettings.PaperSize = New System.Drawing.Printing.PaperSize("A4", Me.CentesimaDePulgada(21), Me.CentesimaDePulgada(29.7))
                mipagesettings.Landscape = False
                mipagesettings.Margins = New System.Drawing.Printing.Margins(Me.CentesimaDePulgada(1), Me.CentesimaDePulgada(1), Me.CentesimaDePulgada(1), Me.CentesimaDePulgada(1))
                Me.PageSetupDialog1.PageSettings = mipagesettings
            End If

            Me.PageSetupDialog1.AllowMargins = True
            Me.PageSetupDialog1.AllowOrientation = True
            Me.PageSetupDialog1.AllowPaper = True
            Me.PageSetupDialog1.AllowPrinter = False

            If Me.PageSetupDialog1.ShowDialog = DialogResult.OK Then

                Me.mPageSettingSeleccionado = Me.PageSetupDialog1.PageSettings
            End If
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#Region "fuente"
    Private Sub cmdFuente_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdFuente.Click
        Try
            If Not Me.mFuenteSeleccionada Is Nothing Then
                Me.FontDialog1.Font = Me.mFuenteSeleccionada

            Else

            End If
            If Me.FontDialog1.ShowDialog = DialogResult.OK Then
                Me.mFuenteSeleccionada = Me.FontDialog1.Font
                Me.txtFuente.Text = Me.mFuenteSeleccionada.ToString
            End If
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub
#End Region

#Region "imágenes"

    Private Sub AgregarImagenATabla(ByVal pCI As FN.GestionPagos.DN.ContenedorImagenDN)
        Dim mir As DataRow = Me.mDatatableIm.NewRow
        mir("Objeto") = pCI
        mir("Imagen") = pCI.Imagen
        mir("Nombre") = pCI.Nombre
        mir("Eje X") = pCI.ImagenX
        mir("Eje Y") = pCI.ImagenY
        mir("Ajustar Desviación") = pCI.AplicarDesviacion
        Me.mDatatableIm.Rows.Add(mir)
    End Sub

    Private Sub cmdAgregarImagen_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAgregarImagen.Click
        Try
            Dim mipaquete As New PaqueteImagen
            Me.Marco.Navegar("ImagenEmbebida", Me.FormularioPadre, Nothing, MotorIU.Motor.TipoNavegacion.Modal, mipaquete.GenerarPaquete)
            If Not mipaquete Is Nothing AndAlso Not mipaquete.ContenedorImagen Is Nothing Then
                If Me.mColImagenes Is Nothing Then
                    Me.mColImagenes = New FN.GestionPagos.DN.ColContenedorImagenDN
                End If
                Me.mColImagenes.Add(mipaquete.ContenedorImagen)
                Me.CargarImagenes(Me.mColImagenes)
                'AgregarImagenATabla(mipaquete.ContenedorImagen)
                'Me.DataGridView1.Refresh()
            End If
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub cmdEliminarImagen_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdEliminarImagen.Click
        Try
            If Algoseleccionado() Then
                If MessageBox.Show("¿Está seguro de que desea eliminar la imagen de la configuración?", "Elminar Imagen", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                    Dim ci As FN.GestionPagos.DN.ContenedorImagenDN = Me.DataGridView1.SelectedRows(0).Cells(0).Value
                    Me.mColImagenes.Remove(ci)
                    CargarImagenes(Me.mColImagenes)
                End If
            End If
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub cmdNavegarImagen_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdNavegarImagen.Click
        Try
            If Algoseleccionado() Then
                Dim ci As FN.GestionPagos.DN.ContenedorImagenDN = Me.DataGridView1.SelectedRows(0).Cells(0).Value
                Dim mipaquete As New PaqueteImagen
                mipaquete.ContenedorImagen = ci
                Me.Marco.Navegar("ImagenEmbebida", Me.FormularioPadre, Nothing, MotorIU.Motor.TipoNavegacion.Modal, mipaquete.GenerarPaquete)
                Me.CargarImagenes(Me.mColImagenes)
            End If
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Function Algoseleccionado() As Boolean
        If Me.DataGridView1.SelectedRows.Count = 0 Then
            MessageBox.Show("Debe seleccionar una imagen", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return False
        End If
        Return True
    End Function

#End Region

End Class
