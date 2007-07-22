<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlContructorFiltro
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
        Me.components = New System.ComponentModel.Container
        Me.DataGridView1 = New System.Windows.Forms.DataGridView
        Me.capo = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.OperadoresArictmetico = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.ValorFinal = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.cboCampos = New System.Windows.Forms.ComboBox
        Me.cboOperadores = New System.Windows.Forms.ComboBox
        Me.cboValoresI = New System.Windows.Forms.ComboBox
        Me.cboValoresF = New System.Windows.Forms.ComboBox
        Me.cmdAgregar = New System.Windows.Forms.Button
        Me.Button2 = New System.Windows.Forms.Button
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
        Me.TableLayoutPanel3 = New System.Windows.Forms.TableLayoutPanel
        Me.TableLayoutPanel2 = New System.Windows.Forms.TableLayoutPanel
        Me.Label4 = New System.Windows.Forms.Label
        Me.Label3 = New System.Windows.Forms.Label
        Me.Label2 = New System.Windows.Forms.Label
        Me.Label1 = New System.Windows.Forms.Label
        Me.TableLayoutPanel4 = New System.Windows.Forms.TableLayoutPanel
        Me.Button3 = New System.Windows.Forms.Button
        Me.Button4 = New System.Windows.Forms.Button
        Me.ValorInicialDataGridViewTextBoxColumn = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.CondicionDNBindingSource = New System.Windows.Forms.BindingSource(Me.components)
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TableLayoutPanel1.SuspendLayout()
        Me.TableLayoutPanel3.SuspendLayout()
        Me.TableLayoutPanel2.SuspendLayout()
        Me.TableLayoutPanel4.SuspendLayout()
        CType(Me.CondicionDNBindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'DataGridView1
        '
        Me.DataGridView1.AllowUserToAddRows = False
        Me.DataGridView1.AllowUserToOrderColumns = True
        Me.DataGridView1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridView1.AutoGenerateColumns = False
        Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridView1.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.capo, Me.OperadoresArictmetico, Me.ValorFinal, Me.ValorInicialDataGridViewTextBoxColumn})
        Me.DataGridView1.DataSource = Me.CondicionDNBindingSource
        Me.DataGridView1.Location = New System.Drawing.Point(3, 43)
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.Size = New System.Drawing.Size(639, 223)
        Me.DataGridView1.TabIndex = 0
        '
        'capo
        '
        Me.capo.DataPropertyName = "capo"
        Me.capo.HeaderText = "capo"
        Me.capo.Name = "capo"
        '
        'OperadoresArictmetico
        '
        Me.OperadoresArictmetico.DataPropertyName = "OperadoresArictmetico"
        Me.OperadoresArictmetico.HeaderText = "OperadoresArictmetico"
        Me.OperadoresArictmetico.Name = "OperadoresArictmetico"
        '
        'ValorFinal
        '
        Me.ValorFinal.DataPropertyName = "ValorFinal"
        Me.ValorFinal.HeaderText = "ValorFinal"
        Me.ValorFinal.Name = "ValorFinal"
        '
        'cboCampos
        '
        Me.cboCampos.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cboCampos.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboCampos.FormattingEnabled = True
        Me.cboCampos.Location = New System.Drawing.Point(3, 15)
        Me.cboCampos.Margin = New System.Windows.Forms.Padding(3, 0, 3, 0)
        Me.cboCampos.Name = "cboCampos"
        Me.cboCampos.Size = New System.Drawing.Size(112, 21)
        Me.cboCampos.TabIndex = 1
        '
        'cboOperadores
        '
        Me.cboOperadores.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cboOperadores.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboOperadores.FormattingEnabled = True
        Me.cboOperadores.Location = New System.Drawing.Point(121, 15)
        Me.cboOperadores.Margin = New System.Windows.Forms.Padding(3, 0, 3, 0)
        Me.cboOperadores.Name = "cboOperadores"
        Me.cboOperadores.Size = New System.Drawing.Size(65, 21)
        Me.cboOperadores.TabIndex = 2
        '
        'cboValoresI
        '
        Me.cboValoresI.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cboValoresI.FormattingEnabled = True
        Me.cboValoresI.Location = New System.Drawing.Point(192, 15)
        Me.cboValoresI.Margin = New System.Windows.Forms.Padding(3, 0, 3, 0)
        Me.cboValoresI.Name = "cboValoresI"
        Me.cboValoresI.Size = New System.Drawing.Size(160, 21)
        Me.cboValoresI.TabIndex = 3
        '
        'cboValoresF
        '
        Me.cboValoresF.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cboValoresF.FormattingEnabled = True
        Me.cboValoresF.Location = New System.Drawing.Point(358, 15)
        Me.cboValoresF.Margin = New System.Windows.Forms.Padding(3, 0, 3, 0)
        Me.cboValoresF.Name = "cboValoresF"
        Me.cboValoresF.Size = New System.Drawing.Size(143, 21)
        Me.cboValoresF.TabIndex = 4
        '
        'cmdAgregar
        '
        Me.cmdAgregar.Anchor = System.Windows.Forms.AnchorStyles.None
        Me.cmdAgregar.ImageKey = "element_add.ico"
        Me.cmdAgregar.Location = New System.Drawing.Point(0, 7)
        Me.cmdAgregar.Margin = New System.Windows.Forms.Padding(0)
        Me.cmdAgregar.Name = "cmdAgregar"
        Me.cmdAgregar.Size = New System.Drawing.Size(37, 25)
        Me.cmdAgregar.TabIndex = 5
        Me.cmdAgregar.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(37, 0)
        Me.Button2.Margin = New System.Windows.Forms.Padding(0)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(37, 25)
        Me.Button2.TabIndex = 6
        Me.Button2.Text = "-"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.ColumnCount = 1
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.Controls.Add(Me.DataGridView1, 0, 1)
        Me.TableLayoutPanel1.Controls.Add(Me.TableLayoutPanel3, 0, 0)
        Me.TableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(0, 0)
        Me.TableLayoutPanel1.Margin = New System.Windows.Forms.Padding(0)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 2
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40.0!))
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(645, 269)
        Me.TableLayoutPanel1.TabIndex = 7
        '
        'TableLayoutPanel3
        '
        Me.TableLayoutPanel3.ColumnCount = 2
        Me.TableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 78.29457!))
        Me.TableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 21.70543!))
        Me.TableLayoutPanel3.Controls.Add(Me.TableLayoutPanel2, 0, 0)
        Me.TableLayoutPanel3.Controls.Add(Me.TableLayoutPanel4, 1, 0)
        Me.TableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel3.Location = New System.Drawing.Point(0, 0)
        Me.TableLayoutPanel3.Margin = New System.Windows.Forms.Padding(0)
        Me.TableLayoutPanel3.Name = "TableLayoutPanel3"
        Me.TableLayoutPanel3.RowCount = 1
        Me.TableLayoutPanel3.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel3.Size = New System.Drawing.Size(645, 40)
        Me.TableLayoutPanel3.TabIndex = 2
        '
        'TableLayoutPanel2
        '
        Me.TableLayoutPanel2.ColumnCount = 4
        Me.TableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 62.45353!))
        Me.TableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 37.54647!))
        Me.TableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 166.0!))
        Me.TableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 148.0!))
        Me.TableLayoutPanel2.Controls.Add(Me.Label4, 3, 0)
        Me.TableLayoutPanel2.Controls.Add(Me.Label3, 2, 0)
        Me.TableLayoutPanel2.Controls.Add(Me.Label2, 1, 0)
        Me.TableLayoutPanel2.Controls.Add(Me.cboCampos, 0, 1)
        Me.TableLayoutPanel2.Controls.Add(Me.cboOperadores, 1, 1)
        Me.TableLayoutPanel2.Controls.Add(Me.cboValoresI, 2, 1)
        Me.TableLayoutPanel2.Controls.Add(Me.cboValoresF, 3, 1)
        Me.TableLayoutPanel2.Controls.Add(Me.Label1, 0, 0)
        Me.TableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel2.Location = New System.Drawing.Point(0, 0)
        Me.TableLayoutPanel2.Margin = New System.Windows.Forms.Padding(0)
        Me.TableLayoutPanel2.Name = "TableLayoutPanel2"
        Me.TableLayoutPanel2.RowCount = 2
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 15.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.TableLayoutPanel2.Size = New System.Drawing.Size(504, 40)
        Me.TableLayoutPanel2.TabIndex = 1
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(358, 0)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(56, 13)
        Me.Label4.TabIndex = 8
        Me.Label4.Text = "Valor Final"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(192, 0)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(61, 13)
        Me.Label3.TabIndex = 7
        Me.Label3.Text = "Valor Inicial"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(121, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(51, 13)
        Me.Label2.TabIndex = 6
        Me.Label2.Text = "Operador"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(3, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(73, 13)
        Me.Label1.TabIndex = 5
        Me.Label1.Text = "Característica"
        '
        'TableLayoutPanel4
        '
        Me.TableLayoutPanel4.ColumnCount = 4
        Me.TableLayoutPanel4.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
        Me.TableLayoutPanel4.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
        Me.TableLayoutPanel4.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
        Me.TableLayoutPanel4.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
        Me.TableLayoutPanel4.Controls.Add(Me.Button2, 1, 0)
        Me.TableLayoutPanel4.Controls.Add(Me.Button3, 2, 0)
        Me.TableLayoutPanel4.Controls.Add(Me.Button4, 3, 0)
        Me.TableLayoutPanel4.Controls.Add(Me.cmdAgregar, 0, 0)
        Me.TableLayoutPanel4.Location = New System.Drawing.Point(504, 0)
        Me.TableLayoutPanel4.Margin = New System.Windows.Forms.Padding(0)
        Me.TableLayoutPanel4.Name = "TableLayoutPanel4"
        Me.TableLayoutPanel4.RowCount = 1
        Me.TableLayoutPanel4.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel4.Size = New System.Drawing.Size(141, 40)
        Me.TableLayoutPanel4.TabIndex = 2
        '
        'Button3
        '
        Me.Button3.Location = New System.Drawing.Point(74, 0)
        Me.Button3.Margin = New System.Windows.Forms.Padding(0)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(29, 25)
        Me.Button3.TabIndex = 9
        Me.Button3.Text = "--"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'Button4
        '
        Me.Button4.Location = New System.Drawing.Point(103, 0)
        Me.Button4.Margin = New System.Windows.Forms.Padding(0)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(38, 25)
        Me.Button4.TabIndex = 12
        Me.Button4.Text = "Buscar"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'ValorInicialDataGridViewTextBoxColumn
        '
        Me.ValorInicialDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells
        Me.ValorInicialDataGridViewTextBoxColumn.DataPropertyName = "ValorInicial"
        Me.ValorInicialDataGridViewTextBoxColumn.HeaderText = "ValorInicial"
        Me.ValorInicialDataGridViewTextBoxColumn.Name = "ValorInicialDataGridViewTextBoxColumn"
        Me.ValorInicialDataGridViewTextBoxColumn.Width = 83
        '
        'CondicionDNBindingSource
        '
        Me.CondicionDNBindingSource.DataSource = GetType(MotorBusquedaDN.CondicionDN)
        '
        'ctrlContructorFiltro
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.TableLayoutPanel1)
        Me.Name = "ctrlContructorFiltro"
        Me.Size = New System.Drawing.Size(645, 269)
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TableLayoutPanel1.ResumeLayout(False)
        Me.TableLayoutPanel3.ResumeLayout(False)
        Me.TableLayoutPanel2.ResumeLayout(False)
        Me.TableLayoutPanel2.PerformLayout()
        Me.TableLayoutPanel4.ResumeLayout(False)
        CType(Me.CondicionDNBindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents DataGridView1 As System.Windows.Forms.DataGridView
    Friend WithEvents cboCampos As System.Windows.Forms.ComboBox
    Friend WithEvents cboOperadores As System.Windows.Forms.ComboBox
    Friend WithEvents cboValoresI As System.Windows.Forms.ComboBox
    Friend WithEvents cboValoresF As System.Windows.Forms.ComboBox
    Friend WithEvents cmdAgregar As System.Windows.Forms.Button
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents CondicionDNBindingSource As System.Windows.Forms.BindingSource
    Friend WithEvents capo As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents OperadoresArictmetico As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ValorInicialDataGridViewTextBoxColumn As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ValorFinal As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents TableLayoutPanel2 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents TableLayoutPanel3 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents TableLayoutPanel4 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents Button4 As System.Windows.Forms.Button

End Class
