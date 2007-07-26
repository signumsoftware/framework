<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmFiltro
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmFiltro))
        Me.ctrlBuscarGenerico = New MotorBusquedaIuWinCtrl.ctrlBuscadorGenerico2
        Me.SuspendLayout()
        '
        'ctrlBuscarGenerico
        '
        Me.ctrlBuscarGenerico.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ctrlBuscarGenerico.Location = New System.Drawing.Point(0, 0)
        Me.ctrlBuscarGenerico.MensajeError = ""
        Me.ctrlBuscarGenerico.MultiSelect = True
        Me.ctrlBuscarGenerico.Name = "ctrlBuscarGenerico"
        Me.ctrlBuscarGenerico.Size = New System.Drawing.Size(742, 481)
        Me.ctrlBuscarGenerico.TabIndex = 0
        Me.ctrlBuscarGenerico.TipoNavegacion = Framework.IU.IUComun.TipoNavegacion.Normal
        Me.ctrlBuscarGenerico.ToolTipText = Nothing
        '
        'frmFiltro
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(742, 481)
        Me.Controls.Add(Me.ctrlBuscarGenerico)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MinimumSize = New System.Drawing.Size(705, 408)
        Me.Name = "frmFiltro"
        Me.Text = "Búsqueda con Filtro"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ctrlBuscarGenerico As MotorBusquedaIuWinCtrl.ctrlBuscadorGenerico2
End Class
