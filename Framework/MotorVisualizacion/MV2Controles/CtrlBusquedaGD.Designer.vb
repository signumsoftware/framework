<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class CtrlBusquedaGD
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
        Me.ctrlBuscadorGenerico = New MotorBusquedaIuWinCtrl.ctrlBuscadorGenerico2
        Me.SuspendLayout()
        '
        'ctrlBuscadorGenerico
        '
        Me.ctrlBuscadorGenerico.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ctrlBuscadorGenerico.FiltroVisible = False
        Me.ctrlBuscadorGenerico.Location = New System.Drawing.Point(0, 0)
        Me.ctrlBuscadorGenerico.MensajeError = ""
        Me.ctrlBuscadorGenerico.MultiSelect = True
        Me.ctrlBuscadorGenerico.Name = "ctrlBuscadorGenerico"
        Me.ctrlBuscadorGenerico.Navegable = False
        Me.ctrlBuscadorGenerico.Size = New System.Drawing.Size(397, 248)
        Me.ctrlBuscadorGenerico.TabIndex = 0
        Me.ctrlBuscadorGenerico.TipoNavegacion = Framework.IU.IUComun.TipoNavegacion.Normal
        Me.ctrlBuscadorGenerico.ToolTipText = Nothing
        '
        'CtrlBusquedaGD
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.ctrlBuscadorGenerico)
        Me.Name = "CtrlBusquedaGD"
        Me.Size = New System.Drawing.Size(397, 248)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ctrlBuscadorGenerico As MotorBusquedaIuWinCtrl.ctrlBuscadorGenerico2
End Class
