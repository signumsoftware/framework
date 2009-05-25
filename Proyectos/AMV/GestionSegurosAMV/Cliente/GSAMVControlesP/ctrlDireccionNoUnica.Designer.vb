<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlDireccionNoUnica
    Inherits MotorIU.ControlesP.BaseControlP

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.cboTipoVia = New ControlesPBase.CboValidador
        Me.tXTVia = New ControlesPBase.textboxXT
        Me.Label2 = New System.Windows.Forms.Label
        Me.txtCodPostal = New ControlesPBase.txtValidable
        Me.Label3 = New System.Windows.Forms.Label
        Me.cboLocalidad = New ControlesPBase.CboValidador
        Me.SuspendLayout()
        '
        'cboTipoVia
        '
        Me.cboTipoVia.ColorBotón = System.Drawing.SystemColors.Control
        Me.cboTipoVia.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboTipoVia.FormattingEnabled = True
        Me.cboTipoVia.Location = New System.Drawing.Point(3, 3)
        Me.cboTipoVia.MensajeError = Nothing
        Me.cboTipoVia.MensajeErrorValidacion = Nothing
        Me.cboTipoVia.Name = "cboTipoVia"
        Me.cboTipoVia.Requerido = False
        Me.cboTipoVia.RequeridoItem = False
        Me.cboTipoVia.Size = New System.Drawing.Size(86, 21)
        Me.cboTipoVia.SoloDouble = False
        Me.cboTipoVia.SoloInteger = False
        Me.cboTipoVia.Sorted = True
        Me.cboTipoVia.TabIndex = 0
        Me.cboTipoVia.ToolTipText = Nothing
        Me.cboTipoVia.Validador1 = Nothing
        '
        'tXTVia
        '
        Me.tXTVia.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.tXTVia.Extendido = False
        Me.tXTVia.Location = New System.Drawing.Point(95, 4)
        Me.tXTVia.MensajeErrorValidacion = Nothing
        Me.tXTVia.Name = "tXTVia"
        Me.tXTVia.ReadonlyXT = False
        Me.tXTVia.Size = New System.Drawing.Size(295, 20)
        Me.tXTVia.TabIndex = 1
        Me.tXTVia.ToolTipText = Nothing
        Me.tXTVia.TrimText = False
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(3, 36)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(72, 13)
        Me.Label2.TabIndex = 4
        Me.Label2.Text = "Código Postal"
        '
        'txtCodPostal
        '
        Me.txtCodPostal.Location = New System.Drawing.Point(81, 33)
        Me.txtCodPostal.MaxLength = 5
        Me.txtCodPostal.MensajeErrorValidacion = Nothing
        Me.txtCodPostal.Name = "txtCodPostal"
        Me.txtCodPostal.Size = New System.Drawing.Size(59, 20)
        Me.txtCodPostal.SoloInteger = True
        Me.txtCodPostal.TabIndex = 2
        Me.txtCodPostal.ToolTipText = Nothing
        Me.txtCodPostal.TrimText = False
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(146, 36)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(53, 13)
        Me.Label3.TabIndex = 6
        Me.Label3.Text = "Localidad"
        '
        'cboLocalidad
        '
        Me.cboLocalidad.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cboLocalidad.ColorBotón = System.Drawing.SystemColors.Control
        Me.cboLocalidad.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboLocalidad.FormattingEnabled = True
        Me.cboLocalidad.Location = New System.Drawing.Point(205, 33)
        Me.cboLocalidad.MensajeError = Nothing
        Me.cboLocalidad.MensajeErrorValidacion = Nothing
        Me.cboLocalidad.Name = "cboLocalidad"
        Me.cboLocalidad.Requerido = False
        Me.cboLocalidad.RequeridoItem = False
        Me.cboLocalidad.Size = New System.Drawing.Size(185, 21)
        Me.cboLocalidad.SoloDouble = False
        Me.cboLocalidad.SoloInteger = False
        Me.cboLocalidad.Sorted = True
        Me.cboLocalidad.TabIndex = 3
        Me.cboLocalidad.ToolTipText = Nothing
        Me.cboLocalidad.Validador1 = Nothing
        '
        'ctrlDireccionNoUnica
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.cboLocalidad)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.txtCodPostal)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.tXTVia)
        Me.Controls.Add(Me.cboTipoVia)
        Me.Name = "ctrlDireccionNoUnica"
        Me.Size = New System.Drawing.Size(397, 60)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents cboTipoVia As ControlesPBase.CboValidador
    Friend WithEvents tXTVia As ControlesPBase.textboxXT
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents txtCodPostal As ControlesPBase.txtValidable
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents cboLocalidad As ControlesPBase.CboValidador

End Class
