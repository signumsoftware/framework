<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmAdjuntarPagoFT
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmAdjuntarPagoFT))
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip
        Me.btnNuevoFT = New System.Windows.Forms.ToolStripButton
        Me.btnAdjuntarPagosFT = New System.Windows.Forms.ToolStripButton
        Me.btnNavegarFT = New System.Windows.Forms.ToolStripButton
        Me.lbFicherosTransferencias = New System.Windows.Forms.ListBox
        Me.dgvPagos = New System.Windows.Forms.DataGridView
        Me.Label1 = New System.Windows.Forms.Label
        Me.Label2 = New System.Windows.Forms.Label
        Me.btnRefrescarListaFT = New System.Windows.Forms.ToolStripButton
        Me.ToolStrip1.SuspendLayout()
        CType(Me.dgvPagos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'ToolStrip1
        '
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.btnNuevoFT, Me.btnAdjuntarPagosFT, Me.btnNavegarFT, Me.btnRefrescarListaFT})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 0)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Size = New System.Drawing.Size(594, 25)
        Me.ToolStrip1.TabIndex = 0
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'btnNuevoFT
        '
        Me.btnNuevoFT.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.btnNuevoFT.Image = Global.FN.GestionPagos.IU.My.Resources.Resources.document_new
        Me.btnNuevoFT.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnNuevoFT.Name = "btnNuevoFT"
        Me.btnNuevoFT.Size = New System.Drawing.Size(23, 22)
        Me.btnNuevoFT.Text = "Nuevo fichero de transferencias"
        '
        'btnAdjuntarPagosFT
        '
        Me.btnAdjuntarPagosFT.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.btnAdjuntarPagosFT.Image = Global.FN.GestionPagos.IU.My.Resources.Resources.document_into
        Me.btnAdjuntarPagosFT.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnAdjuntarPagosFT.Name = "btnAdjuntarPagosFT"
        Me.btnAdjuntarPagosFT.Size = New System.Drawing.Size(23, 22)
        Me.btnAdjuntarPagosFT.Text = "Adjuntar pagos al fichero de transferencias"
        '
        'btnNavegarFT
        '
        Me.btnNavegarFT.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.btnNavegarFT.Image = Global.FN.GestionPagos.IU.My.Resources.Resources.document_view
        Me.btnNavegarFT.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnNavegarFT.Name = "btnNavegarFT"
        Me.btnNavegarFT.Size = New System.Drawing.Size(23, 22)
        Me.btnNavegarFT.Text = "Navegar a fichero de transferencias"
        '
        'lbFicherosTransferencias
        '
        Me.lbFicherosTransferencias.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lbFicherosTransferencias.FormattingEnabled = True
        Me.lbFicherosTransferencias.Location = New System.Drawing.Point(12, 52)
        Me.lbFicherosTransferencias.Name = "lbFicherosTransferencias"
        Me.lbFicherosTransferencias.Size = New System.Drawing.Size(567, 147)
        Me.lbFicherosTransferencias.TabIndex = 1
        '
        'dgvPagos
        '
        Me.dgvPagos.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dgvPagos.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvPagos.Location = New System.Drawing.Point(12, 225)
        Me.dgvPagos.Name = "dgvPagos"
        Me.dgvPagos.Size = New System.Drawing.Size(567, 223)
        Me.dgvPagos.TabIndex = 2
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(13, 33)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(186, 13)
        Me.Label1.TabIndex = 3
        Me.Label1.Text = "Ficheros de transferencias disponibles"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(12, 206)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(108, 13)
        Me.Label2.TabIndex = 4
        Me.Label2.Text = "Pagos seleccionados"
        '
        'btnRefrescarListaFT
        '
        Me.btnRefrescarListaFT.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.btnRefrescarListaFT.Image = Global.FN.GestionPagos.IU.My.Resources.Resources.refresh
        Me.btnRefrescarListaFT.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnRefrescarListaFT.Name = "btnRefrescarListaFT"
        Me.btnRefrescarListaFT.Size = New System.Drawing.Size(23, 22)
        Me.btnRefrescarListaFT.ToolTipText = "Refrescar lista de ficheros"
        '
        'frmAdjuntarPagoFT
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(594, 460)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.dgvPagos)
        Me.Controls.Add(Me.lbFicherosTransferencias)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmAdjuntarPagoFT"
        Me.Text = "Adjuntar Pagos a fichero de transferencias"
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        CType(Me.dgvPagos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ToolStrip1 As System.Windows.Forms.ToolStrip
    Friend WithEvents btnNuevoFT As System.Windows.Forms.ToolStripButton
    Friend WithEvents btnAdjuntarPagosFT As System.Windows.Forms.ToolStripButton
    Friend WithEvents btnNavegarFT As System.Windows.Forms.ToolStripButton
    Friend WithEvents lbFicherosTransferencias As System.Windows.Forms.ListBox
    Friend WithEvents dgvPagos As System.Windows.Forms.DataGridView
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents btnRefrescarListaFT As System.Windows.Forms.ToolStripButton
End Class
