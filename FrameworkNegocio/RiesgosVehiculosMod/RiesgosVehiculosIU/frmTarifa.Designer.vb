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
        Me.ctrlTarifa1 = New FN.RiesgosVehiculos.IU.Controles.ctrlTarifa
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.ctrlTarifa1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ctrlTarifa1.Location = New System.Drawing.Point(12, 12)
        Me.ctrlTarifa1.MensajeError = ""
        Me.ctrlTarifa1.Name = "ctrlTarifa1"
        Me.ctrlTarifa1.Size = New System.Drawing.Size(673, 349)
        Me.ctrlTarifa1.TabIndex = 0
        '
        'frmTarifa
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(698, 408)
        Me.Controls.Add(Me.ctrlTarifa1)
        Me.Name = "frmTarifa"
        Me.Text = "Form1"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ctrlTarifa1 As FN.RiesgosVehiculos.IU.Controles.ctrlTarifa

End Class
