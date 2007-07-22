<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmCuestionario1
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
        Me.CtrlCuestionarioTarificacion1 = New GSAMVControlesP.ctrlCuestionarioTarificacion
        Me.SuspendLayout()
        '
        'CtrlCuestionarioTarificacion1
        '
        Me.CtrlCuestionarioTarificacion1.BackColor = System.Drawing.SystemColors.Control
        Me.CtrlCuestionarioTarificacion1.Location = New System.Drawing.Point(12, 12)
        Me.CtrlCuestionarioTarificacion1.MensajeError = ""
        Me.CtrlCuestionarioTarificacion1.Name = "CtrlCuestionarioTarificacion1"
        Me.CtrlCuestionarioTarificacion1.Size = New System.Drawing.Size(690, 452)
        Me.CtrlCuestionarioTarificacion1.TabIndex = 0
        Me.CtrlCuestionarioTarificacion1.ToolTipText = Nothing
        '
        'frmCuestionario1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoScroll = True
        Me.ClientSize = New System.Drawing.Size(712, 469)
        Me.Controls.Add(Me.CtrlCuestionarioTarificacion1)
        Me.Name = "frmCuestionario1"
        Me.Text = "Cuestionario de tarificación"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents CtrlCuestionarioTarificacion1 As GSAMVControlesP.ctrlCuestionarioTarificacion
End Class
