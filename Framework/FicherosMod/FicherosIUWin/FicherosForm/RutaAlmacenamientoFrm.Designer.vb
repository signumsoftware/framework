<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class RutaAlmacenamientoFrm
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
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(RutaAlmacenamientoFrm))
        Me.gbDatosRuta = New System.Windows.Forms.GroupBox
        Me.CtrlRutaAlmacenamiento1 = New Framework.Ficheros.FicherosIU.ctrlRutaAlmacenamiento
        Me.dgvRutas = New System.Windows.Forms.DataGridView
        Me.ID = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.GUID = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.Nombre = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.Ruta = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.Estado = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.FechaCreacion = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.gbListadoRutas = New System.Windows.Forms.GroupBox
        Me.cmdCancelar = New System.Windows.Forms.Button
        Me.cmdAbrir = New System.Windows.Forms.Button
        Me.cmdGuardar = New System.Windows.Forms.Button
        Me.cmdNuevo = New System.Windows.Forms.Button
        Me.lblCambiarEstado = New System.Windows.Forms.Label
        Me.btnDisponible = New System.Windows.Forms.Button
        Me.btnAbierta = New System.Windows.Forms.Button
        Me.btnCerrada = New System.Windows.Forms.Button
        Me.gbDatosRuta.SuspendLayout()
        CType(Me.dgvRutas, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.gbListadoRutas.SuspendLayout()
        Me.SuspendLayout()
        '
        'gbDatosRuta
        '
        Me.gbDatosRuta.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gbDatosRuta.Controls.Add(Me.CtrlRutaAlmacenamiento1)
        Me.gbDatosRuta.Location = New System.Drawing.Point(5, 3)
        Me.gbDatosRuta.Name = "gbDatosRuta"
        Me.gbDatosRuta.Size = New System.Drawing.Size(635, 105)
        Me.gbDatosRuta.TabIndex = 1
        Me.gbDatosRuta.TabStop = False
        Me.gbDatosRuta.Text = "Datos ruta almacenamiento"
        '
        'CtrlRutaAlmacenamiento1
        '
        Me.CtrlRutaAlmacenamiento1.BackColor = System.Drawing.SystemColors.Control
        Me.CtrlRutaAlmacenamiento1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.CtrlRutaAlmacenamiento1.Location = New System.Drawing.Point(3, 16)
        Me.CtrlRutaAlmacenamiento1.MensajeError = ""
        Me.CtrlRutaAlmacenamiento1.Name = "CtrlRutaAlmacenamiento1"
        Me.CtrlRutaAlmacenamiento1.Size = New System.Drawing.Size(629, 86)
        Me.CtrlRutaAlmacenamiento1.TabIndex = 0
        Me.CtrlRutaAlmacenamiento1.ToolTipText = Nothing
        '
        'dgvRutas
        '
        Me.dgvRutas.AllowUserToAddRows = False
        Me.dgvRutas.AllowUserToDeleteRows = False
        Me.dgvRutas.AllowUserToOrderColumns = True
        DataGridViewCellStyle1.BackColor = System.Drawing.Color.LightBlue
        Me.dgvRutas.AlternatingRowsDefaultCellStyle = DataGridViewCellStyle1
        Me.dgvRutas.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvRutas.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.ID, Me.GUID, Me.Nombre, Me.Ruta, Me.Estado, Me.FechaCreacion})
        Me.dgvRutas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.dgvRutas.Location = New System.Drawing.Point(3, 16)
        Me.dgvRutas.MultiSelect = False
        Me.dgvRutas.Name = "dgvRutas"
        Me.dgvRutas.ReadOnly = True
        Me.dgvRutas.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvRutas.Size = New System.Drawing.Size(629, 283)
        Me.dgvRutas.TabIndex = 1
        '
        'ID
        '
        Me.ID.HeaderText = "ID"
        Me.ID.Name = "ID"
        Me.ID.ReadOnly = True
        Me.ID.Visible = False
        '
        'GUID
        '
        Me.GUID.HeaderText = "GUID"
        Me.GUID.Name = "GUID"
        Me.GUID.ReadOnly = True
        Me.GUID.Visible = False
        '
        'Nombre
        '
        Me.Nombre.HeaderText = "Nombre"
        Me.Nombre.Name = "Nombre"
        Me.Nombre.ReadOnly = True
        Me.Nombre.Width = 180
        '
        'Ruta
        '
        Me.Ruta.HeaderText = "Ruta"
        Me.Ruta.Name = "Ruta"
        Me.Ruta.ReadOnly = True
        Me.Ruta.Width = 350
        '
        'Estado
        '
        Me.Estado.HeaderText = "Estado"
        Me.Estado.Name = "Estado"
        Me.Estado.ReadOnly = True
        '
        'FechaCreacion
        '
        Me.FechaCreacion.HeaderText = "F Creación"
        Me.FechaCreacion.Name = "FechaCreacion"
        Me.FechaCreacion.ReadOnly = True
        Me.FechaCreacion.Width = 120
        '
        'gbListadoRutas
        '
        Me.gbListadoRutas.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gbListadoRutas.Controls.Add(Me.dgvRutas)
        Me.gbListadoRutas.Location = New System.Drawing.Point(5, 143)
        Me.gbListadoRutas.Name = "gbListadoRutas"
        Me.gbListadoRutas.Size = New System.Drawing.Size(635, 302)
        Me.gbListadoRutas.TabIndex = 3
        Me.gbListadoRutas.TabStop = False
        Me.gbListadoRutas.Text = "Listado rutas"
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCancelar.Image = CType(resources.GetObject("cmdCancelar.Image"), System.Drawing.Image)
        Me.cmdCancelar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdCancelar.Location = New System.Drawing.Point(558, 451)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.Size = New System.Drawing.Size(75, 23)
        Me.cmdCancelar.TabIndex = 5
        Me.cmdCancelar.Text = "Cerrar"
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'cmdAbrir
        '
        Me.cmdAbrir.Image = CType(resources.GetObject("cmdAbrir.Image"), System.Drawing.Image)
        Me.cmdAbrir.Location = New System.Drawing.Point(91, 114)
        Me.cmdAbrir.Name = "cmdAbrir"
        Me.cmdAbrir.Size = New System.Drawing.Size(75, 23)
        Me.cmdAbrir.TabIndex = 8
        Me.cmdAbrir.Text = "Abrir"
        Me.cmdAbrir.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAbrir.UseVisualStyleBackColor = True
        '
        'cmdGuardar
        '
        Me.cmdGuardar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdGuardar.Image = CType(resources.GetObject("cmdGuardar.Image"), System.Drawing.Image)
        Me.cmdGuardar.Location = New System.Drawing.Point(477, 451)
        Me.cmdGuardar.Name = "cmdGuardar"
        Me.cmdGuardar.Size = New System.Drawing.Size(75, 23)
        Me.cmdGuardar.TabIndex = 9
        Me.cmdGuardar.Text = "Guadar"
        Me.cmdGuardar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdGuardar.UseVisualStyleBackColor = True
        '
        'cmdNuevo
        '
        Me.cmdNuevo.Image = CType(resources.GetObject("cmdNuevo.Image"), System.Drawing.Image)
        Me.cmdNuevo.Location = New System.Drawing.Point(10, 114)
        Me.cmdNuevo.Name = "cmdNuevo"
        Me.cmdNuevo.Size = New System.Drawing.Size(75, 23)
        Me.cmdNuevo.TabIndex = 10
        Me.cmdNuevo.Text = "Nueva"
        Me.cmdNuevo.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdNuevo.UseVisualStyleBackColor = True
        '
        'lblCambiarEstado
        '
        Me.lblCambiarEstado.AutoSize = True
        Me.lblCambiarEstado.Location = New System.Drawing.Point(192, 119)
        Me.lblCambiarEstado.Name = "lblCambiarEstado"
        Me.lblCambiarEstado.Size = New System.Drawing.Size(89, 13)
        Me.lblCambiarEstado.TabIndex = 11
        Me.lblCambiarEstado.Text = "Cambiar estado a"
        '
        'btnDisponible
        '
        Me.btnDisponible.Enabled = False
        Me.btnDisponible.Image = CType(resources.GetObject("btnDisponible.Image"), System.Drawing.Image)
        Me.btnDisponible.Location = New System.Drawing.Point(287, 114)
        Me.btnDisponible.Name = "btnDisponible"
        Me.btnDisponible.Size = New System.Drawing.Size(86, 23)
        Me.btnDisponible.TabIndex = 12
        Me.btnDisponible.Text = "Disponible"
        Me.btnDisponible.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.btnDisponible.UseVisualStyleBackColor = True
        '
        'btnAbierta
        '
        Me.btnAbierta.Enabled = False
        Me.btnAbierta.Image = CType(resources.GetObject("btnAbierta.Image"), System.Drawing.Image)
        Me.btnAbierta.Location = New System.Drawing.Point(379, 114)
        Me.btnAbierta.Name = "btnAbierta"
        Me.btnAbierta.Size = New System.Drawing.Size(86, 23)
        Me.btnAbierta.TabIndex = 13
        Me.btnAbierta.Text = "Abierta"
        Me.btnAbierta.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.btnAbierta.UseVisualStyleBackColor = True
        '
        'btnCerrada
        '
        Me.btnCerrada.Enabled = False
        Me.btnCerrada.Image = CType(resources.GetObject("btnCerrada.Image"), System.Drawing.Image)
        Me.btnCerrada.Location = New System.Drawing.Point(471, 114)
        Me.btnCerrada.Name = "btnCerrada"
        Me.btnCerrada.Size = New System.Drawing.Size(86, 23)
        Me.btnCerrada.TabIndex = 14
        Me.btnCerrada.Text = "Cerrada"
        Me.btnCerrada.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.btnCerrada.UseVisualStyleBackColor = True
        '
        'RutaAlmacenamientoFrm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(652, 482)
        Me.Controls.Add(Me.btnCerrada)
        Me.Controls.Add(Me.btnAbierta)
        Me.Controls.Add(Me.btnDisponible)
        Me.Controls.Add(Me.lblCambiarEstado)
        Me.Controls.Add(Me.cmdNuevo)
        Me.Controls.Add(Me.cmdGuardar)
        Me.Controls.Add(Me.cmdAbrir)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.gbListadoRutas)
        Me.Controls.Add(Me.gbDatosRuta)
        Me.Name = "RutaAlmacenamientoFrm"
        Me.Text = "Rutas de almacenamientos"
        Me.gbDatosRuta.ResumeLayout(False)
        CType(Me.dgvRutas, System.ComponentModel.ISupportInitialize).EndInit()
        Me.gbListadoRutas.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents CtrlRutaAlmacenamiento1 As Framework.Ficheros.FicherosIU.ctrlRutaAlmacenamiento
    Friend WithEvents gbDatosRuta As System.Windows.Forms.GroupBox
    Friend WithEvents dgvRutas As System.Windows.Forms.DataGridView
    Friend WithEvents gbListadoRutas As System.Windows.Forms.GroupBox
    Friend WithEvents cmdCancelar As System.Windows.Forms.Button
    Friend WithEvents cmdAbrir As System.Windows.Forms.Button
    Friend WithEvents cmdGuardar As System.Windows.Forms.Button
    Friend WithEvents cmdNuevo As System.Windows.Forms.Button
    Friend WithEvents ID As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents GUID As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Nombre As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Ruta As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Estado As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents FechaCreacion As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents lblCambiarEstado As System.Windows.Forms.Label
    Friend WithEvents btnDisponible As System.Windows.Forms.Button
    Friend WithEvents btnAbierta As System.Windows.Forms.Button
    Friend WithEvents btnCerrada As System.Windows.Forms.Button

End Class
