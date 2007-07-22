<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlMulticonductor
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
        Me.ctrlConductorAdicional1 = New GSAMVControlesP.ctrlConductorAdicional
        Me.CtrlConductorAdicional2 = New GSAMVControlesP.ctrlConductorAdicional
        Me.CtrlConductorAdicional3 = New GSAMVControlesP.ctrlConductorAdicional
        Me.CtrlConductorAdicional4 = New GSAMVControlesP.ctrlConductorAdicional
        Me.SuspendLayout()
        '
        'ctrlConductorAdicional1
        '
        Me.ctrlConductorAdicional1.Location = New System.Drawing.Point(0, 3)
        Me.ctrlConductorAdicional1.MensajeError = ""
        Me.ctrlConductorAdicional1.Name = "ctrlConductorAdicional1"
        Me.ctrlConductorAdicional1.Size = New System.Drawing.Size(526, 152)
        Me.ctrlConductorAdicional1.TabIndex = 0
        Me.ctrlConductorAdicional1.ToolTipText = Nothing
        '
        'CtrlConductorAdicional2
        '
        Me.CtrlConductorAdicional2.Location = New System.Drawing.Point(0, 155)
        Me.CtrlConductorAdicional2.MensajeError = ""
        Me.CtrlConductorAdicional2.Name = "CtrlConductorAdicional2"
        Me.CtrlConductorAdicional2.Size = New System.Drawing.Size(526, 152)
        Me.CtrlConductorAdicional2.TabIndex = 1
        Me.CtrlConductorAdicional2.ToolTipText = Nothing
        '
        'CtrlConductorAdicional3
        '
        Me.CtrlConductorAdicional3.Location = New System.Drawing.Point(0, 306)
        Me.CtrlConductorAdicional3.MensajeError = ""
        Me.CtrlConductorAdicional3.Name = "CtrlConductorAdicional3"
        Me.CtrlConductorAdicional3.Size = New System.Drawing.Size(526, 152)
        Me.CtrlConductorAdicional3.TabIndex = 2
        Me.CtrlConductorAdicional3.ToolTipText = Nothing
        '
        'CtrlConductorAdicional4
        '
        Me.CtrlConductorAdicional4.Location = New System.Drawing.Point(0, 458)
        Me.CtrlConductorAdicional4.MensajeError = ""
        Me.CtrlConductorAdicional4.Name = "CtrlConductorAdicional4"
        Me.CtrlConductorAdicional4.Size = New System.Drawing.Size(526, 152)
        Me.CtrlConductorAdicional4.TabIndex = 3
        Me.CtrlConductorAdicional4.ToolTipText = Nothing
        '
        'ctrlMulticonductor
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.CtrlConductorAdicional4)
        Me.Controls.Add(Me.CtrlConductorAdicional3)
        Me.Controls.Add(Me.CtrlConductorAdicional2)
        Me.Controls.Add(Me.ctrlConductorAdicional1)
        Me.Name = "ctrlMulticonductor"
        Me.Size = New System.Drawing.Size(529, 615)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ctrlConductorAdicional1 As ctrlConductorAdicional
    Friend WithEvents CtrlConductorAdicional2 As GSAMVControlesP.ctrlConductorAdicional
    Friend WithEvents CtrlConductorAdicional3 As GSAMVControlesP.ctrlConductorAdicional
    Friend WithEvents CtrlConductorAdicional4 As GSAMVControlesP.ctrlConductorAdicional

End Class
