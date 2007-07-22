<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlPostClasificar
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ctrlPostClasificar))
        Dim DataGridViewCellStyle3 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle
        Me.Label2 = New System.Windows.Forms.Label
        Me.cmdRechazarOperacion = New System.Windows.Forms.Button
        Me.cmdAnularOperacion = New System.Windows.Forms.Button
        Me.cmdIncidentarOperacion = New System.Windows.Forms.Button
        Me.cmdRecuperarSiguteOperacion = New System.Windows.Forms.Button
        Me.Label4 = New System.Windows.Forms.Label
        Me.ArbolNododeT1 = New ControlesPGenericos.ArbolTConBusqueda
        Me.cmdEliminarEntidad = New System.Windows.Forms.Button
        Me.dgvEntidades = New System.Windows.Forms.DataGridView
        Me.cboTipoDocs = New System.Windows.Forms.ComboBox
        Me.Label1 = New System.Windows.Forms.Label
        Me.lblDocumentoCargado = New System.Windows.Forms.Label
        Me.Label5 = New System.Windows.Forms.Label
        Me.txtvComentarioOperacion = New ControlesPBase.txtValidable
        Me.cmdAbrir = New System.Windows.Forms.Button
        Me.cmdCopiarRuta = New System.Windows.Forms.Button
        Me.cmdCopiarID = New System.Windows.Forms.Button
        Me.Label3 = New System.Windows.Forms.Label
        Me.lblBusquedaArbol = New System.Windows.Forms.Label
        Me.cmdSeleccionarArbol = New System.Windows.Forms.Button
        Me.cmdAceptarYCerrarOperacion = New System.Windows.Forms.Button
        Me.Panel1 = New System.Windows.Forms.Panel
        Me.cmdAceptarOperacion = New System.Windows.Forms.Button
        Me.Button2 = New System.Windows.Forms.Button
        Me.DataGridView1 = New System.Windows.Forms.DataGridView
        Me.Button1 = New System.Windows.Forms.Button
        Me.ComboBox1 = New System.Windows.Forms.ComboBox
        CType(Me.dgvEntidades, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel1.SuspendLayout()
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Label2
        '
        Me.Label2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(5, 605)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(138, 13)
        Me.Label2.TabIndex = 81
        Me.Label2.Text = "Comentario sobre el Fichero"
        '
        'cmdRechazarOperacion
        '
        Me.cmdRechazarOperacion.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdRechazarOperacion.Image = CType(resources.GetObject("cmdRechazarOperacion.Image"), System.Drawing.Image)
        Me.cmdRechazarOperacion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdRechazarOperacion.Location = New System.Drawing.Point(432, 332)
        Me.cmdRechazarOperacion.Name = "cmdRechazarOperacion"
        Me.cmdRechazarOperacion.Size = New System.Drawing.Size(102, 43)
        Me.cmdRechazarOperacion.TabIndex = 80
        Me.cmdRechazarOperacion.Text = "Rechazar"
        Me.cmdRechazarOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdRechazarOperacion.UseVisualStyleBackColor = True
        '
        'cmdAnularOperacion
        '
        Me.cmdAnularOperacion.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAnularOperacion.Image = CType(resources.GetObject("cmdAnularOperacion.Image"), System.Drawing.Image)
        Me.cmdAnularOperacion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdAnularOperacion.Location = New System.Drawing.Point(432, 281)
        Me.cmdAnularOperacion.Name = "cmdAnularOperacion"
        Me.cmdAnularOperacion.Size = New System.Drawing.Size(104, 43)
        Me.cmdAnularOperacion.TabIndex = 79
        Me.cmdAnularOperacion.Text = "Anular"
        Me.cmdAnularOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAnularOperacion.UseVisualStyleBackColor = True
        '
        'cmdIncidentarOperacion
        '
        Me.cmdIncidentarOperacion.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdIncidentarOperacion.Image = CType(resources.GetObject("cmdIncidentarOperacion.Image"), System.Drawing.Image)
        Me.cmdIncidentarOperacion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdIncidentarOperacion.Location = New System.Drawing.Point(432, 232)
        Me.cmdIncidentarOperacion.Name = "cmdIncidentarOperacion"
        Me.cmdIncidentarOperacion.Size = New System.Drawing.Size(101, 43)
        Me.cmdIncidentarOperacion.TabIndex = 78
        Me.cmdIncidentarOperacion.Text = "Incidentar"
        Me.cmdIncidentarOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdIncidentarOperacion.UseVisualStyleBackColor = True
        '
        'cmdRecuperarSiguteOperacion
        '
        Me.cmdRecuperarSiguteOperacion.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdRecuperarSiguteOperacion.Image = CType(resources.GetObject("cmdRecuperarSiguteOperacion.Image"), System.Drawing.Image)
        Me.cmdRecuperarSiguteOperacion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdRecuperarSiguteOperacion.Location = New System.Drawing.Point(432, 21)
        Me.cmdRecuperarSiguteOperacion.Name = "cmdRecuperarSiguteOperacion"
        Me.cmdRecuperarSiguteOperacion.Size = New System.Drawing.Size(101, 74)
        Me.cmdRecuperarSiguteOperacion.TabIndex = 75
        Me.cmdRecuperarSiguteOperacion.Text = "Recuperar Siguiente Doc" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "(F1)"
        Me.cmdRecuperarSiguteOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdRecuperarSiguteOperacion.UseVisualStyleBackColor = True
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(10, 57)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(95, 13)
        Me.Label4.TabIndex = 23
        Me.Label4.Text = "Árbol de entidades"
        '
        'ArbolNododeT1
        '
        Me.ArbolNododeT1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ArbolNododeT1.BackColor = System.Drawing.SystemColors.Control
        Me.ArbolNododeT1.Location = New System.Drawing.Point(8, 57)
        Me.ArbolNododeT1.MensajeError = ""
        Me.ArbolNododeT1.Name = "ArbolNododeT1"
        Me.ArbolNododeT1.Size = New System.Drawing.Size(399, 276)
        Me.ArbolNododeT1.TabIndex = 3
        Me.ArbolNododeT1.ToolTipText = Nothing
        '
        'cmdEliminarEntidad
        '
        Me.cmdEliminarEntidad.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdEliminarEntidad.Image = CType(resources.GetObject("cmdEliminarEntidad.Image"), System.Drawing.Image)
        Me.cmdEliminarEntidad.Location = New System.Drawing.Point(373, 334)
        Me.cmdEliminarEntidad.Name = "cmdEliminarEntidad"
        Me.cmdEliminarEntidad.Size = New System.Drawing.Size(25, 23)
        Me.cmdEliminarEntidad.TabIndex = 73
        Me.cmdEliminarEntidad.UseVisualStyleBackColor = True
        '
        'dgvEntidades
        '
        Me.dgvEntidades.AllowUserToAddRows = False
        Me.dgvEntidades.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dgvEntidades.BackgroundColor = System.Drawing.Color.White
        Me.dgvEntidades.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvEntidades.Location = New System.Drawing.Point(7, 360)
        Me.dgvEntidades.Name = "dgvEntidades"
        Me.dgvEntidades.RowHeadersVisible = False
        Me.dgvEntidades.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect
        Me.dgvEntidades.Size = New System.Drawing.Size(400, 112)
        Me.dgvEntidades.TabIndex = 72
        '
        'cboTipoDocs
        '
        Me.cboTipoDocs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboTipoDocs.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.cboTipoDocs.FormattingEnabled = True
        Me.cboTipoDocs.Location = New System.Drawing.Point(68, 7)
        Me.cboTipoDocs.Name = "cboTipoDocs"
        Me.cboTipoDocs.Size = New System.Drawing.Size(113, 21)
        Me.cboTipoDocs.TabIndex = 63
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(5, 10)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(41, 13)
        Me.Label1.TabIndex = 71
        Me.Label1.Text = "Cargar:"
        '
        'lblDocumentoCargado
        '
        Me.lblDocumentoCargado.AutoEllipsis = True
        Me.lblDocumentoCargado.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblDocumentoCargado.ForeColor = System.Drawing.Color.Blue
        Me.lblDocumentoCargado.Location = New System.Drawing.Point(117, 2)
        Me.lblDocumentoCargado.Name = "lblDocumentoCargado"
        Me.lblDocumentoCargado.Size = New System.Drawing.Size(239, 13)
        Me.lblDocumentoCargado.TabIndex = 70
        Me.lblDocumentoCargado.Text = "Ninguno"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.ForeColor = System.Drawing.Color.Blue
        Me.Label5.Location = New System.Drawing.Point(4, 2)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(107, 13)
        Me.Label5.TabIndex = 69
        Me.Label5.Text = "Documento cargado:"
        '
        'txtvComentarioOperacion
        '
        Me.txtvComentarioOperacion.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtvComentarioOperacion.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtvComentarioOperacion.Location = New System.Drawing.Point(8, 621)
        Me.txtvComentarioOperacion.MensajeErrorValidacion = Nothing
        Me.txtvComentarioOperacion.Multiline = True
        Me.txtvComentarioOperacion.Name = "txtvComentarioOperacion"
        Me.txtvComentarioOperacion.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtvComentarioOperacion.Size = New System.Drawing.Size(399, 49)
        Me.txtvComentarioOperacion.TabIndex = 65
        Me.txtvComentarioOperacion.ToolTipText = Nothing
        Me.txtvComentarioOperacion.TrimText = False
        '
        'cmdAbrir
        '
        Me.cmdAbrir.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAbrir.Image = Global.GDocEntrantes.My.Resources.Resources.documento_ver_32
        Me.cmdAbrir.Location = New System.Drawing.Point(429, 524)
        Me.cmdAbrir.Name = "cmdAbrir"
        Me.cmdAbrir.Size = New System.Drawing.Size(104, 41)
        Me.cmdAbrir.TabIndex = 64
        Me.cmdAbrir.Text = "Abrir (F4)"
        Me.cmdAbrir.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAbrir.UseVisualStyleBackColor = True
        '
        'cmdCopiarRuta
        '
        Me.cmdCopiarRuta.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCopiarRuta.Image = CType(resources.GetObject("cmdCopiarRuta.Image"), System.Drawing.Image)
        Me.cmdCopiarRuta.Location = New System.Drawing.Point(429, 477)
        Me.cmdCopiarRuta.Name = "cmdCopiarRuta"
        Me.cmdCopiarRuta.Size = New System.Drawing.Size(104, 41)
        Me.cmdCopiarRuta.TabIndex = 67
        Me.cmdCopiarRuta.Text = "Ruta (F3)"
        Me.cmdCopiarRuta.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCopiarRuta.UseVisualStyleBackColor = True
        '
        'cmdCopiarID
        '
        Me.cmdCopiarID.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCopiarID.Image = CType(resources.GetObject("cmdCopiarID.Image"), System.Drawing.Image)
        Me.cmdCopiarID.Location = New System.Drawing.Point(431, 430)
        Me.cmdCopiarID.Name = "cmdCopiarID"
        Me.cmdCopiarID.Size = New System.Drawing.Size(102, 41)
        Me.cmdCopiarID.TabIndex = 66
        Me.cmdCopiarID.Text = "ID (F2)"
        Me.cmdCopiarID.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCopiarID.UseVisualStyleBackColor = True
        '
        'Label3
        '
        Me.Label3.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(13, 344)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(112, 13)
        Me.Label3.TabIndex = 68
        Me.Label3.Text = "Entidad/es Referida/s"
        '
        'lblBusquedaArbol
        '
        Me.lblBusquedaArbol.AllowDrop = True
        Me.lblBusquedaArbol.AutoEllipsis = True
        Me.lblBusquedaArbol.BackColor = System.Drawing.Color.White
        Me.lblBusquedaArbol.Location = New System.Drawing.Point(187, 7)
        Me.lblBusquedaArbol.Name = "lblBusquedaArbol"
        Me.lblBusquedaArbol.Size = New System.Drawing.Size(122, 21)
        Me.lblBusquedaArbol.TabIndex = 82
        Me.lblBusquedaArbol.Text = "Todos"
        Me.lblBusquedaArbol.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'cmdSeleccionarArbol
        '
        Me.cmdSeleccionarArbol.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.cmdSeleccionarArbol.Image = CType(resources.GetObject("cmdSeleccionarArbol.Image"), System.Drawing.Image)
        Me.cmdSeleccionarArbol.Location = New System.Drawing.Point(325, 5)
        Me.cmdSeleccionarArbol.Name = "cmdSeleccionarArbol"
        Me.cmdSeleccionarArbol.Size = New System.Drawing.Size(41, 24)
        Me.cmdSeleccionarArbol.TabIndex = 83
        Me.cmdSeleccionarArbol.UseVisualStyleBackColor = True
        '
        'cmdAceptarYCerrarOperacion
        '
        Me.cmdAceptarYCerrarOperacion.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAceptarYCerrarOperacion.Image = Global.GDocEntrantes.My.Resources.Resources.documento_ok_32
        Me.cmdAceptarYCerrarOperacion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdAceptarYCerrarOperacion.Location = New System.Drawing.Point(432, 158)
        Me.cmdAceptarYCerrarOperacion.Name = "cmdAceptarYCerrarOperacion"
        Me.cmdAceptarYCerrarOperacion.Size = New System.Drawing.Size(101, 50)
        Me.cmdAceptarYCerrarOperacion.TabIndex = 77
        Me.cmdAceptarYCerrarOperacion.Text = "Aceptar y Cerrar" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "(F6)"
        Me.cmdAceptarYCerrarOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAceptarYCerrarOperacion.UseVisualStyleBackColor = True
        '
        'Panel1
        '
        Me.Panel1.Controls.Add(Me.cmdSeleccionarArbol)
        Me.Panel1.Controls.Add(Me.lblBusquedaArbol)
        Me.Panel1.Controls.Add(Me.cboTipoDocs)
        Me.Panel1.Controls.Add(Me.Label1)
        Me.Panel1.Location = New System.Drawing.Point(8, 20)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(399, 31)
        Me.Panel1.TabIndex = 84
        '
        'cmdAceptarOperacion
        '
        Me.cmdAceptarOperacion.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAceptarOperacion.Image = CType(resources.GetObject("cmdAceptarOperacion.Image"), System.Drawing.Image)
        Me.cmdAceptarOperacion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdAceptarOperacion.Location = New System.Drawing.Point(432, 109)
        Me.cmdAceptarOperacion.Name = "cmdAceptarOperacion"
        Me.cmdAceptarOperacion.Size = New System.Drawing.Size(101, 43)
        Me.cmdAceptarOperacion.TabIndex = 85
        Me.cmdAceptarOperacion.Text = "Clasificar" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "(F5)"
        Me.cmdAceptarOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAceptarOperacion.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button2.Image = CType(resources.GetObject("Button2.Image"), System.Drawing.Image)
        Me.Button2.Location = New System.Drawing.Point(344, 478)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(25, 23)
        Me.Button2.TabIndex = 89
        Me.Button2.UseVisualStyleBackColor = True
        '
        'DataGridView1
        '
        Me.DataGridView1.AllowUserToAddRows = False
        Me.DataGridView1.AllowUserToDeleteRows = False
        DataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(CType(CType(192, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.DataGridView1.AlternatingRowsDefaultCellStyle = DataGridViewCellStyle3
        Me.DataGridView1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridView1.BackgroundColor = System.Drawing.SystemColors.ControlLightLight
        Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridView1.Location = New System.Drawing.Point(8, 504)
        Me.DataGridView1.MultiSelect = False
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.Size = New System.Drawing.Size(399, 98)
        Me.DataGridView1.TabIndex = 88
        '
        'Button1
        '
        Me.Button1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button1.Image = CType(resources.GetObject("Button1.Image"), System.Drawing.Image)
        Me.Button1.Location = New System.Drawing.Point(373, 478)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(25, 23)
        Me.Button1.TabIndex = 87
        Me.Button1.UseVisualStyleBackColor = True
        '
        'ComboBox1
        '
        Me.ComboBox1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ComboBox1.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest
        Me.ComboBox1.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems
        Me.ComboBox1.FormattingEnabled = True
        Me.ComboBox1.Location = New System.Drawing.Point(8, 478)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(309, 21)
        Me.ComboBox1.TabIndex = 86
        '
        'ctrlPostClasificar
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.DataGridView1)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.ComboBox1)
        Me.Controls.Add(Me.cmdAceptarOperacion)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.ArbolNododeT1)
        Me.Controls.Add(Me.cmdRechazarOperacion)
        Me.Controls.Add(Me.cmdAnularOperacion)
        Me.Controls.Add(Me.cmdIncidentarOperacion)
        Me.Controls.Add(Me.cmdAceptarYCerrarOperacion)
        Me.Controls.Add(Me.cmdRecuperarSiguteOperacion)
        Me.Controls.Add(Me.cmdEliminarEntidad)
        Me.Controls.Add(Me.dgvEntidades)
        Me.Controls.Add(Me.lblDocumentoCargado)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.txtvComentarioOperacion)
        Me.Controls.Add(Me.cmdAbrir)
        Me.Controls.Add(Me.cmdCopiarRuta)
        Me.Controls.Add(Me.cmdCopiarID)
        Me.Controls.Add(Me.Label3)
        Me.Name = "ctrlPostClasificar"
        Me.Size = New System.Drawing.Size(536, 673)
        CType(Me.dgvEntidades, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents cmdRechazarOperacion As System.Windows.Forms.Button
    Friend WithEvents cmdAnularOperacion As System.Windows.Forms.Button
    Friend WithEvents cmdIncidentarOperacion As System.Windows.Forms.Button
    Friend WithEvents cmdRecuperarSiguteOperacion As System.Windows.Forms.Button
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents ArbolNododeT1 As ControlesPGenericos.ArbolTConBusqueda
    Friend WithEvents cmdEliminarEntidad As System.Windows.Forms.Button
    Friend WithEvents dgvEntidades As System.Windows.Forms.DataGridView
    Friend WithEvents cboTipoDocs As System.Windows.Forms.ComboBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents lblDocumentoCargado As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents txtvComentarioOperacion As ControlesPBase.txtValidable
    Friend WithEvents cmdAbrir As System.Windows.Forms.Button
    Friend WithEvents cmdCopiarRuta As System.Windows.Forms.Button
    Friend WithEvents cmdCopiarID As System.Windows.Forms.Button
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents lblBusquedaArbol As System.Windows.Forms.Label
    Friend WithEvents cmdSeleccionarArbol As System.Windows.Forms.Button
    Friend WithEvents cmdAceptarYCerrarOperacion As System.Windows.Forms.Button
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents cmdAceptarOperacion As System.Windows.Forms.Button
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents DataGridView1 As System.Windows.Forms.DataGridView
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox

End Class
