<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmOperador
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmOperador))
        Me.CtrlOperador1 = New ClienteAdmin.ctrlOperador
        Me.cmdCancelar = New System.Windows.Forms.Button
        Me.cmd_Aceptar = New System.Windows.Forms.Button
        Me.cmdAltaBaja = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'CtrlOperador1
        '
        Me.CtrlOperador1.BackColor = System.Drawing.SystemColors.Control
        Me.CtrlOperador1.Location = New System.Drawing.Point(3, -1)
        Me.CtrlOperador1.MensajeError = ""
        Me.CtrlOperador1.Name = "CtrlOperador1"
        Me.CtrlOperador1.Size = New System.Drawing.Size(653, 539)
        Me.CtrlOperador1.TabIndex = 0
        Me.CtrlOperador1.ToolTipText = Nothing
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Image = CType(resources.GetObject("cmdCancelar.Image"), System.Drawing.Image)
        Me.cmdCancelar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdCancelar.Location = New System.Drawing.Point(660, 74)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.Size = New System.Drawing.Size(79, 24)
        Me.cmdCancelar.TabIndex = 3
        Me.cmdCancelar.Text = "Cancelar"
        Me.cmdCancelar.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'cmd_Aceptar
        '
        Me.cmd_Aceptar.Image = CType(resources.GetObject("cmd_Aceptar.Image"), System.Drawing.Image)
        Me.cmd_Aceptar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmd_Aceptar.Location = New System.Drawing.Point(660, 12)
        Me.cmd_Aceptar.Name = "cmd_Aceptar"
        Me.cmd_Aceptar.Size = New System.Drawing.Size(79, 24)
        Me.cmd_Aceptar.TabIndex = 1
        Me.cmd_Aceptar.Text = "Aceptar"
        Me.cmd_Aceptar.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.cmd_Aceptar.UseVisualStyleBackColor = True
        '
        'cmdAltaBaja
        '
        Me.cmdAltaBaja.Image = CType(resources.GetObject("cmdAltaBaja.Image"), System.Drawing.Image)
        Me.cmdAltaBaja.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdAltaBaja.Location = New System.Drawing.Point(660, 43)
        Me.cmdAltaBaja.Name = "cmdAltaBaja"
        Me.cmdAltaBaja.Size = New System.Drawing.Size(79, 24)
        Me.cmdAltaBaja.TabIndex = 2
        Me.cmdAltaBaja.Text = "Baja"
        Me.cmdAltaBaja.UseVisualStyleBackColor = True
        '
        'frmOperador
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(749, 541)
        Me.Controls.Add(Me.cmdAltaBaja)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.cmd_Aceptar)
        Me.Controls.Add(Me.CtrlOperador1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.MaximizeBox = False
        Me.Name = "frmOperador"
        Me.Text = "Operador"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents CtrlOperador1 As ClienteAdmin.ctrlOperador
    Friend WithEvents cmdCancelar As System.Windows.Forms.Button
    Friend WithEvents cmd_Aceptar As System.Windows.Forms.Button
    Friend WithEvents cmdAltaBaja As System.Windows.Forms.Button

End Class
