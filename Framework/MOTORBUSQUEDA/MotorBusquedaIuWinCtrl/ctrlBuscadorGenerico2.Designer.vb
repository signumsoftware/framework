<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlBuscadorGenerico2
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
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer
        Me.ctrlFiltro = New MotorBusquedaIuWinCtrl.ctrlFiltro
        Me.DataGridViewXT1 = New ControlesPGenericos.DataGridViewXT
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        Me.SuspendLayout()
        '
        'SplitContainer1
        '
        Me.SplitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.SplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitContainer1.Location = New System.Drawing.Point(0, 0)
        Me.SplitContainer1.Name = "SplitContainer1"
        Me.SplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal
        '
        'SplitContainer1.Panel1
        '
        Me.SplitContainer1.Panel1.Controls.Add(Me.ctrlFiltro)
        '
        'SplitContainer1.Panel2
        '
        Me.SplitContainer1.Panel2.Controls.Add(Me.DataGridViewXT1)
        Me.SplitContainer1.Size = New System.Drawing.Size(767, 588)
        Me.SplitContainer1.SplitterDistance = 216
        Me.SplitContainer1.TabIndex = 1
        '
        'ctrlFiltro
        '
        Me.ctrlFiltro.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ctrlFiltro.Location = New System.Drawing.Point(0, 0)
        Me.ctrlFiltro.MensajeError = "No se ha definido ningún filtro"
        Me.ctrlFiltro.Name = "ctrlFiltro"
        Me.ctrlFiltro.Size = New System.Drawing.Size(765, 214)
        Me.ctrlFiltro.TabIndex = 0
        Me.ctrlFiltro.ToolTipText = Nothing
        '
        'DataGridViewXT1
        '
        Me.DataGridViewXT1.Agregable = False
        Me.DataGridViewXT1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridViewXT1.Eliminable = False
        Me.DataGridViewXT1.Filtrable = False
        Me.DataGridViewXT1.Location = New System.Drawing.Point(3, 3)
        Me.DataGridViewXT1.MensajeError = ""
        Me.DataGridViewXT1.Name = "DataGridViewXT1"
        Me.DataGridViewXT1.Size = New System.Drawing.Size(759, 360)
        Me.DataGridViewXT1.TabIndex = 0
        Me.DataGridViewXT1.TituloListado = "Listado"
        Me.DataGridViewXT1.ToolTipText = Nothing
        '
        'ctrlBuscadorGenerico2
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.SplitContainer1)
        Me.Name = "ctrlBuscadorGenerico2"
        Me.Size = New System.Drawing.Size(767, 588)
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        Me.SplitContainer1.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents DataGridViewXT1 As ControlesPGenericos.DataGridViewXT
    Friend WithEvents ctrlFiltro As ctrlFiltro
    Friend WithEvents SplitContainer1 As System.Windows.Forms.SplitContainer

End Class
