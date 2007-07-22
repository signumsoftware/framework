<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

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
        Dim RecuperadorMapeadoXFicheroXMLAD2 As MV2DN.RecuperadorMapeadoXFicheroXMLAD = New MV2DN.RecuperadorMapeadoXFicheroXMLAD
        Me.btnVincularTipo = New System.Windows.Forms.Button
        Me.Button1 = New System.Windows.Forms.Button
        Me.Button2 = New System.Windows.Forms.Button
        Me.Button3 = New System.Windows.Forms.Button
        Me.TextBox1 = New System.Windows.Forms.TextBox
        Me.Label1 = New System.Windows.Forms.Label
        Me.CtrlArbolNavD1 = New MNavegacionDatosIUWin.ctrlArbolNavD
        Me.Button4 = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'btnVincularTipo
        '
        Me.btnVincularTipo.Location = New System.Drawing.Point(126, 310)
        Me.btnVincularTipo.Name = "btnVincularTipo"
        Me.btnVincularTipo.Size = New System.Drawing.Size(127, 23)
        Me.btnVincularTipo.TabIndex = 1
        Me.btnVincularTipo.Text = "VincularTipo"
        Me.btnVincularTipo.UseVisualStyleBackColor = True
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(765, 294)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(139, 23)
        Me.Button1.TabIndex = 2
        Me.Button1.Text = "Cargar tipo seleccioando"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(374, 310)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(106, 23)
        Me.Button2.TabIndex = 3
        Me.Button2.Text = "Vincular persona"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Button3
        '
        Me.Button3.Location = New System.Drawing.Point(12, 310)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(96, 23)
        Me.Button3.TabIndex = 4
        Me.Button3.Text = "CrearTablas"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'TextBox1
        '
        Me.TextBox1.Location = New System.Drawing.Point(507, 312)
        Me.TextBox1.Name = "TextBox1"
        Me.TextBox1.Size = New System.Drawing.Size(46, 20)
        Me.TextBox1.TabIndex = 5
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(486, 315)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(15, 13)
        Me.Label1.TabIndex = 6
        Me.Label1.Text = "id"
        '
        'CtrlArbolNavD1
        '
        Me.CtrlArbolNavD1.Location = New System.Drawing.Point(12, 12)
        Me.CtrlArbolNavD1.Name = "CtrlArbolNavD1"
        RecuperadorMapeadoXFicheroXMLAD2.RutaDirectorioMapeados = Nothing
        Me.CtrlArbolNavD1.RecuperadorMap = RecuperadorMapeadoXFicheroXMLAD2
        Me.CtrlArbolNavD1.Size = New System.Drawing.Size(892, 276)
        Me.CtrlArbolNavD1.TabIndex = 0
        '
        'Button4
        '
        Me.Button4.Location = New System.Drawing.Point(126, 339)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(127, 23)
        Me.Button4.TabIndex = 7
        Me.Button4.Text = "vinc campo interface"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(913, 390)
        Me.Controls.Add(Me.Button4)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.TextBox1)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.btnVincularTipo)
        Me.Controls.Add(Me.CtrlArbolNavD1)
        Me.Name = "Form1"
        Me.Text = "Form1"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents CtrlArbolNavD1 As MNavegacionDatosIUWin.ctrlArbolNavD
    Friend WithEvents btnVincularTipo As System.Windows.Forms.Button
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents TextBox1 As System.Windows.Forms.TextBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Button4 As System.Windows.Forms.Button

End Class
