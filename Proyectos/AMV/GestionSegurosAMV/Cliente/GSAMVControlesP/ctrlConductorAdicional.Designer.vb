<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlConductorAdicional
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
        Me.pnlConductor1 = New System.Windows.Forms.Panel
        Me.txtNIF = New ControlesPBase.txtValidable
        Me.lblNIF = New System.Windows.Forms.Label
        Me.lblEdadCalc = New System.Windows.Forms.Label
        Me.lblEdad1 = New System.Windows.Forms.Label
        Me.Label1 = New System.Windows.Forms.Label
        Me.cboParentesco = New ControlesPBase.CboValidador
        Me.Label2 = New System.Windows.Forms.Label
        Me.dtpFechaNacimiento = New System.Windows.Forms.DateTimePicker
        Me.Label3 = New System.Windows.Forms.Label
        Me.txtApellido2 = New ControlesPBase.txtValidable
        Me.Label4 = New System.Windows.Forms.Label
        Me.txtApellido1 = New ControlesPBase.txtValidable
        Me.txtNombre = New ControlesPBase.txtValidable
        Me.pnlConductor1.SuspendLayout()
        Me.SuspendLayout()
        '
        'pnlConductor1
        '
        Me.pnlConductor1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pnlConductor1.Controls.Add(Me.txtNIF)
        Me.pnlConductor1.Controls.Add(Me.lblNIF)
        Me.pnlConductor1.Controls.Add(Me.lblEdadCalc)
        Me.pnlConductor1.Controls.Add(Me.lblEdad1)
        Me.pnlConductor1.Controls.Add(Me.Label1)
        Me.pnlConductor1.Controls.Add(Me.cboParentesco)
        Me.pnlConductor1.Controls.Add(Me.Label2)
        Me.pnlConductor1.Controls.Add(Me.dtpFechaNacimiento)
        Me.pnlConductor1.Controls.Add(Me.Label3)
        Me.pnlConductor1.Controls.Add(Me.txtApellido2)
        Me.pnlConductor1.Controls.Add(Me.Label4)
        Me.pnlConductor1.Controls.Add(Me.txtApellido1)
        Me.pnlConductor1.Controls.Add(Me.txtNombre)
        Me.pnlConductor1.Location = New System.Drawing.Point(3, 3)
        Me.pnlConductor1.Name = "pnlConductor1"
        Me.pnlConductor1.Size = New System.Drawing.Size(520, 147)
        Me.pnlConductor1.TabIndex = 25
        '
        'txtNIF
        '
        Me.txtNIF.Location = New System.Drawing.Point(133, 59)
        Me.txtNIF.MaxLength = 9
        Me.txtNIF.MensajeErrorValidacion = Nothing
        Me.txtNIF.Name = "txtNIF"
        Me.txtNIF.Size = New System.Drawing.Size(73, 20)
        Me.txtNIF.TabIndex = 3
        Me.txtNIF.ToolTipText = Nothing
        Me.txtNIF.TrimText = False
        '
        'lblNIF
        '
        Me.lblNIF.AutoSize = True
        Me.lblNIF.Location = New System.Drawing.Point(87, 62)
        Me.lblNIF.Name = "lblNIF"
        Me.lblNIF.Size = New System.Drawing.Size(24, 13)
        Me.lblNIF.TabIndex = 27
        Me.lblNIF.Text = "NIF"
        '
        'lblEdadCalc
        '
        Me.lblEdadCalc.AutoSize = True
        Me.lblEdadCalc.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblEdadCalc.Location = New System.Drawing.Point(330, 91)
        Me.lblEdadCalc.Name = "lblEdadCalc"
        Me.lblEdadCalc.Size = New System.Drawing.Size(12, 15)
        Me.lblEdadCalc.TabIndex = 26
        Me.lblEdadCalc.Text = "-"
        '
        'lblEdad1
        '
        Me.lblEdad1.AutoSize = True
        Me.lblEdad1.Location = New System.Drawing.Point(283, 91)
        Me.lblEdad1.Name = "lblEdad1"
        Me.lblEdad1.Size = New System.Drawing.Size(32, 13)
        Me.lblEdad1.TabIndex = 25
        Me.lblEdad1.Text = "Edad"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(67, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(44, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Nombre"
        '
        'cboParentesco
        '
        Me.cboParentesco.ColorBotón = System.Drawing.SystemColors.Control
        Me.cboParentesco.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboParentesco.FormattingEnabled = True
        Me.cboParentesco.ItemHeight = 13
        Me.cboParentesco.Location = New System.Drawing.Point(133, 115)
        Me.cboParentesco.MensajeError = Nothing
        Me.cboParentesco.MensajeErrorValidacion = Nothing
        Me.cboParentesco.Name = "cboParentesco"
        Me.cboParentesco.Requerido = False
        Me.cboParentesco.RequeridoItem = False
        Me.cboParentesco.Size = New System.Drawing.Size(95, 21)
        Me.cboParentesco.SoloDouble = False
        Me.cboParentesco.SoloInteger = False
        Me.cboParentesco.TabIndex = 5
        Me.cboParentesco.ToolTipText = Nothing
        Me.cboParentesco.Validador1 = Nothing
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(62, 35)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(49, 13)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Apellidos"
        '
        'dtpFechaNacimiento
        '
        Me.dtpFechaNacimiento.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpFechaNacimiento.Location = New System.Drawing.Point(133, 87)
        Me.dtpFechaNacimiento.Name = "dtpFechaNacimiento"
        Me.dtpFechaNacimiento.Size = New System.Drawing.Size(95, 20)
        Me.dtpFechaNacimiento.TabIndex = 4
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(3, 91)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(108, 13)
        Me.Label3.TabIndex = 2
        Me.Label3.Text = "Fecha de Nacimiento"
        '
        'txtApellido2
        '
        Me.txtApellido2.Location = New System.Drawing.Point(330, 32)
        Me.txtApellido2.MensajeErrorValidacion = Nothing
        Me.txtApellido2.Name = "txtApellido2"
        Me.txtApellido2.Size = New System.Drawing.Size(182, 20)
        Me.txtApellido2.TabIndex = 2
        Me.txtApellido2.ToolTipText = Nothing
        Me.txtApellido2.TrimText = False
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(50, 118)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(61, 13)
        Me.Label4.TabIndex = 3
        Me.Label4.Text = "Parentesco"
        '
        'txtApellido1
        '
        Me.txtApellido1.Location = New System.Drawing.Point(133, 32)
        Me.txtApellido1.MensajeErrorValidacion = Nothing
        Me.txtApellido1.Name = "txtApellido1"
        Me.txtApellido1.Size = New System.Drawing.Size(182, 20)
        Me.txtApellido1.TabIndex = 1
        Me.txtApellido1.ToolTipText = Nothing
        Me.txtApellido1.TrimText = False
        '
        'txtNombre
        '
        Me.txtNombre.Location = New System.Drawing.Point(133, 6)
        Me.txtNombre.MensajeErrorValidacion = Nothing
        Me.txtNombre.Name = "txtNombre"
        Me.txtNombre.Size = New System.Drawing.Size(182, 20)
        Me.txtNombre.TabIndex = 0
        Me.txtNombre.ToolTipText = Nothing
        Me.txtNombre.TrimText = False
        '
        'ctrlConductorAdicional
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.pnlConductor1)
        Me.Name = "ctrlConductorAdicional"
        Me.Size = New System.Drawing.Size(526, 152)
        Me.pnlConductor1.ResumeLayout(False)
        Me.pnlConductor1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents pnlConductor1 As System.Windows.Forms.Panel
    Friend WithEvents lblEdadCalc As System.Windows.Forms.Label
    Friend WithEvents lblEdad1 As System.Windows.Forms.Label
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents cboParentesco As ControlesPBase.CboValidador
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents dtpFechaNacimiento As System.Windows.Forms.DateTimePicker
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents txtApellido2 As ControlesPBase.txtValidable
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents txtApellido1 As ControlesPBase.txtValidable
    Friend WithEvents txtNombre As ControlesPBase.txtValidable
    Friend WithEvents txtNIF As ControlesPBase.txtValidable
    Friend WithEvents lblNIF As System.Windows.Forms.Label

End Class
