<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlValor
    Inherits MotorIU.ControlesP.BaseControlP

    'UserControl overrides dispose to clean up the component list.
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
        Me.cboValor = New System.Windows.Forms.ComboBox
        Me.dtpValor = New System.Windows.Forms.DateTimePicker
        Me.SuspendLayout()
        '
        'cboValor
        '
        Me.cboValor.Dock = System.Windows.Forms.DockStyle.Fill
        Me.cboValor.FormattingEnabled = True
        Me.cboValor.Location = New System.Drawing.Point(0, 0)
        Me.cboValor.Name = "cboValor"
        Me.cboValor.Size = New System.Drawing.Size(150, 21)
        Me.cboValor.TabIndex = 0
        '
        'dtpValor
        '
        Me.dtpValor.Dock = System.Windows.Forms.DockStyle.Fill
        Me.dtpValor.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpValor.Location = New System.Drawing.Point(0, 0)
        Me.dtpValor.Name = "dtpValor"
        Me.dtpValor.Size = New System.Drawing.Size(150, 20)
        Me.dtpValor.TabIndex = 1
        '
        'ctrlValor
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.cboValor)
        Me.Controls.Add(Me.dtpValor)
        Me.Name = "ctrlValor"
        Me.Size = New System.Drawing.Size(150, 20)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents cboValor As System.Windows.Forms.ComboBox
    Friend WithEvents dtpValor As System.Windows.Forms.DateTimePicker

End Class
