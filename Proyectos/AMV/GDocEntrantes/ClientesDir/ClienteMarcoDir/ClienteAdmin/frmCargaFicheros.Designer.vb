<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmCargaPagos
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmCargaPagos))
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog
        Me.DataGridView1 = New System.Windows.Forms.DataGridView
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip
        Me.ToolStripButton1 = New System.Windows.Forms.ToolStripButton
        Me.ToolStripButton2 = New System.Windows.Forms.ToolStripButton
        Me.ToolStripRefrescar = New System.Windows.Forms.ToolStripButton
        Me.Label1 = New System.Windows.Forms.Label
        Me.Label2 = New System.Windows.Forms.Label
        Me.Label3 = New System.Windows.Forms.Label
        Me.cboTipoOrigen = New System.Windows.Forms.ComboBox
        Me.cboOperacion = New System.Windows.Forms.ComboBox
        Me.txtFicheroCarga = New System.Windows.Forms.TextBox
        Me.cmdExaminarFichero = New System.Windows.Forms.Button
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.ToolStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'DataGridView1
        '
        Me.DataGridView1.AllowUserToAddRows = False
        Me.DataGridView1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridView1.Location = New System.Drawing.Point(0, 81)
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.Size = New System.Drawing.Size(735, 366)
        Me.DataGridView1.TabIndex = 5
        '
        'ToolStrip1
        '
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripButton1, Me.ToolStripButton2, Me.ToolStripRefrescar})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 0)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Size = New System.Drawing.Size(735, 25)
        Me.ToolStrip1.TabIndex = 0
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'ToolStripButton1
        '
        Me.ToolStripButton1.Image = CType(resources.GetObject("ToolStripButton1.Image"), System.Drawing.Image)
        Me.ToolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripButton1.Name = "ToolStripButton1"
        Me.ToolStripButton1.Size = New System.Drawing.Size(124, 22)
        Me.ToolStripButton1.Text = "Cargar Proveedores"
        '
        'ToolStripButton2
        '
        Me.ToolStripButton2.Image = CType(resources.GetObject("ToolStripButton2.Image"), System.Drawing.Image)
        Me.ToolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripButton2.Name = "ToolStripButton2"
        Me.ToolStripButton2.Size = New System.Drawing.Size(92, 22)
        Me.ToolStripButton2.Text = "Cargar pagos"
        '
        'ToolStripRefrescar
        '
        Me.ToolStripRefrescar.Image = CType(resources.GetObject("ToolStripRefrescar.Image"), System.Drawing.Image)
        Me.ToolStripRefrescar.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripRefrescar.Name = "ToolStripRefrescar"
        Me.ToolStripRefrescar.Size = New System.Drawing.Size(74, 22)
        Me.ToolStripRefrescar.Text = "Refrescar"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(3, 57)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(75, 13)
        Me.Label1.TabIndex = 7
        Me.Label1.Text = "Tipo de origen"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(385, 57)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(56, 13)
        Me.Label2.TabIndex = 8
        Me.Label2.Text = "Operación"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(36, 31)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(42, 13)
        Me.Label3.TabIndex = 6
        Me.Label3.Text = "Fichero"
        '
        'cboTipoOrigen
        '
        Me.cboTipoOrigen.FormattingEnabled = True
        Me.cboTipoOrigen.Location = New System.Drawing.Point(84, 54)
        Me.cboTipoOrigen.Name = "cboTipoOrigen"
        Me.cboTipoOrigen.Size = New System.Drawing.Size(264, 21)
        Me.cboTipoOrigen.TabIndex = 3
        '
        'cboOperacion
        '
        Me.cboOperacion.FormattingEnabled = True
        Me.cboOperacion.Location = New System.Drawing.Point(447, 54)
        Me.cboOperacion.Name = "cboOperacion"
        Me.cboOperacion.Size = New System.Drawing.Size(264, 21)
        Me.cboOperacion.TabIndex = 4
        '
        'txtFicheroCarga
        '
        Me.txtFicheroCarga.Location = New System.Drawing.Point(84, 28)
        Me.txtFicheroCarga.Name = "txtFicheroCarga"
        Me.txtFicheroCarga.Size = New System.Drawing.Size(534, 20)
        Me.txtFicheroCarga.TabIndex = 1
        '
        'cmdExaminarFichero
        '
        Me.cmdExaminarFichero.Location = New System.Drawing.Point(636, 26)
        Me.cmdExaminarFichero.Name = "cmdExaminarFichero"
        Me.cmdExaminarFichero.Size = New System.Drawing.Size(75, 23)
        Me.cmdExaminarFichero.TabIndex = 2
        Me.cmdExaminarFichero.Text = "Examinar"
        Me.cmdExaminarFichero.UseVisualStyleBackColor = True
        '
        'frmCargaPagos
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(735, 447)
        Me.Controls.Add(Me.cmdExaminarFichero)
        Me.Controls.Add(Me.txtFicheroCarga)
        Me.Controls.Add(Me.cboOperacion)
        Me.Controls.Add(Me.cboTipoOrigen)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Controls.Add(Me.DataGridView1)
        Me.Name = "frmCargaPagos"
        Me.Text = "Importación Pagos"
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents OpenFileDialog1 As System.Windows.Forms.OpenFileDialog
    Friend WithEvents DataGridView1 As System.Windows.Forms.DataGridView
    Friend WithEvents ToolStrip1 As System.Windows.Forms.ToolStrip
    Friend WithEvents ToolStripButton1 As System.Windows.Forms.ToolStripButton
    Friend WithEvents ToolStripButton2 As System.Windows.Forms.ToolStripButton
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents cboTipoOrigen As System.Windows.Forms.ComboBox
    Friend WithEvents cboOperacion As System.Windows.Forms.ComboBox
    Friend WithEvents txtFicheroCarga As System.Windows.Forms.TextBox
    Friend WithEvents cmdExaminarFichero As System.Windows.Forms.Button
    Friend WithEvents ToolStripRefrescar As System.Windows.Forms.ToolStripButton
End Class
