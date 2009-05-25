<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmClienteSonda
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
        Me.components = New System.ComponentModel.Container
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmClienteSonda))
        Me.ctrlCanalEntrada1 = New ClienteSonda.ctrlCanalEntrada
        Me.DataGridView1 = New System.Windows.Forms.DataGridView
        Me.NombreDataGridViewTextBoxColumn = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.TipoCanal = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.TipoEntNegocioReferidoraDataGridViewTextBoxColumn = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.ActividadDataGridViewCheckBoxColumn = New System.Windows.Forms.DataGridViewCheckBoxColumn
        Me.RutaDataGridViewTextBoxColumn = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.Incidentado = New System.Windows.Forms.DataGridViewCheckBoxColumn
        Me.CanalEntradaDocsDNBindingSource = New System.Windows.Forms.BindingSource(Me.components)
        Me.ContextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.btnAñadir = New System.Windows.Forms.Button
        Me.btnGuardar = New System.Windows.Forms.Button
        Me.btnEliminar = New System.Windows.Forms.Button
        Me.btnTodoOn = New System.Windows.Forms.Button
        Me.btnTodoOff = New System.Windows.Forms.Button
        Me.btnBloquear1 = New System.Windows.Forms.Button
        Me.Button1 = New System.Windows.Forms.Button
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.cmdAbrirRuta = New System.Windows.Forms.Button
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.CanalEntradaDocsDNBindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'ctrlCanalEntrada1
        '
        Me.ctrlCanalEntrada1.CanalEntradaDocs = Nothing
        Me.ctrlCanalEntrada1.Location = New System.Drawing.Point(10, 10)
        Me.ctrlCanalEntrada1.Name = "ctrlCanalEntrada1"
        Me.ctrlCanalEntrada1.Size = New System.Drawing.Size(753, 245)
        Me.ctrlCanalEntrada1.TabIndex = 0
        '
        'DataGridView1
        '
        Me.DataGridView1.AllowUserToAddRows = False
        Me.DataGridView1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridView1.AutoGenerateColumns = False
        Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridView1.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.NombreDataGridViewTextBoxColumn, Me.TipoCanal, Me.TipoEntNegocioReferidoraDataGridViewTextBoxColumn, Me.ActividadDataGridViewCheckBoxColumn, Me.RutaDataGridViewTextBoxColumn, Me.Incidentado})
        Me.DataGridView1.DataSource = Me.CanalEntradaDocsDNBindingSource
        Me.DataGridView1.Location = New System.Drawing.Point(9, 290)
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.Size = New System.Drawing.Size(754, 198)
        Me.DataGridView1.TabIndex = 5
        '
        'NombreDataGridViewTextBoxColumn
        '
        Me.NombreDataGridViewTextBoxColumn.DataPropertyName = "Nombre"
        Me.NombreDataGridViewTextBoxColumn.HeaderText = "Nombre"
        Me.NombreDataGridViewTextBoxColumn.Name = "NombreDataGridViewTextBoxColumn"
        '
        'TipoCanal
        '
        Me.TipoCanal.DataPropertyName = "TipoCanal"
        Me.TipoCanal.HeaderText = "TipoCanal"
        Me.TipoCanal.Name = "TipoCanal"
        Me.TipoCanal.ReadOnly = True
        '
        'TipoEntNegocioReferidoraDataGridViewTextBoxColumn
        '
        Me.TipoEntNegocioReferidoraDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.TipoEntNegocioReferidoraDataGridViewTextBoxColumn.DataPropertyName = "TipoEntNegocioReferidora"
        Me.TipoEntNegocioReferidoraDataGridViewTextBoxColumn.HeaderText = "Tipo Entidad Negocio"
        Me.TipoEntNegocioReferidoraDataGridViewTextBoxColumn.Name = "TipoEntNegocioReferidoraDataGridViewTextBoxColumn"
        '
        'ActividadDataGridViewCheckBoxColumn
        '
        Me.ActividadDataGridViewCheckBoxColumn.DataPropertyName = "Actividad"
        Me.ActividadDataGridViewCheckBoxColumn.HeaderText = "Actividad"
        Me.ActividadDataGridViewCheckBoxColumn.Name = "ActividadDataGridViewCheckBoxColumn"
        '
        'RutaDataGridViewTextBoxColumn
        '
        Me.RutaDataGridViewTextBoxColumn.DataPropertyName = "Ruta"
        Me.RutaDataGridViewTextBoxColumn.HeaderText = "Ruta"
        Me.RutaDataGridViewTextBoxColumn.Name = "RutaDataGridViewTextBoxColumn"
        '
        'Incidentado
        '
        Me.Incidentado.DataPropertyName = "Incidentado"
        Me.Incidentado.HeaderText = "Incidentado"
        Me.Incidentado.Name = "Incidentado"
        '
        'CanalEntradaDocsDNBindingSource
        '
        Me.CanalEntradaDocsDNBindingSource.DataSource = GetType(AmvDocumentosDN.CanalEntradaDocsDN)
        '
        'ContextMenuStrip1
        '
        Me.ContextMenuStrip1.Name = "ContextMenuStrip1"
        Me.ContextMenuStrip1.Size = New System.Drawing.Size(61, 4)
        '
        'btnAñadir
        '
        Me.btnAñadir.Image = CType(resources.GetObject("btnAñadir.Image"), System.Drawing.Image)
        Me.btnAñadir.Location = New System.Drawing.Point(9, 261)
        Me.btnAñadir.Name = "btnAñadir"
        Me.btnAñadir.Size = New System.Drawing.Size(75, 23)
        Me.btnAñadir.TabIndex = 0
        Me.btnAñadir.Text = "Añadir"
        Me.btnAñadir.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.btnAñadir.UseVisualStyleBackColor = True
        '
        'btnGuardar
        '
        Me.btnGuardar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnGuardar.Image = CType(resources.GetObject("btnGuardar.Image"), System.Drawing.Image)
        Me.btnGuardar.Location = New System.Drawing.Point(688, 494)
        Me.btnGuardar.Name = "btnGuardar"
        Me.btnGuardar.Size = New System.Drawing.Size(75, 23)
        Me.btnGuardar.TabIndex = 7
        Me.btnGuardar.Text = "Guadar"
        Me.btnGuardar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.btnGuardar.UseVisualStyleBackColor = True
        '
        'btnEliminar
        '
        Me.btnEliminar.Image = CType(resources.GetObject("btnEliminar.Image"), System.Drawing.Image)
        Me.btnEliminar.Location = New System.Drawing.Point(90, 261)
        Me.btnEliminar.Name = "btnEliminar"
        Me.btnEliminar.Size = New System.Drawing.Size(75, 23)
        Me.btnEliminar.TabIndex = 1
        Me.btnEliminar.Text = "Eliminar"
        Me.btnEliminar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.btnEliminar.UseVisualStyleBackColor = True
        '
        'btnTodoOn
        '
        Me.btnTodoOn.Image = CType(resources.GetObject("btnTodoOn.Image"), System.Drawing.Image)
        Me.btnTodoOn.Location = New System.Drawing.Point(454, 261)
        Me.btnTodoOn.Name = "btnTodoOn"
        Me.btnTodoOn.Size = New System.Drawing.Size(82, 23)
        Me.btnTodoOn.TabIndex = 3
        Me.btnTodoOn.Text = "TODO On"
        Me.btnTodoOn.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.btnTodoOn.UseVisualStyleBackColor = True
        '
        'btnTodoOff
        '
        Me.btnTodoOff.Image = CType(resources.GetObject("btnTodoOff.Image"), System.Drawing.Image)
        Me.btnTodoOff.Location = New System.Drawing.Point(542, 261)
        Me.btnTodoOff.Name = "btnTodoOff"
        Me.btnTodoOff.Size = New System.Drawing.Size(87, 23)
        Me.btnTodoOff.TabIndex = 4
        Me.btnTodoOff.Text = "TodoOff"
        Me.btnTodoOff.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.btnTodoOff.UseVisualStyleBackColor = True
        '
        'btnBloquear1
        '
        Me.btnBloquear1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBloquear1.Image = CType(resources.GetObject("btnBloquear1.Image"), System.Drawing.Image)
        Me.btnBloquear1.Location = New System.Drawing.Point(603, 494)
        Me.btnBloquear1.Name = "btnBloquear1"
        Me.btnBloquear1.Size = New System.Drawing.Size(75, 23)
        Me.btnBloquear1.TabIndex = 6
        Me.btnBloquear1.Text = "Bloquear"
        Me.btnBloquear1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.btnBloquear1.UseVisualStyleBackColor = True
        '
        'Button1
        '
        Me.Button1.Image = CType(resources.GetObject("Button1.Image"), System.Drawing.Image)
        Me.Button1.Location = New System.Drawing.Point(298, 261)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(139, 23)
        Me.Button1.TabIndex = 2
        Me.Button1.Text = "Procesar todo ahora"
        Me.Button1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Timer1
        '
        '
        'cmdAbrirRuta
        '
        Me.cmdAbrirRuta.Image = CType(resources.GetObject("cmdAbrirRuta.Image"), System.Drawing.Image)
        Me.cmdAbrirRuta.Location = New System.Drawing.Point(204, 261)
        Me.cmdAbrirRuta.Name = "cmdAbrirRuta"
        Me.cmdAbrirRuta.Size = New System.Drawing.Size(75, 23)
        Me.cmdAbrirRuta.TabIndex = 8
        Me.cmdAbrirRuta.Text = "AbrirRuta"
        Me.cmdAbrirRuta.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAbrirRuta.UseVisualStyleBackColor = True
        '
        'frmClienteSonda
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(775, 523)
        Me.Controls.Add(Me.cmdAbrirRuta)
        Me.Controls.Add(Me.btnBloquear1)
        Me.Controls.Add(Me.ctrlCanalEntrada1)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.DataGridView1)
        Me.Controls.Add(Me.btnEliminar)
        Me.Controls.Add(Me.btnAñadir)
        Me.Controls.Add(Me.btnTodoOff)
        Me.Controls.Add(Me.btnGuardar)
        Me.Controls.Add(Me.btnTodoOn)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MinimumSize = New System.Drawing.Size(643, 557)
        Me.Name = "frmClienteSonda"
        Me.Text = "Cliente Sonda de Directorios"
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.CanalEntradaDocsDNBindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ctrlCanalEntrada1 As ctrlCanalEntrada
    Friend WithEvents DataGridView1 As System.Windows.Forms.DataGridView
    Friend WithEvents ContextMenuStrip1 As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents btnAñadir As System.Windows.Forms.Button
    Friend WithEvents btnGuardar As System.Windows.Forms.Button
    Friend WithEvents btnEliminar As System.Windows.Forms.Button
    Friend WithEvents btnTodoOn As System.Windows.Forms.Button
    Friend WithEvents btnTodoOff As System.Windows.Forms.Button
    Friend WithEvents CanalEntradaDocsDNBindingSource As System.Windows.Forms.BindingSource
    Friend WithEvents btnBloquear1 As System.Windows.Forms.Button
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Timer1 As System.Windows.Forms.Timer
    Friend WithEvents cmdAbrirRuta As System.Windows.Forms.Button
    Friend WithEvents NombreDataGridViewTextBoxColumn As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents TipoCanal As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents TipoEntNegocioReferidoraDataGridViewTextBoxColumn As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ActividadDataGridViewCheckBoxColumn As System.Windows.Forms.DataGridViewCheckBoxColumn
    Friend WithEvents RutaDataGridViewTextBoxColumn As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Incidentado As System.Windows.Forms.DataGridViewCheckBoxColumn

End Class
