Imports System.Drawing.Printing


Public Class frmImpresion

#Region "atributos"
    Private m_nFirstCharOnPage As Integer
    'Dim printDoc As New PrintDocument

    Private mTalonDocumento As FN.GestionPagos.DN.TalonDocumentoDN
    Private mConfiguracionImpresion As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN
    'Private mNumeroSerie As String

    Private mPaqueteIU As PaqueteImpresion

    Private mControlador As frmImpresionctrl


#End Region

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        If Not Me.Paquete Is Nothing OrElse Not Paquete.Contains("Paquete") Then

            For Each txt As String In Me.cboZoom.Items
                If txt = "30%" Then
                    Me.cboZoom.SelectedItem = txt
                    Exit For
                End If
            Next

            Me.mPaqueteIU = Me.Paquete("Paquete")

            Me.mTalonDocumento = mPaqueteIU.TalonDocumento
            If Me.mTalonDocumento Is Nothing Then
                Throw New ApplicationException("No se ha pasado un TalónDocumento al formulario de impresión")
            End If
            Me.lblNumeroSerie.Text = Me.mTalonDocumento.NumeroSerie

            'cargamos la huella con el contenedorRTF en el RichTextBox1
            If Not Me.mTalonDocumento.HuellaRTF Is Nothing Then
                LNC.CargarHuella(Me.mTalonDocumento.HuellaRTF)
                Dim texto As String = CType(Me.mTalonDocumento.HuellaRTF.EntidadReferida, FN.GestionPagos.DN.ContenedorRTFDN).RTF
                If texto.StartsWith("{\rtf1") Then
                    'es un RTF
                    Me.RichTextBoxXTAPI1.Rtf = texto
                Else
                    'es texto plano
                    Me.RichTextBoxXTAPI1.Text = texto
                End If
            End If

            'asignamos la configuración de impresión que nos han pasado
            Me.mConfiguracionImpresion = mPaqueteIU.ConfiguracionImpresion
            If Me.mConfiguracionImpresion Is Nothing Then
                Throw New ApplicationException("No se ha pasado una Configuración de Impresión al formulario de impresión")
            End If

            Me.mPaqueteIU.MensajeError = String.Empty

            Me.PrintDocument1.DefaultPageSettings = Me.mConfiguracionImpresion.ConfigPagina
            Me.PrintDocPreview.DefaultPageSettings = Me.mConfiguracionImpresion.ConfigPagina

            Me.mControlador = Me.Controlador

        Else
            Throw New ApplicationException("No se ha establecido un Paquete para el formulario de impresión")
        End If

    End Sub

    Private Sub frmImpresion_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        'si estamos en impresión automática, mandamos a imprimir directamente
        If Me.mPaqueteIU.ImpresionSilenciosa Then
            Me.Visible = False
            PrintDocument1.PrinterSettings = Me.mPaqueteIU.PrinterSettings
            Try
                Imprimir()
            Catch ex As Exception
                Me.mPaqueteIU.MensajeError = ex.Message
                Me.mPaqueteIU.Impreso = False
                Me.Close()
            End Try
        End If
    End Sub


#Region "imprimir"

#Region "imprimir_preview"
    'hacemos lo mismo que en el imprimir normal, pero
    'esto evita que se lleve la cuenta de la impresión real
    'y que se modifique el paquete y se salga al terminar

    Private m_primercaracterdepagina As Integer

    Private Sub PrintDocPreview_BeginPrint(ByVal sender As Object, ByVal e As System.Drawing.Printing.PrintEventArgs) Handles PrintDocPreview.BeginPrint
        m_primercaracterdepagina = 0
    End Sub

    Private Sub PrintDocPreview_EndPrint(ByVal sender As Object, ByVal e As System.Drawing.Printing.PrintEventArgs) Handles PrintDocPreview.EndPrint
        ' Clean up cached information
        RichTextBoxXTAPI1.FormatRangeDone()
    End Sub

    Private Sub PrintDocPreview_PrintPage(ByVal sender As Object, ByVal e As System.Drawing.Printing.PrintPageEventArgs) Handles PrintDocPreview.PrintPage
        ' para imprimir los límites de los mágrenes
        ' descomentar la siguiente línea:
        ' e.Graphics.DrawRectangle(System.Drawing.Pens.Blue, e.MarginBounds)

        'hacemos al RichTextBoxAPI que calcule y renderice tanto texto
        'como quepa en la página y recuerde el último carácter que imprima
        'para el comienzo de la página siguiente
        m_primercaracterdepagina = RichTextBoxXTAPI1.FormatRange(False, e, m_primercaracterdepagina, RichTextBoxXTAPI1.TextLength)


        'ahora imprimimos el cheque

       ' Me.PintarCheque(Me.mTalonDocumento.Importe.ToString, Framework.Utilidades.ConvertidorTextoNum.RecuperarTextoNumero(mTalonDocumento.Importe.ToString).ToUpper, mTalonDocumento.Destinatario, mTalonDocumento.FechaImpresion.ToLongDateString, e)
        Me.PintarCheque(Me.mTalonDocumento.Importe.ToString, AuxIU.ConversorTextoANumero.ConvertirNumeroATexto(mTalonDocumento.Importe.ToString).ToUpper, mTalonDocumento.Destinatario, mTalonDocumento.FechaImpresion.ToLongDateString, e)

        ' comprueba si hay más páginas para imprimir
        If (m_primercaracterdepagina < RichTextBoxXTAPI1.TextLength) Then
            e.HasMorePages = True
        Else
            e.HasMorePages = False
        End If
    End Sub
