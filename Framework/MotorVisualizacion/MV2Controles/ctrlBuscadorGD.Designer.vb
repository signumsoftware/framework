<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlBuscadorGD
    Inherits System.Windows.Forms.UserControl

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
        Me.ctrlBuscadorGenerico21 = New MotorBusquedaIuWinCtrl.ctrlBuscadorGenerico2
        Me.SuspendLayout()
        '
        'ctrlBuscadorGenerico21
        '
        Me.ctrlBuscadorGenerico21.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ctrlBuscadorGenerico21.FiltroVisible = False
        Me.ctrlBuscadorGenerico21.Location = New System.Drawing.Point(0, 0)
        Me.ctrlBuscadorGenerico21.MensajeError = ""
        Me.ctrlBuscadorGenerico21.MultiSelect = True
        Me.ctrlBuscadorGenerico21.Name = "ctrlBuscadorGenerico21"
        Me.ctrlBuscadorGenerico21.Navegable = False
        Me.ctrlBuscadorGenerico21.Size = New System.Drawing.Size(904, 429)
        Me.ctrlBuscadorGenerico21.TabIndex = 1
        Me.ctrlBuscadorGenerico21.TipoNavegacion = Framework.IU.IUComun.TipoNavegacion.Normal
        Me.ctrlBuscadorGenerico21.ToolTipText = Nothing
        '
        'ctrlBuscadorGD
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.ctrlBuscadorGenerico21)
        Me.Name = "ctrlBuscadorGD"
        Me.Size = New System.Drawing.Size(904, 429)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ctrlBuscadorGenerico21 As MotorBusquedaIuWinCtrl.ctrlBuscadorGenerico2

End Class
