<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlFiltro
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ctrlFiltro))
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
        Me.Label1 = New System.Windows.Forms.Label
        Me.cmdEliminar = New System.Windows.Forms.Button
        Me.Label2 = New System.Windows.Forms.Label
        Me.cboCampo = New System.Windows.Forms.ComboBox
        Me.cboOperador = New System.Windows.Forms.ComboBox
        Me.cboValorInicial = New MotorBusquedaIuWinCtrl.ctrlValor
        Me.FlowLayoutPanel3 = New System.Windows.Forms.FlowLayoutPanel
        Me.cmdAgregar = New System.Windows.Forms.Button
        Me.lblValorInicial = New System.Windows.Forms.Label
        Me.cboOperaciones = New System.Windows.Forms.ComboBox
        Me.FlowLayoutPanel2 = New System.Windows.Forms.FlowLayoutPanel
        Me.cmdAgregarCondOperacion = New System.Windows.Forms.Button
        Me.Button1 = New System.Windows.Forms.Button
        Me.cmdEliminarTodos = New System.Windows.Forms.Button
        Me.cmdBuscar = New System.Windows.Forms.Button
        Me.DataGridView1 = New System.Windows.Forms.DataGridView
        Me.TableLayoutPanel3 = New System.Windows.Forms.TableLayoutPanel
        Me.lbxOperacionesPosibles = New System.Windows.Forms.ListBox
        Me.TableLayoutPanel4 = New System.Windows.Forms.TableLayoutPanel
        Me.Label3 = New System.Windows.Forms.Label
        Me.cboValorFinal = New MotorBusquedaIuWinCtrl.ctrlValor
        Me.TableLayoutPanel1.SuspendLayout()
        Me.FlowLayoutPanel3.SuspendLayout()
        Me.FlowLayoutPanel2.SuspendLayout()
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TableLayoutPanel3.SuspendLayout()
        Me.TableLayoutPanel4.SuspendLayout()
        Me.SuspendLayout()
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.ColumnCount = 4
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.23699!))
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 82.0!))
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 51.76849!))
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15.11254!))
        Me.TableLayoutPanel1.Controls.Add(Me.Label1, 0, 0)
        Me.TableLayoutPanel1.Controls.Add(Me.cmdEliminar, 3, 0)
        Me.TableLayoutPanel1.Controls.Add(Me.Label2, 1, 0)
        Me.TableLayoutPanel1.Controls.Add(Me.cboCampo, 0, 1)
        Me.TableLayoutPanel1.Controls.Add(Me.cboOperador, 1, 1)
        Me.TableLayoutPanel1.Controls.Add(Me.cboValorInicial, 2, 1)
        Me.TableLayoutPanel1.Controls.Add(Me.FlowLayoutPanel3, 3, 1)
        Me.TableLayoutPanel1.Controls.Add(Me.lblValorInicial, 2, 0)
        Me.TableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(176, 3)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 2
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(374, 49)
        Me.TableLayoutPanel1.TabIndex = 0
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(3, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(40, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Campo"
        '
        'cmdEliminar
        '
        Me.cmdEliminar.Dock = System.Windows.Forms.DockStyle.Left
        Me.cmdEliminar.Enabled = False
        Me.cmdEliminar.Image = CType(resources.GetObject("cmdEliminar.Image"), System.Drawing.Image)
        Me.cmdEliminar.Location = New System.Drawing.Point(331, 0)
        Me.cmdEliminar.Margin = New System.Windows.Forms.Padding(3, 0, 0, 0)
        Me.cmdEliminar.Name = "cmdEliminar"
        Me.cmdEliminar.Size = New System.Drawing.Size(37, 20)
        Me.cmdEliminar.TabIndex = 2
        Me.cmdEliminar.UseVisualStyleBackColor = True
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(99, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(51, 13)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Operador"
        '
        'cboCampo
        '
        Me.cboCampo.Dock = System.Windows.Forms.DockStyle.Fill
        Me.cboCampo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboCampo.FormattingEnabled = True
        Me.cboCampo.Location = New System.Drawing.Point(3, 23)
        Me.cboCampo.Name = "cboCampo"
        Me.cboCampo.Size = New System.Drawing.Size(90, 21)
        Me.cboCampo.TabIndex = 4
        '
        'cboOperador
        '
        Me.cboOperador.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboOperador.FormattingEnabled = True
        Me.cboOperador.Location = New System.Drawing.Point(99, 23)
        Me.cboOperador.Name = "cboOperador"
        Me.cboOperador.Size = New System.Drawing.Size(76, 21)
        Me.cboOperador.TabIndex = 5
        '
        'cboValorInicial
        '
        Me.cboValorInicial.Dock = System.Windows.Forms.DockStyle.Fill
        Me.cboValorInicial.Location = New System.Drawing.Point(181, 23)
        Me.cboValorInicial.MensajeError = ""
        Me.cboValorInicial.Name = "cboValorInicial"
        Me.cboValorInicial.Size = New System.Drawing.Size(144, 23)
        Me.cboValorInicial.TabIndex = 6
        Me.cboValorInicial.ToolTipText = Nothing
        '
        'FlowLayoutPanel3
        '
        Me.FlowLayoutPanel3.Controls.Add(Me.cmdAgregar)
        Me.FlowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill
        Me.FlowLayoutPanel3.Location = New System.Drawing.Point(328, 20)
        Me.FlowLayoutPanel3.Margin = New System.Windows.Forms.Padding(0)
        Me.FlowLayoutPanel3.Name = "FlowLayoutPanel3"
        Me.FlowLayoutPanel3.Size = New System.Drawing.Size(46, 29)
        Me.FlowLayoutPanel3.TabIndex = 12
        '
        'cmdAgregar
        '
        Me.cmdAgregar.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAgregar.Image = CType(resources.GetObject("cmdAgregar.Image"), System.Drawing.Image)
        Me.cmdAgregar.Location = New System.Drawing.Point(3, 3)
        Me.cmdAgregar.Name = "cmdAgregar"
        Me.cmdAgregar.Size = New System.Drawing.Size(37, 26)
        Me.cmdAgregar.TabIndex = 1
        Me.cmdAgregar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        Me.cmdAgregar.UseVisualStyleBackColor = True
        '
        'lblValorInicial
        '
        Me.lblValorInicial.AutoSize = True
        Me.lblValorInicial.Location = New System.Drawing.Point(181, 0)
        Me.lblValorInicial.Name = "lblValorInicial"
        Me.lblValorInicial.Size = New System.Drawing.Size(31, 13)
        Me.lblValorInicial.TabIndex = 2
        Me.lblValorInicial.Text = "Valor"
        '
        'cboOperaciones
        '
        Me.cboOperaciones.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cboOperaciones.FormattingEnabled = True
        Me.cboOperaciones.Location = New System.Drawing.Point(3, 23)
        Me.cboOperaciones.Name = "cboOperaciones"
        Me.cboOperaciones.Size = New System.Drawing.Size(116, 21)
        Me.cboOperaciones.TabIndex = 9
        '
        'FlowLayoutPanel2
        '
        Me.FlowLayoutPanel2.Controls.Add(Me.cmdAgregarCondOperacion)
        Me.FlowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill
        Me.FlowLayoutPanel2.Location = New System.Drawing.Point(122, 20)
        Me.FlowLayoutPanel2.Margin = New System.Windows.Forms.Padding(0)
        Me.FlowLayoutPanel2.Name = "FlowLayoutPanel2"
        Me.FlowLayoutPanel2.Size = New System.Drawing.Size(45, 29)
        Me.FlowLayoutPanel2.TabIndex = 11
        '
        'cmdAgregarCondOperacion
        '
        Me.cmdAgregarCondOperacion.Image = CType(resources.GetObject("cmdAgregarCondOperacion.Image"), System.Drawing.Image)
        Me.cmdAgregarCondOperacion.Location = New System.Drawing.Point(3, 3)
        Me.cmdAgregarCondOperacion.Name = "cmdAgregarCondOperacion"
        Me.cmdAgregarCondOperacion.Size = New System.Drawing.Size(37, 25)
        Me.cmdAgregarCondOperacion.TabIndex = 10
        Me.cmdAgregarCondOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        Me.cmdAgregarCondOperacion.UseVisualStyleBackColor = True
        '
        'Button1
        '
        Me.Button1.Dock = System.Windows.Forms.DockStyle.Left
        Me.Button1.Enabled = False
        Me.Button1.Image = CType(resources.GetObject("Button1.Image"), System.Drawing.Image)
        Me.Button1.Location = New System.Drawing.Point(125, 0)
        Me.Button1.Margin = New System.Windows.Forms.Padding(3, 0, 0, 0)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(37, 20)
        Me.Button1.TabIndex = 11
        Me.Button1.UseVisualStyleBackColor = True
        '
        'cmdEliminarTodos
        '
        Me.cmdEliminarTodos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.cmdEliminarTodos.Image = CType(resources.GetObject("cmdEliminarTodos.Image"), System.Drawing.Image)
        Me.cmdEliminarTodos.Location = New System.Drawing.Point(556, 3)
        Me.cmdEliminarTodos.Name = "cmdEliminarTodos"
        Me.cmdEliminarTodos.Size = New System.Drawing.Size(54, 49)
        Me.cmdEliminarTodos.TabIndex = 3
        Me.cmdEliminarTodos.UseVisualStyleBackColor = True
        '
        'cmdBuscar
        '
        Me.cmdBuscar.Dock = System.Windows.Forms.DockStyle.Fill
        Me.cmdBuscar.Image = CType(resources.GetObject("cmdBuscar.Image"), System.Drawing.Image)
        Me.cmdBuscar.Location = New System.Drawing.Point(556, 58)
        Me.cmdBuscar.Name = "cmdBuscar"
        Me.cmdBuscar.Size = New System.Drawing.Size(54, 208)
        Me.cmdBuscar.TabIndex = 4
        Me.cmdBuscar.UseVisualStyleBackColor = True
        '
        'DataGridView1
        '
        Me.DataGridView1.AllowUserToAddRows = False
        Me.DataGridView1.AllowUserToDeleteRows = False
        Me.DataGridView1.AllowUserToOrderColumns = True
        DataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer))
        Me.DataGridView1.AlternatingRowsDefaultCellStyle = DataGridViewCellStyle1
        Me.DataGridView1.BackgroundColor = System.Drawing.Color.White
        Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridView1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.DataGridView1.Location = New System.Drawing.Point(176, 58)
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.ReadOnly = True
        Me.DataGridView1.RowHeadersVisible = False
        Me.DataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.DataGridView1.Size = New System.Drawing.Size(374, 208)
        Me.DataGridView1.TabIndex = 5
        '
        'TableLayoutPanel3
        '
        Me.TableLayoutPanel3.ColumnCount = 3
        Me.TableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 31.32137!))
        Me.TableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 68.67863!))
        Me.TableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 59.0!))
        Me.TableLayoutPanel3.Controls.Add(Me.cmdEliminarTodos, 2, 0)
        Me.TableLayoutPanel3.Controls.Add(Me.cmdBuscar, 2, 1)
        Me.TableLayoutPanel3.Controls.Add(Me.DataGridView1, 1, 1)
        Me.TableLayoutPanel3.Controls.Add(Me.TableLayoutPanel1, 1, 0)
        Me.TableLayoutPanel3.Controls.Add(Me.lbxOperacionesPosibles, 0, 1)
        Me.TableLayoutPanel3.Controls.Add(Me.TableLayoutPanel4, 0, 0)
        Me.TableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel3.Location = New System.Drawing.Point(0, 0)
        Me.TableLayoutPanel3.Name = "TableLayoutPanel3"
        Me.TableLayoutPanel3.RowCount = 2
        Me.TableLayoutPanel3.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 55.0!))
        Me.TableLayoutPanel3.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel3.Size = New System.Drawing.Size(613, 269)
        Me.TableLayoutPanel3.TabIndex = 8
        '
        'lbxOperacionesPosibles
        '
        Me.lbxOperacionesPosibles.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lbxOperacionesPosibles.FormattingEnabled = True
        Me.lbxOperacionesPosibles.Location = New System.Drawing.Point(1, 56)
        Me.lbxOperacionesPosibles.Margin = New System.Windows.Forms.Padding(1)
        Me.lbxOperacionesPosibles.Name = "lbxOperacionesPosibles"
        Me.lbxOperacionesPosibles.Size = New System.Drawing.Size(171, 212)
        Me.lbxOperacionesPosibles.TabIndex = 6
        '
        'TableLayoutPanel4
        '
        Me.TableLayoutPanel4.ColumnCount = 2
        Me.TableLayoutPanel4.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 73.29546!))
        Me.TableLayoutPanel4.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 26.70455!))
        Me.TableLayoutPanel4.Controls.Add(Me.Label3, 0, 0)
        Me.TableLayoutPanel4.Controls.Add(Me.cboOperaciones, 0, 1)
        Me.TableLayoutPanel4.Controls.Add(Me.FlowLayoutPanel2, 1, 1)
        Me.TableLayoutPanel4.Controls.Add(Me.Button1, 1, 0)
        Me.TableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel4.Location = New System.Drawing.Point(3, 3)
        Me.TableLayoutPanel4.Name = "TableLayoutPanel4"
        Me.TableLayoutPanel4.RowCount = 2
        Me.TableLayoutPanel4.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel4.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel4.Size = New System.Drawing.Size(167, 49)
        Me.TableLayoutPanel4.TabIndex = 7
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(3, 0)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(67, 13)
        Me.Label3.TabIndex = 12
        Me.Label3.Text = "Operaciones"
        '
        'cboValorFinal
        '
        Me.cboValorFinal.Location = New System.Drawing.Point(395, 252)
        Me.cboValorFinal.MensajeError = ""
        Me.cboValorFinal.Name = "cboValorFinal"
        Me.cboValorFinal.Size = New System.Drawing.Size(42, 14)
        Me.cboValorFinal.TabIndex = 8
        Me.cboValorFinal.ToolTipText = Nothing
        '
        'ctrlFiltro
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.TableLayoutPanel3)
        Me.Controls.Add(Me.cboValorFinal)
        Me.Name = "ctrlFiltro"
        Me.Size = New System.Drawing.Size(613, 269)
        Me.TableLayoutPanel1.ResumeLayout(False)
        Me.TableLayoutPanel1.PerformLayout()
        Me.FlowLayoutPanel3.ResumeLayout(False)
        Me.FlowLayoutPanel2.ResumeLayout(False)
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TableLayoutPanel3.ResumeLayout(False)
        Me.TableLayoutPanel4.ResumeLayout(False)
        Me.TableLayoutPanel4.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents lblValorInicial As System.Windows.Forms.Label
    Friend WithEvents cboCampo As System.Windows.Forms.ComboBox
    Friend WithEvents cboOperador As System.Windows.Forms.ComboBox
    Friend WithEvents cboValorInicial As ctrlValor
    Friend WithEvents cboValorFinal As ctrlValor
    Friend WithEvents cmdAgregar As System.Windows.Forms.Button
    Friend WithEvents cmdEliminar As System.Windows.Forms.Button
    Friend WithEvents cmdEliminarTodos As System.Windows.Forms.Button
    Friend WithEvents cmdBuscar As System.Windows.Forms.Button
    Friend WithEvents DataGridView1 As System.Windows.Forms.DataGridView
    Friend WithEvents cboOperaciones As System.Windows.Forms.ComboBox
    Friend WithEvents cmdAgregarCondOperacion As System.Windows.Forms.Button
    Friend WithEvents TableLayoutPanel3 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents lbxOperacionesPosibles As System.Windows.Forms.ListBox
    Friend WithEvents FlowLayoutPanel2 As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents FlowLayoutPanel3 As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents TableLayoutPanel4 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents Label3 As System.Windows.Forms.Label

End Class