#End Region

    Private Sub printDoc_BeginPrint(ByVal sender As Object, ByVal e As System.Drawing.Printing.PrintEventArgs) Handles PrintDocument1.BeginPrint
        ' Start at the beginning of the text
        m_nFirstCharOnPage = 0
    End Sub


    Private Sub printDoc_PrintPage(ByVal sender As Object, ByVal e As System.Drawing.Printing.PrintPageEventArgs) Handles PrintDocument1.PrintPage
        'ponemos los márgenes de impresión
        'e.PageSettings = Me.mConfiguracionImpresion.ConfigPagina

        ' para imprimir los límites de los mágrenes
        ' descomentar la siguiente línea:
        ' e.Graphics.DrawRectangle(System.Drawing.Pens.Blue, e.MarginBounds)

        'hacemos al RichTextBoxAPI que calcule y renderice tanto texto
        'como quepa en la página y recuerde el último carácter que imprima
        'para el comienzo de la página siguiente
        m_nFirstCharOnPage = RichTextBoxXTAPI1.FormatRange(False, e, m_nFirstCharOnPage, RichTextBoxXTAPI1.TextLength)


        'ahora imprimimos el cheque
        ' Me.PintarCheque(Me.mTalonDocumento.Importe, Framework.Utilidades.ConvertidorTextoNum.RecuperarTextoNumero(mTalonDocumento.Importe.ToString), mTalonDocumento.Destinatario, mTalonDocumento.FechaImpresion.ToLongDateString, e)
        Me.PintarCheque(Me.mTalonDocumento.Importe, AuxIU.ConversorTextoANumero.ConvertirNumeroATexto(mTalonDocumento.Importe.ToString), mTalonDocumento.Destinatario, mTalonDocumento.FechaImpresion.ToLongDateString, e)

        ' comprueba si hay más páginas para imprimir
        If (m_nFirstCharOnPage < RichTextBoxXTAPI1.TextLength) Then
            e.HasMorePages = True
        Else
            e.HasMorePages = False
        End If
    End Sub

    Private Sub printDoc_EndPrint(ByVal sender As Object, ByVal e As System.Drawing.Printing.PrintEventArgs) Handles PrintDocument1.EndPrint
        ' Clean up cached information
        RichTextBoxXTAPI1.FormatRangeDone()

        'apuntamos que hemos impreso y salimos
        Me.mPaqueteIU.Impreso = True
        Me.mPaqueteIU.TalonDocumento = Me.mTalonDocumento
        Me.Close()
    End Sub

    Private Sub Imprimir()
        'realizamos la operación en el servidor y, si falla,
        'no hacemos la impresión y salimos, notificando el error
        If Not AutorizarWebservice(Me.mPaqueteIU.MensajeError) Then
            Me.mPaqueteIU.Impreso = False
            Me.mPaqueteIU.TalonDocumento = Me.mTalonDocumento
            Me.Close()
        Else
            'comienza el proceso de impresión
            PrintDocument1.Print()
        End If
    End Sub

    Private Sub SolicitarImprimir()

        Me.PrintDialog1.Document = PrintDocument1
        If Me.PrintDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
            Imprimir()
        End If

    End Sub

    Private Sub PintarCheque(ByVal CantidadNumero As String, ByVal CantidadTexto As String, ByVal Destinatario As String, ByVal Fecha As String, ByVal e As System.Drawing.Printing.PrintPageEventArgs)

        Dim fuente As Font = Me.mConfiguracionImpresion.Fuente
        Dim mibrush As Brush = Brushes.Black

        'las cantidades ya están en milímetros

        'ahora ajustamos en función de ese margen
        Dim GX As Single = Me.mConfiguracionImpresion.GeneralX
        Dim GY As Single = Me.mConfiguracionImpresion.GeneralY

        Dim CantX As Single = Me.mConfiguracionImpresion.CantidadX + GX
        Dim CantY As Single = Me.mConfiguracionImpresion.CantidadY + GY
        Dim DestX As Single = Me.mConfiguracionImpresion.DestinatarioX + GX
        Dim DestY As Single = Me.mConfiguracionImpresion.DestinatarioY + GY
        Dim CantLeX As Single = Me.mConfiguracionImpresion.CantidadLetrasX + GX
        Dim CantLeY As Single = Me.mConfiguracionImpresion.CantidadLetrasY + GY
        Dim FecX As Single = Me.mConfiguracionImpresion.FechaX + GX
        Dim FecY As Single = Me.mConfiguracionImpresion.FechaY + GY

        e.Graphics.PageUnit = GraphicsUnit.Millimeter


        'las imágenes que haya en la configuración de página
        If Not Me.mConfiguracionImpresion.ColImagenes Is Nothing Then
            For Each ci As FN.GestionPagos.DN.ContenedorImagenDN In Me.mConfiguracionImpresion.ColImagenes
                Dim ImX As Single = ci.ImagenX
                Dim ImY As Single = ci.ImagenY
                If ci.AplicarDesviacion Then
                    ImX += GX
                    ImY += GY
                End If
                'pintamos la imagen
                e.Graphics.DrawImageUnscaled(ci.Imagen, ImX, ImY)
            Next
        End If


        'la cantidad en números
        e.Graphics.DrawString(New AuxIU.FormateadorMoneda(2).Formatear(CantidadNumero), fuente, mibrush, CantX, CantY)
        'e.Graphics.DrawString(CantidadNumero, fuente, mibrush, CentesimaDePulgada(CantX), CentesimaDePulgada(CantY))

        'el destinatario
        e.Graphics.DrawString(Destinatario, fuente, mibrush, DestX, DestY)
        'e.Graphics.DrawString(Destinatario, fuente, mibrush, CentesimaDePulgada(DestX), CentesimaDePulgada(DestY))

        'la cantidad en letras
        'debemos medir si va a ocupar más de una linea y si es así lo imprimimos en dos líneas
        Dim formatoimpresion As New System.Drawing.StringFormat
        formatoimpresion.Trimming = StringTrimming.Word

        Dim tamañodelimitado As SizeF = e.Graphics.MeasureString(CantidadTexto, fuente, 130, formatoimpresion)
        'Dim tamañodelimitado As SizeF = e.Graphics.MeasureString(CantidadTexto, fuente, CentesimaDePulgada(130), formatoimpresion)

        Dim rectF As New RectangleF(New System.Drawing.PointF(CantLeX, CantLeY), tamañodelimitado)
        'Dim rectF As New RectangleF(New System.Drawing.PointF(CentesimaDePulgada(CantLeX), CentesimaDePulgada(CantLeY)), tamañodelimitado)

        e.Graphics.DrawString(CantidadTexto, fuente, mibrush, rectF, formatoimpresion)

        'la fecha
        e.Graphics.DrawString(Fecha, fuente, mibrush, FecX, FecY)
        'e.Graphics.DrawString(Fecha, fuente, mibrush, CentesimaDePulgada(FecX), CentesimaDePulgada(FecY))

    End Sub


    Private Function CentesimaDePulgada(ByVal pMilimetros As Integer) As Int32
        'convertimos los milimetros a pulgada
        Dim pulg As Double = AuxIU.Conversores.MilimetrosAPulgadas(pMilimetros)
        'multiplicamos por 100
        Return pulg * 100

        'Return (pMilimetros / 25.4) * 100
    End Function


    Private Function ParsearASingle(ByVal pTexto As String) As Single
        Dim parsed As Single = 0
        If Not Single.TryParse(pTexto, parsed) Then
            Return 0
        End If
        Return parsed
    End Function

