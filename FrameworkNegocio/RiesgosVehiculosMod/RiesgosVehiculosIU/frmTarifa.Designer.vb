<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmTarifa
    Inherits MotorIU.FormulariosP.FormularioBase

    'Form overrides dispose to clean up the component list.
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmTarifa))
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip
        Me.tsbGuardar = New System.Windows.Forms.ToolStripButton
        Me.tsbNavegarCuestionario = New System.Windows.Forms.ToolStripButton
        Me.Label1 = New System.Windows.Forms.Label
        Me.Label2 = New System.Windows.Forms.Label
        Me.chkTarifaRenovacion = New System.Windows.Forms.CheckBox
        Me.Label3 = New System.Windows.Forms.Label
        Me.txtNumSiniestros = New System.Windows.Forms.TextBox
        Me.txtBonificacionActual = New System.Windows.Forms.TextBox
        Me.ctrlTarifa1 = New FN.RiesgosVehiculos.IU.Controles.ctrlTarifa
        Me.Label4 = New System.Windows.Forms.Label
        Me.lblNivelBonificacion = New System.Windows.Forms.Label
        Me.ToolStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'ToolStrip1
        '
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.tsbGuardar, Me.tsbNavegarCuestionario})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 0)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Size = New System.Drawing.Size(835, 25)
        Me.ToolStrip1.TabIndex = 1
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'tsbGuardar
        '
        Me.tsbGuardar.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.tsbGuardar.Image = CType(resources.GetObject("tsbGuardar.Image"), System.Drawing.Image)
        Me.tsbGuardar.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.tsbGuardar.Name = "tsbGuardar"
        Me.tsbGuardar.Size = New System.Drawing.Size(23, 22)
        Me.tsbGuardar.Text = "Guardar"
        '
        'tsbNavegarCuestionario
        '
        Me.tsbNavegarCuestionario.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.tsbNavegarCuestionario.Image = CType(resources.GetObject("tsbNavegarCuestionario.Image"), System.Drawing.Image)
        Me.tsbNavegarCuestionario.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.tsbNavegarCuestionario.Name = "tsbNavegarCuestionario"
        Me.tsbNavegarCuestionario.Size = New System.Drawing.Size(23, 22)
        Me.tsbNavegarCuestionario.Text = "Modificar datos del cuestionario"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(268, 63)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(97, 13)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "Bonificación actual"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(19, 63)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(90, 13)
        Me.Label2.TabIndex = 3
        Me.Label2.Text = "Número siniestros"
        '
        'chkTarifaRenovacion
        '
        Me.chkTarifaRenovacion.AutoSize = True
        Me.chkTarifaRenovacion.Location = New System.Drawing.Point(129, 38)
        Me.chkTarifaRenovacion.Name = "chkTarifaRenovacion"
        Me.chkTarifaRenovacion.Size = New System.Drawing.Size(15, 14)
        Me.chkTarifaRenovacion.TabIndex = 0
        Me.chkTarifaRenovacion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        Me.chkTarifaRenovacion.UseVisualStyleBackColor = True
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(19, 38)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(90, 13)
        Me.Label3.TabIndex = 4
        Me.Label3.Text = "Tarifa renovación"
        '
        'txtNumSiniestros
        '
        Me.txtNumSiniestros.Location = New System.Drawing.Point(129, 60)
        Me.txtNumSiniestros.Name = "txtNumSiniestros"
        Me.txtNumSiniestros.Size = New System.Drawing.Size(59, 20)
        Me.txtNumSiniestros.TabIndex = 1
        '
        'txtBonificacionActual
        '
        Me.txtBonificacionActual.Location = New System.Drawing.Point(387, 60)
        Me.txtBonificacionActual.Name = "txtBonificacionActual"
        Me.txtBonificacionActual.Size = New System.Drawing.Size(59, 20)
        Me.txtBonificacionActual.TabIndex = 5
        '
        'ctrlTarifa1
        '
        Me.ctrlTarifa1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ctrlTarifa1.DN = CType(resources.GetObject("ctrlTarifa1.DN"), Object)
        Me.ctrlTarifa1.Location = New System.Drawing.Point(12, 86)
        Me.ctrlTarifa1.MensajeError = ""
        Me.ctrlTarifa1.Name = "ctrlTarifa1"
        Me.ctrlTarifa1.Size = New System.Drawing.Size(810, 559)
        Me.ctrlTarifa1.TabIndex = 0
        Me.ctrlTarifa1.ToolTipText = Nothing
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(524, 63)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(91, 13)
        Me.Label4.TabIndex = 6
        Me.Label4.Text = "Nivel bonificación"
        '
        'lblNivelBonificacion
        '
        Me.lblNivelBonificacion.AutoSize = True
        Me.lblNivelBonificacion.Location = New System.Drawing.Point(631, 63)
        Me.lblNivelBonificacion.Name = "lblNivelBonificacion"
        Me.lblNivelBonificacion.Size = New System.Drawing.Size(16, 13)
        Me.lblNivelBonificacion.TabIndex = 7
        Me.lblNivelBonificacion.Text = " - "
        '
        'frmTarifa
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(835, 657)
        Me.Controls.Add(Me.lblNivelBonificacion)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.txtBonificacionActual)
        Me.Controls.Add(Me.txtNumSiniestros)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.chkTarifaRenovacion)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Controls.Add(Me.ctrlTarifa1)
        Me.Name = "frmTarifa"
        Me.Text = "Tarificador"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ctrlTarifa1 As FN.RiesgosVehiculos.IU.Controles.ctrlTarifa
    Friend WithEvents ToolStrip1 As System.Windows.Forms.ToolStrip
    Friend WithEvents tsbGuardar As System.Windows.Forms.ToolStripButton
    Friend WithEvents tsbNavegarCuestionario As System.Windows.Forms.ToolStripButton
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents chkTarifaRenovacion As System.Windows.Forms.CheckBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents txtNumSiniestros As System.Windows.Forms.TextBox
    Friend WithEvents txtBonificacionActual As System.Windows.Forms.TextBox
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents lblNivelBonificacion As System.Windows.Forms.Label

End Class
