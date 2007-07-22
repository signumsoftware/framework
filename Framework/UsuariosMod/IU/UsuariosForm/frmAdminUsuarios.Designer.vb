<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmAdminUsuarios
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmAdminUsuarios))
        Me.cmdAceptar = New System.Windows.Forms.Button
        Me.cmdCancelar = New System.Windows.Forms.Button
        Me.CtrlUsuarios1 = New Framework.Usuarios.IUWin.Controles.CtrlUsuario
        Me.cmdAltaBaja = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'cmdAceptar
        '
        Me.cmdAceptar.Image = CType(resources.GetObject("cmdAceptar.Image"), System.Drawing.Image)
        Me.cmdAceptar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdAceptar.Location = New System.Drawing.Point(561, 10)
        Me.cmdAceptar.Name = "cmdAceptar"
        Me.cmdAceptar.Size = New System.Drawing.Size(79, 24)
        Me.cmdAceptar.TabIndex = 1
        Me.cmdAceptar.Text = "Aceptar"
        Me.cmdAceptar.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.cmdAceptar.UseVisualStyleBackColor = True
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Image = CType(resources.GetObject("cmdCancelar.Image"), System.Drawing.Image)
        Me.cmdCancelar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdCancelar.Location = New System.Drawing.Point(561, 70)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.Size = New System.Drawing.Size(79, 24)
        Me.cmdCancelar.TabIndex = 3
        Me.cmdCancelar.Text = "Cancelar"
        Me.cmdCancelar.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'CtrlUsuarios1
        '
        Me.CtrlUsuarios1.BackColor = System.Drawing.SystemColors.Control
        Me.CtrlUsuarios1.Dock = System.Windows.Forms.DockStyle.Left
        Me.CtrlUsuarios1.Location = New System.Drawing.Point(0, 0)
        Me.CtrlUsuarios1.MensajeError = ""
        Me.CtrlUsuarios1.Name = "CtrlUsuarios1"
        Me.CtrlUsuarios1.Size = New System.Drawing.Size(551, 417)
        Me.CtrlUsuarios1.TabIndex = 0
        Me.CtrlUsuarios1.ToolTipText = Nothing
        '
        'cmdAltaBaja
        '
        Me.cmdAltaBaja.Image = CType(resources.GetObject("cmdAltaBaja.Image"), System.Drawing.Image)
        Me.cmdAltaBaja.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdAltaBaja.Location = New System.Drawing.Point(561, 40)
        Me.cmdAltaBaja.Name = "cmdAltaBaja"
        Me.cmdAltaBaja.Size = New System.Drawing.Size(79, 24)
        Me.cmdAltaBaja.TabIndex = 2
        Me.cmdAltaBaja.Text = "Baja"
        Me.cmdAltaBaja.UseVisualStyleBackColor = True
        '
        'frmAdminUsuarios
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(652, 417)
        Me.Controls.Add(Me.cmdAltaBaja)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.cmdAceptar)
        Me.Controls.Add(Me.CtrlUsuarios1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.MaximizeBox = False
        Me.Name = "frmAdminUsuarios"
        Me.Text = "Administración de Usuarios"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents CtrlUsuarios1 As Framework.Usuarios.IUWin.Controles.CtrlUsuario
    Friend WithEvents cmdAceptar As System.Windows.Forms.Button
    Friend WithEvents cmdCancelar As System.Windows.Forms.Button
    Friend WithEvents cmdAltaBaja As System.Windows.Forms.Button

End Class