#End Region

#Region "Autorización WebService"

    ''' <summary>
    ''' Realiza la operación de "impresión" en el servidor en el caso de que
    ''' sno se trate de una prueba.
    ''' </summary>
    ''' <returns>true si todo va bien</returns>
    ''' <param name="pMensaje">el mensaje de error en el caso de que lo haya</param> 
    ''' <remarks></remarks>
    Private Function AutorizarWebservice(ByRef pMensaje As String) As Boolean

        If Not Me.mPaqueteIU.Prueba Then
            Return Me.mControlador.AutorizarImpresionTalon(Me.mTalonDocumento.Talon.Pago, pMensaje)
        Else
            Return True
        End If

    End Function

#End Region

    Private Sub Imprimir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdImprimir.Click
        Try
            Me.SolicitarImprimir()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub


    'Private Sub txtZoom_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtZoom.TextChanged
    '    Try
    '        Dim texto As String = Me.txtZoom.Text.Trim
    '        Dim dz As Double = 0
    '        If Double.TryParse(texto, dz) AndAlso dz > 0 Then
    '            Me.PrintPreviewControl1.Zoom = dz
    '        End If
    '    Catch ex As Exception
    '        MostrarError(ex)
    '    End Try
    'End Sub


    Private Sub ComboBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboZoom.SelectedIndexChanged
        Try
            Me.PrintPreviewControl1.Zoom = CInt(Microsoft.VisualBasic.Left(Me.cboZoom.SelectedItem.ToString, Me.cboZoom.SelectedItem.ToString.Length - 1)) / 100
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub chkAjustar_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkAjustar.CheckedChanged
        Try
            Me.cboZoom.Enabled = Not Me.chkAjustar.Checked
            Me.PrintPreviewControl1.AutoZoom = Me.chkAjustar.Checked
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Try
            Me.Close()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub
End Class