<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmImpresion
    Inherits MotorIU.FormulariosP.FormularioBase

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing AndAlso components IsNot Nothing Then
            components.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmImpresion))
        Me.PrintDialog1 = New System.Windows.Forms.PrintDialog
        Me.cmdImprimir = New System.Windows.Forms.Button
        Me.RichTextBoxXTAPI1 = New ControlesPBaseAPI.RichTextBoxXTAPI
        Me.cmdCancelar = New System.Windows.Forms.Button
        Me.PrintDocument1 = New System.Drawing.Printing.PrintDocument
        Me.Label1 = New System.Windows.Forms.Label
        Me.lblNumeroSerie = New System.Windows.Forms.Label
        Me.PrintPreviewControl1 = New System.Windows.Forms.PrintPreviewControl
        Me.PrintDocPreview = New System.Drawing.Printing.PrintDocument
        Me.Label3 = New System.Windows.Forms.Label
        Me.Label4 = New System.Windows.Forms.Label
        Me.PageSetupDialog1 = New System.Windows.Forms.PageSetupDialog
        Me.cboZoom = New System.Windows.Forms.ComboBox
        Me.chkAjustar = New System.Windows.Forms.CheckBox
        Me.SuspendLayout()
        '
        'PrintDialog1
        '
        Me.PrintDialog1.UseEXDialog = True
        '
        'cmdImprimir
        '
        Me.cmdImprimir.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdImprimir.Image = CType(resources.GetObject("cmdImprimir.Image"), System.Drawing.Image)
        Me.cmdImprimir.Location = New System.Drawing.Point(465, 451)
        Me.cmdImprimir.Name = "cmdImprimir"
        Me.cmdImprimir.Size = New System.Drawing.Size(46, 43)
        Me.cmdImprimir.TabIndex = 33
        Me.cmdImprimir.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        Me.cmdImprimir.UseVisualStyleBackColor = True
        '
        'RichTextBoxXTAPI1
        '
        Me.RichTextBoxXTAPI1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.RichTextBoxXTAPI1.Location = New System.Drawing.Point(311, 365)
        Me.RichTextBoxXTAPI1.Name = "RichTextBoxXTAPI1"
        Me.RichTextBoxXTAPI1.Size = New System.Drawing.Size(252, 129)
        Me.RichTextBoxXTAPI1.TabIndex = 36
        Me.RichTextBoxXTAPI1.Text = ""
        Me.RichTextBoxXTAPI1.Visible = False
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCancelar.Image = CType(resources.GetObject("cmdCancelar.Image"), System.Drawing.Image)
        Me.cmdCancelar.Location = New System.Drawing.Point(517, 451)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.Size = New System.Drawing.Size(46, 43)
        Me.cmdCancelar.TabIndex = 38
        Me.cmdCancelar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'PrintDocument1
        '
        '
        'Label1
        '
        Me.Label1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(13, 453)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(89, 13)
        Me.Label1.TabIndex = 39
        Me.Label1.Text = "Número de Serie:"
        '
        'lblNumeroSerie
        '
        Me.lblNumeroSerie.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lblNumeroSerie.AutoSize = True
        Me.lblNumeroSerie.ForeColor = System.Drawing.Color.Blue
        Me.lblNumeroSerie.Location = New System.Drawing.Point(108, 453)
        Me.lblNumeroSerie.Name = "lblNumeroSerie"
        Me.lblNumeroSerie.Size = New System.Drawing.Size(14, 13)
        Me.lblNumeroSerie.TabIndex = 40
        Me.lblNumeroSerie.Text = "#"
        '
        'PrintPreviewControl1
        '
        Me.PrintPreviewControl1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.PrintPreviewControl1.AutoZoom = False
        Me.PrintPreviewControl1.Document = Me.PrintDocPreview
        Me.PrintPreviewControl1.Location = New System.Drawing.Point(16, 40)
        Me.PrintPreviewControl1.Name = "PrintPreviewControl1"
        Me.PrintPreviewControl1.Size = New System.Drawing.Size(547, 398)
        Me.PrintPreviewControl1.TabIndex = 41
        Me.PrintPreviewControl1.UseAntiAlias = True
        Me.PrintPreviewControl1.Zoom = 0.33704020530367834
        '
        'PrintDocPreview
        '
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(13, 9)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(173, 13)
        Me.Label3.TabIndex = 43
        Me.Label3.Text = "Previsualización del Talón Impreso:"
        '
        'Label4
        '
        Me.Label4.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(312, 10)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(34, 13)
        Me.Label4.TabIndex = 46
        Me.Label4.Text = "Zoom"
        '
        'cboZoom
        '
        Me.cboZoom.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cboZoom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboZoom.FormattingEnabled = True
        Me.cboZoom.Items.AddRange(New Object() {"200%", "150%", "115%", "100%", "75%", "50%", "30%", "15%", "10%", "5%"})
        Me.cboZoom.Location = New System.Drawing.Point(352, 6)
        Me.cboZoom.Name = "cboZoom"
        Me.cboZoom.Size = New System.Drawing.Size(76, 21)
        Me.cboZoom.TabIndex = 47
        '
        'chkAjustar
        '
        Me.chkAjustar.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.chkAjustar.AutoSize = True
        Me.chkAjustar.Location = New System.Drawing.Point(452, 8)
        Me.chkAjustar.Name = "chkAjustar"
        Me.chkAjustar.Size = New System.Drawing.Size(111, 17)
        Me.chkAjustar.TabIndex = 48
        Me.chkAjustar.Text = "Ajustar al Tamaño"
        Me.chkAjustar.UseVisualStyleBackColor = True
        '
        'frmImpresion
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(575, 506)
        Me.Controls.Add(Me.chkAjustar)
        Me.Controls.Add(Me.cboZoom)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.PrintPreviewControl1)
        Me.Controls.Add(Me.lblNumeroSerie)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.cmdImprimir)
        Me.Controls.Add(Me.RichTextBoxXTAPI1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmImpresion"
        Me.Text = "Impresion de Talón"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents PrintDialog1 As System.Windows.Forms.PrintDialog
    Friend WithEvents cmdImprimir As System.Windows.Forms.Button
    Friend WithEvents RichTextBoxXTAPI1 As ControlesPBaseAPI.RichTextBoxXTAPI
    Friend WithEvents cmdCancelar As System.Windows.Forms.Button
    Friend WithEvents PrintDocument1 As System.Drawing.Printing.PrintDocument
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents lblNumeroSerie As System.Windows.Forms.Label
    Friend WithEvents PrintPreviewControl1 As System.Windows.Forms.PrintPreviewControl
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents PrintDocPreview As System.Drawing.Printing.PrintDocument
    Friend WithEvents PageSetupDialog1 As System.Windows.Forms.PageSetupDialog
    Friend WithEvents cboZoom As System.Windows.Forms.ComboBox
    Friend WithEvents chkAjustar As System.Windows.Forms.CheckBox
End Class
