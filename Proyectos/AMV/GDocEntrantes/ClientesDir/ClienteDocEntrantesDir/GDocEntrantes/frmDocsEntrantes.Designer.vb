<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmDocsEntrantes
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
        Me.components = New System.ComponentModel.Container
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmDocsEntrantes))
        Me.TabControl1 = New System.Windows.Forms.TabControl
        Me.tbpProcesar = New System.Windows.Forms.TabPage
        Me.ctrlClasificar = New GDocEntrantes.ctrlClasificar
        Me.tbpPostProcesar = New System.Windows.Forms.TabPage
        Me.ctrlPostClasificar = New GDocEntrantes.ctrlPostClasificar
        Me.tbpHistorial = New System.Windows.Forms.TabPage
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
        Me.DataGridView1 = New System.Windows.Forms.DataGridView
        Me.cmdAbrir = New System.Windows.Forms.Button
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.CheckBox1 = New System.Windows.Forms.CheckBox
        Me.cmdRefrescar = New System.Windows.Forms.Button
        Me.cmdBuscarArchivo = New System.Windows.Forms.Button
        Me.TabControl1.SuspendLayout()
        Me.tbpProcesar.SuspendLayout()
        Me.tbpPostProcesar.SuspendLayout()
        Me.tbpHistorial.SuspendLayout()
        Me.TableLayoutPanel1.SuspendLayout()
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.tbpProcesar)
        Me.TabControl1.Controls.Add(Me.tbpPostProcesar)
        Me.TabControl1.Controls.Add(Me.tbpHistorial)
        Me.TabControl1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TabControl1.Location = New System.Drawing.Point(0, 0)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(515, 715)
        Me.TabControl1.TabIndex = 19
        '
        'tbpProcesar
        '
        Me.tbpProcesar.BackColor = System.Drawing.Color.Transparent
        Me.tbpProcesar.Controls.Add(Me.ctrlClasificar)
        Me.tbpProcesar.Location = New System.Drawing.Point(4, 22)
        Me.tbpProcesar.Name = "tbpProcesar"
        Me.tbpProcesar.Size = New System.Drawing.Size(507, 689)
        Me.tbpProcesar.TabIndex = 0
        Me.tbpProcesar.Text = "Procesar Entradas"
        Me.tbpProcesar.UseVisualStyleBackColor = True
        '
        'ctrlClasificar
        '
        Me.ctrlClasificar.BackColor = System.Drawing.SystemColors.Control
        Me.ctrlClasificar.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ctrlClasificar.Location = New System.Drawing.Point(0, 0)
        Me.ctrlClasificar.MensajeError = ""
        Me.ctrlClasificar.Name = "ctrlClasificar"
        Me.ctrlClasificar.Size = New System.Drawing.Size(507, 689)
        Me.ctrlClasificar.TabIndex = 0
        Me.ctrlClasificar.ToolTipText = Nothing
        '
        'tbpPostProcesar
        '
        Me.tbpPostProcesar.Controls.Add(Me.ctrlPostClasificar)
        Me.tbpPostProcesar.Location = New System.Drawing.Point(4, 22)
        Me.tbpPostProcesar.Name = "tbpPostProcesar"
        Me.tbpPostProcesar.Size = New System.Drawing.Size(507, 689)
        Me.tbpPostProcesar.TabIndex = 2
        Me.tbpPostProcesar.Text = "Procesar Clasificados"
        Me.tbpPostProcesar.UseVisualStyleBackColor = True
        '
        'ctrlPostClasificar
        '
        Me.ctrlPostClasificar.BackColor = System.Drawing.SystemColors.Control
        Me.ctrlPostClasificar.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ctrlPostClasificar.Location = New System.Drawing.Point(0, 0)
        Me.ctrlPostClasificar.MensajeError = ""
        Me.ctrlPostClasificar.Name = "ctrlPostClasificar"
        Me.ctrlPostClasificar.Size = New System.Drawing.Size(507, 689)
        Me.ctrlPostClasificar.TabIndex = 0
        Me.ctrlPostClasificar.ToolTipText = Nothing
        '
        'tbpHistorial
        '
        Me.tbpHistorial.Controls.Add(Me.TableLayoutPanel1)
        Me.tbpHistorial.Location = New System.Drawing.Point(4, 22)
        Me.tbpHistorial.Name = "tbpHistorial"
        Me.tbpHistorial.Size = New System.Drawing.Size(507, 689)
        Me.tbpHistorial.TabIndex = 1
        Me.tbpHistorial.Text = "Historial de Sesión"
        Me.tbpHistorial.UseVisualStyleBackColor = True
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.ColumnCount = 1
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.Controls.Add(Me.DataGridView1, 0, 0)
        Me.TableLayoutPanel1.Controls.Add(Me.cmdAbrir, 0, 1)
        Me.TableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(0, 0)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 2
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(507, 689)
        Me.TableLayoutPanel1.TabIndex = 1
        '
        'DataGridView1
        '
        Me.DataGridView1.AllowUserToAddRows = False
        Me.DataGridView1.AllowUserToDeleteRows = False
        Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridView1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.DataGridView1.Location = New System.Drawing.Point(3, 3)
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.ReadOnly = True
        Me.DataGridView1.Size = New System.Drawing.Size(501, 633)
        Me.DataGridView1.TabIndex = 0
        '
        'cmdAbrir
        '
        Me.cmdAbrir.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.cmdAbrir.Image = Global.GDocEntrantes.My.Resources.Resources.documento_ver_32
        Me.cmdAbrir.Location = New System.Drawing.Point(3, 645)
        Me.cmdAbrir.Name = "cmdAbrir"
        Me.cmdAbrir.Size = New System.Drawing.Size(90, 41)
        Me.cmdAbrir.TabIndex = 46
        Me.cmdAbrir.Text = "Abrir"
        Me.cmdAbrir.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAbrir.UseVisualStyleBackColor = True
        '
        'Timer1
        '
        Me.Timer1.Interval = 1000
        '
        'CheckBox1
        '
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Location = New System.Drawing.Point(410, 2)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(96, 17)
        Me.CheckBox1.TabIndex = 13
        Me.CheckBox1.Text = "Siempre visible"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'cmdRefrescar
        '
        Me.cmdRefrescar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdRefrescar.Image = CType(resources.GetObject("cmdRefrescar.Image"), System.Drawing.Image)
        Me.cmdRefrescar.Location = New System.Drawing.Point(476, 676)
        Me.cmdRefrescar.Name = "cmdRefrescar"
        Me.cmdRefrescar.Size = New System.Drawing.Size(29, 28)
        Me.cmdRefrescar.TabIndex = 20
        Me.cmdRefrescar.UseVisualStyleBackColor = True
        '
        'cmdBuscarArchivo
        '
        Me.cmdBuscarArchivo.Image = CType(resources.GetObject("cmdBuscarArchivo.Image"), System.Drawing.Image)
        Me.cmdBuscarArchivo.Location = New System.Drawing.Point(314, 1)
        Me.cmdBuscarArchivo.Name = "cmdBuscarArchivo"
        Me.cmdBuscarArchivo.Size = New System.Drawing.Size(81, 19)
        Me.cmdBuscarArchivo.TabIndex = 21
        Me.cmdBuscarArchivo.UseVisualStyleBackColor = True
        '
        'frmDocsEntrantes
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(515, 715)
        Me.Controls.Add(Me.cmdBuscarArchivo)
        Me.Controls.Add(Me.cmdRefrescar)
        Me.Controls.Add(Me.CheckBox1)
        Me.Controls.Add(Me.TabControl1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.KeyPreview = True
        Me.MinimumSize = New System.Drawing.Size(523, 605)
        Me.Name = "frmDocsEntrantes"
        Me.Text = "Procesado de Documentos Entrantes"
        Me.TabControl1.ResumeLayout(False)
        Me.tbpProcesar.ResumeLayout(False)
        Me.tbpPostProcesar.ResumeLayout(False)
        Me.tbpHistorial.ResumeLayout(False)
        Me.TableLayoutPanel1.ResumeLayout(False)
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
    Friend WithEvents tbpProcesar As System.Windows.Forms.TabPage
    Friend WithEvents tbpHistorial As System.Windows.Forms.TabPage
    Friend WithEvents DataGridView1 As System.Windows.Forms.DataGridView
    Friend WithEvents Timer1 As System.Windows.Forms.Timer
    Friend WithEvents CheckBox1 As System.Windows.Forms.CheckBox
    Friend WithEvents tbpPostProcesar As System.Windows.Forms.TabPage
    Friend WithEvents ctrlClasificar As ctrlClasificar
    Friend WithEvents ctrlPostClasificar As ctrlPostClasificar
    Friend WithEvents cmdRefrescar As System.Windows.Forms.Button
    Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents cmdAbrir As System.Windows.Forms.Button
    Friend WithEvents cmdBuscarArchivo As System.Windows.Forms.Button

End Class
