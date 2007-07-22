<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MarcoMDI
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MarcoMDI))
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip
        Me.tsbFInicio = New System.Windows.Forms.ToolStripButton
        Me.tsbFBusqueda = New System.Windows.Forms.ToolStripButton
        Me.ToolStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'ToolStrip1
        '
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.tsbFInicio, Me.tsbFBusqueda})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 0)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Size = New System.Drawing.Size(697, 25)
        Me.ToolStrip1.TabIndex = 1
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'tsbFInicio
        '
        Me.tsbFInicio.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.tsbFInicio.Image = CType(resources.GetObject("tsbFInicio.Image"), System.Drawing.Image)
        Me.tsbFInicio.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.tsbFInicio.Name = "tsbFInicio"
        Me.tsbFInicio.Size = New System.Drawing.Size(23, 22)
        Me.tsbFInicio.Text = "fInicio"
        '
        'tsbFBusqueda
        '
        Me.tsbFBusqueda.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.tsbFBusqueda.Image = CType(resources.GetObject("tsbFBusqueda.Image"), System.Drawing.Image)
        Me.tsbFBusqueda.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.tsbFBusqueda.Name = "tsbFBusqueda"
        Me.tsbFBusqueda.Size = New System.Drawing.Size(23, 22)
        Me.tsbFBusqueda.Text = "fBusqueda"
        '
        'MarcoMDI
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(697, 441)
        Me.Controls.Add(Me.ToolStrip1)
        Me.IsMdiContainer = True
        Me.Name = "MarcoMDI"
        Me.Text = "MarcoMDI"
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ToolStrip1 As System.Windows.Forms.ToolStrip
    Friend WithEvents tsbFInicio As System.Windows.Forms.ToolStripButton
    Friend WithEvents tsbFBusqueda As System.Windows.Forms.ToolStripButton
End Class
