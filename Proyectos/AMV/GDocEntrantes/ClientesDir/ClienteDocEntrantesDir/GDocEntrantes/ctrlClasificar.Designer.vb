<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlClasificar
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ctrlClasificar))
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer
        Me.SelectorCargaTab = New System.Windows.Forms.TabControl
        Me.tpCanales = New System.Windows.Forms.TabPage
        Me.chkCanales = New System.Windows.Forms.CheckBox
        Me.lstCanales = New System.Windows.Forms.ListBox
        Me.Label6 = New System.Windows.Forms.Label
        Me.tpTipoEntNeg = New System.Windows.Forms.TabPage
        Me.Panel1 = New System.Windows.Forms.Panel
        Me.cmdSeleccionarArbol = New System.Windows.Forms.Button
        Me.lblBusquedaArbol = New System.Windows.Forms.Label
        Me.Label7 = New System.Windows.Forms.Label
        Me.Label4 = New System.Windows.Forms.Label
        Me.ArbolNododeT1 = New ControlesPGenericos.ArbolTConBusqueda
        Me.dgvEntidades = New System.Windows.Forms.DataGridView
        Me.lblDocumentoCargado = New System.Windows.Forms.Label
        Me.Label5 = New System.Windows.Forms.Label
        Me.txtvComentarioOperacion = New ControlesPBase.txtValidable
        Me.Label3 = New System.Windows.Forms.Label
        Me.cmdEliminarEntidad = New System.Windows.Forms.Button
        Me.cmdAbrir = New System.Windows.Forms.Button
        Me.cmdCopiarRuta = New System.Windows.Forms.Button
        Me.cmdCopiarID = New System.Windows.Forms.Button
        Me.cmdRechazarOperacion = New System.Windows.Forms.Button
        Me.cmdAnularOperacion = New System.Windows.Forms.Button
        Me.cmdIncidentarOperacion = New System.Windows.Forms.Button
        Me.cmdAceptarOperacion = New System.Windows.Forms.Button
        Me.cmdRecuperarSiguteOperacion = New System.Windows.Forms.Button
        Me.Label2 = New System.Windows.Forms.Label
        Me.cmdAceptarYCerrar = New System.Windows.Forms.Button
        Me.chkMultiseleccion = New System.Windows.Forms.CheckBox
        Me.ComboBox1 = New System.Windows.Forms.ComboBox
        Me.Button1 = New System.Windows.Forms.Button
        Me.DataGridView1 = New System.Windows.Forms.DataGridView
        Me.Button2 = New System.Windows.Forms.Button
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        Me.SelectorCargaTab.SuspendLayout()
        Me.tpCanales.SuspendLayout()
        Me.tpTipoEntNeg.SuspendLayout()
        Me.Panel1.SuspendLayout()
        CType(Me.dgvEntidades, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'SplitContainer1
        '
        Me.SplitContainer1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.SplitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.SplitContainer1.ForeColor = System.Drawing.SystemColors.ControlText
        Me.SplitContainer1.Location = New System.Drawing.Point(4, 21)
        Me.SplitContainer1.Name = "SplitContainer1"
        Me.SplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal
        '
        'SplitContainer1.Panel1
        '
        Me.SplitContainer1.Panel1.Controls.Add(Me.SelectorCargaTab)
        '
        'SplitContainer1.Panel2
        '
        Me.SplitContainer1.Panel2.Controls.Add(Me.Label4)
        Me.SplitContainer1.Panel2.Controls.Add(Me.ArbolNododeT1)
        Me.SplitContainer1.Size = New System.Drawing.Size(470, 401)
        Me.SplitContainer1.SplitterDistance = 124
        Me.SplitContainer1.TabIndex = 55
        '
        'SelectorCargaTab
        '
        Me.SelectorCargaTab.Controls.Add(Me.tpCanales)
        Me.SelectorCargaTab.Controls.Add(Me.tpTipoEntNeg)
        Me.SelectorCargaTab.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SelectorCargaTab.Location = New System.Drawing.Point(0, 0)
        Me.SelectorCargaTab.Name = "SelectorCargaTab"
        Me.SelectorCargaTab.SelectedIndex = 0
        Me.SelectorCargaTab.Size = New System.Drawing.Size(468, 122)
        Me.SelectorCargaTab.TabIndex = 69
        '
        'tpCanales
        '
        Me.tpCanales.Controls.Add(Me.chkCanales)
        Me.tpCanales.Controls.Add(Me.lstCanales)
        Me.tpCanales.Controls.Add(Me.Label6)
        Me.tpCanales.Location = New System.Drawing.Point(4, 22)
        Me.tpCanales.Name = "tpCanales"
        Me.tpCanales.Padding = New System.Windows.Forms.Padding(3)
        Me.tpCanales.Size = New System.Drawing.Size(460, 96)
        Me.tpCanales.TabIndex = 0
        Me.tpCanales.Text = "Nuevos"
        Me.tpCanales.UseVisualStyleBackColor = True
        '
        'chkCanales
        '
        Me.chkCanales.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.chkCanales.AutoSize = True
        Me.chkCanales.Location = New System.Drawing.Point(316, 6)
        Me.chkCanales.Name = "chkCanales"
        Me.chkCanales.Size = New System.Drawing.Size(135, 17)
        Me.chkCanales.TabIndex = 53
        Me.chkCanales.Text = "Recuperar por Canales"
        Me.chkCanales.UseVisualStyleBackColor = True
        '
        'lstCanales
        '
        Me.lstCanales.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstCanales.Enabled = False
        Me.lstCanales.FormattingEnabled = True
        Me.lstCanales.IntegralHeight = False
        Me.lstCanales.Location = New System.Drawing.Point(6, 30)
        Me.lstCanales.MultiColumn = True
        Me.lstCanales.Name = "lstCanales"
        Me.lstCanales.Size = New System.Drawing.Size(448, 60)
        Me.lstCanales.TabIndex = 44
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(2, 8)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(100, 13)
        Me.Label6.TabIndex = 43
        Me.Label6.Text = "Canales de Entrada"
        '
        'tpTipoEntNeg
        '
        Me.tpTipoEntNeg.Controls.Add(Me.Panel1)
        Me.tpTipoEntNeg.Location = New System.Drawing.Point(4, 22)
        Me.tpTipoEntNeg.Name = "tpTipoEntNeg"
        Me.tpTipoEntNeg.Padding = New System.Windows.Forms.Padding(3)
        Me.tpTipoEntNeg.Size = New System.Drawing.Size(460, 96)
        Me.tpTipoEntNeg.TabIndex = 1
        Me.tpTipoEntNeg.Text = "Clasificados"
        Me.tpTipoEntNeg.UseVisualStyleBackColor = True
        '
        'Panel1
        '
        Me.Panel1.Controls.Add(Me.cmdSeleccionarArbol)
        Me.Panel1.Controls.Add(Me.lblBusquedaArbol)
        Me.Panel1.Controls.Add(Me.Label7)
        Me.Panel1.Location = New System.Drawing.Point(3, 7)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(399, 31)
        Me.Panel1.TabIndex = 85
        '
        'cmdSeleccionarArbol
        '
        Me.cmdSeleccionarArbol.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.cmdSeleccionarArbol.Image = CType(resources.GetObject("cmdSeleccionarArbol.Image"), System.Drawing.Image)
        Me.cmdSeleccionarArbol.Location = New System.Drawing.Point(243, 7)
        Me.cmdSeleccionarArbol.Name = "cmdSeleccionarArbol"
        Me.cmdSeleccionarArbol.Size = New System.Drawing.Size(41, 24)
        Me.cmdSeleccionarArbol.TabIndex = 83
        Me.cmdSeleccionarArbol.UseVisualStyleBackColor = True
        '
        'lblBusquedaArbol
        '
        Me.lblBusquedaArbol.AllowDrop = True
        Me.lblBusquedaArbol.AutoEllipsis = True
        Me.lblBusquedaArbol.BackColor = System.Drawing.Color.White
        Me.lblBusquedaArbol.Location = New System.Drawing.Point(52, 5)
        Me.lblBusquedaArbol.Name = "lblBusquedaArbol"
        Me.lblBusquedaArbol.Size = New System.Drawing.Size(174, 21)
        Me.lblBusquedaArbol.TabIndex = 82
        Me.lblBusquedaArbol.Text = "Todos"
        Me.lblBusquedaArbol.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(5, 10)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(41, 13)
        Me.Label7.TabIndex = 71
        Me.Label7.Text = "Cargar:"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(9, 13)
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
        Me.ArbolNododeT1.Location = New System.Drawing.Point(9, 13)
        Me.ArbolNododeT1.MensajeError = ""
        Me.ArbolNododeT1.Name = "ArbolNododeT1"
        Me.ArbolNododeT1.Size = New System.Drawing.Size(446, 254)
        Me.ArbolNododeT1.TabIndex = 3
        Me.ArbolNododeT1.ToolTipText = Nothing
        '
        'dgvEntidades
        '
        Me.dgvEntidades.AllowUserToAddRows = False
        Me.dgvEntidades.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dgvEntidades.BackgroundColor = System.Drawing.Color.White
        Me.dgvEntidades.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvEntidades.Location = New System.Drawing.Point(4, 453)
        Me.dgvEntidades.Name = "dgvEntidades"
        Me.dgvEntidades.RowHeadersVisible = False
        Me.dgvEntidades.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect
        Me.dgvEntidades.Size = New System.Drawing.Size(467, 84)
        Me.dgvEntidades.TabIndex = 53
        '
        'lblDocumentoCargado
        '
        Me.lblDocumentoCargado.AutoEllipsis = True
        Me.lblDocumentoCargado.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblDocumentoCargado.ForeColor = System.Drawing.Color.Blue
        Me.lblDocumentoCargado.Location = New System.Drawing.Point(116, 3)
        Me.lblDocumentoCargado.Name = "lblDocumentoCargado"
        Me.lblDocumentoCargado.Size = New System.Drawing.Size(464, 13)
        Me.lblDocumentoCargado.TabIndex = 51
        Me.lblDocumentoCargado.Text = "Ninguno"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.ForeColor = System.Drawing.Color.Blue
        Me.Label5.Location = New System.Drawing.Point(3, 3)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(107, 13)
        Me.Label5.TabIndex = 50
        Me.Label5.Text = "Documento cargado:"
        '
        'txtvComentarioOperacion
        '
        Me.txtvComentarioOperacion.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtvComentarioOperacion.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtvComentarioOperacion.Location = New System.Drawing.Point(4, 727)
        Me.txtvComentarioOperacion.MensajeErrorValidacion = Nothing
        Me.txtvComentarioOperacion.Multiline = True
        Me.txtvComentarioOperacion.Name = "txtvComentarioOperacion"
        Me.txtvComentarioOperacion.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtvComentarioOperacion.Size = New System.Drawing.Size(467, 49)
        Me.txtvComentarioOperacion.TabIndex = 46
        Me.txtvComentarioOperacion.ToolTipText = Nothing
        Me.txtvComentarioOperacion.TrimText = False
        '
        'Label3
        '
        Me.Label3.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(4, 437)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(112, 13)
        Me.Label3.TabIndex = 49
        Me.Label3.Text = "Entidad/es Referida/s"
        '
        'cmdEliminarEntidad
        '
        Me.cmdEliminarEntidad.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdEliminarEntidad.Image = CType(resources.GetObject("cmdEliminarEntidad.Image"), System.Drawing.Image)
        Me.cmdEliminarEntidad.Location = New System.Drawing.Point(427, 428)
        Me.cmdEliminarEntidad.Name = "cmdEliminarEntidad"
        Me.cmdEliminarEntidad.Size = New System.Drawing.Size(25, 23)
        Me.cmdEliminarEntidad.TabIndex = 54
        Me.cmdEliminarEntidad.UseVisualStyleBackColor = True
        '
        'cmdAbrir
        '
        Me.cmdAbrir.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAbrir.Image = Global.GDocEntrantes.My.Resources.Resources.documento_ver_32
        Me.cmdAbrir.Location = New System.Drawing.Point(477, 541)
        Me.cmdAbrir.Name = "cmdAbrir"
        Me.cmdAbrir.Size = New System.Drawing.Size(100, 41)
        Me.cmdAbrir.TabIndex = 45
        Me.cmdAbrir.Text = "Abrir (F4)"
        Me.cmdAbrir.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAbrir.UseVisualStyleBackColor = True
        '
        'cmdCopiarRuta
        '
        Me.cmdCopiarRuta.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCopiarRuta.Image = CType(resources.GetObject("cmdCopiarRuta.Image"), System.Drawing.Image)
        Me.cmdCopiarRuta.Location = New System.Drawing.Point(477, 494)
        Me.cmdCopiarRuta.Name = "cmdCopiarRuta"
        Me.cmdCopiarRuta.Size = New System.Drawing.Size(103, 41)
        Me.cmdCopiarRuta.TabIndex = 48
        Me.cmdCopiarRuta.Text = "Ruta (F3)"
        Me.cmdCopiarRuta.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCopiarRuta.UseVisualStyleBackColor = True
        '
        'cmdCopiarID
        '
        Me.cmdCopiarID.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCopiarID.Image = CType(resources.GetObject("cmdCopiarID.Image"), System.Drawing.Image)
        Me.cmdCopiarID.Location = New System.Drawing.Point(477, 447)
        Me.cmdCopiarID.Name = "cmdCopiarID"
        Me.cmdCopiarID.Size = New System.Drawing.Size(103, 41)
        Me.cmdCopiarID.TabIndex = 47
        Me.cmdCopiarID.Text = "ID (F2)"
        Me.cmdCopiarID.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCopiarID.UseVisualStyleBackColor = True
        '
        'cmdRechazarOperacion
        '
        Me.cmdRechazarOperacion.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdRechazarOperacion.Image = CType(resources.GetObject("cmdRechazarOperacion.Image"), System.Drawing.Image)
        Me.cmdRechazarOperacion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdRechazarOperacion.Location = New System.Drawing.Point(480, 347)
        Me.cmdRechazarOperacion.Name = "cmdRechazarOperacion"
        Me.cmdRechazarOperacion.Size = New System.Drawing.Size(100, 43)
        Me.cmdRechazarOperacion.TabIndex = 61
        Me.cmdRechazarOperacion.Text = "Rechazar"
        Me.cmdRechazarOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdRechazarOperacion.UseVisualStyleBackColor = True
        '
        'cmdAnularOperacion
        '
        Me.cmdAnularOperacion.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAnularOperacion.Image = CType(resources.GetObject("cmdAnularOperacion.Image"), System.Drawing.Image)
        Me.cmdAnularOperacion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdAnularOperacion.Location = New System.Drawing.Point(480, 300)
        Me.cmdAnularOperacion.Name = "cmdAnularOperacion"
        Me.cmdAnularOperacion.Size = New System.Drawing.Size(100, 43)
        Me.cmdAnularOperacion.TabIndex = 60
        Me.cmdAnularOperacion.Text = "Anular"
        Me.cmdAnularOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAnularOperacion.UseVisualStyleBackColor = True
        '
        'cmdIncidentarOperacion
        '
        Me.cmdIncidentarOperacion.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdIncidentarOperacion.Image = CType(resources.GetObject("cmdIncidentarOperacion.Image"), System.Drawing.Image)
        Me.cmdIncidentarOperacion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdIncidentarOperacion.Location = New System.Drawing.Point(480, 253)
        Me.cmdIncidentarOperacion.Name = "cmdIncidentarOperacion"
        Me.cmdIncidentarOperacion.Size = New System.Drawing.Size(100, 43)
        Me.cmdIncidentarOperacion.TabIndex = 59
        Me.cmdIncidentarOperacion.Text = "Incidentar"
        Me.cmdIncidentarOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdIncidentarOperacion.UseVisualStyleBackColor = True
        '
        'cmdAceptarOperacion
        '
        Me.cmdAceptarOperacion.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAceptarOperacion.Image = CType(resources.GetObject("cmdAceptarOperacion.Image"), System.Drawing.Image)
        Me.cmdAceptarOperacion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdAceptarOperacion.Location = New System.Drawing.Point(480, 124)
        Me.cmdAceptarOperacion.Name = "cmdAceptarOperacion"
        Me.cmdAceptarOperacion.Size = New System.Drawing.Size(100, 43)
        Me.cmdAceptarOperacion.TabIndex = 57
        Me.cmdAceptarOperacion.Text = "Clasificar (F5)"
        Me.cmdAceptarOperacion.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.cmdAceptarOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAceptarOperacion.UseVisualStyleBackColor = True
        '
        'cmdRecuperarSiguteOperacion
        '
        Me.cmdRecuperarSiguteOperacion.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdRecuperarSiguteOperacion.Image = CType(resources.GetObject("cmdRecuperarSiguteOperacion.Image"), System.Drawing.Image)
        Me.cmdRecuperarSiguteOperacion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdRecuperarSiguteOperacion.Location = New System.Drawing.Point(480, 21)
        Me.cmdRecuperarSiguteOperacion.Name = "cmdRecuperarSiguteOperacion"
        Me.cmdRecuperarSiguteOperacion.Size = New System.Drawing.Size(100, 74)
        Me.cmdRecuperarSiguteOperacion.TabIndex = 56
        Me.cmdRecuperarSiguteOperacion.Text = "Recuperar Siguiente" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Doc (F1)"
        Me.cmdRecuperarSiguteOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdRecuperarSiguteOperacion.UseVisualStyleBackColor = True
        '
        'Label2
        '
        Me.Label2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(4, 710)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(152, 13)
        Me.Label2.TabIndex = 62
        Me.Label2.Text = "Comentario sobre la Operación"
        '
        'cmdAceptarYCerrar
        '
        Me.cmdAceptarYCerrar.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAceptarYCerrar.Image = CType(resources.GetObject("cmdAceptarYCerrar.Image"), System.Drawing.Image)
        Me.cmdAceptarYCerrar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdAceptarYCerrar.Location = New System.Drawing.Point(480, 173)
        Me.cmdAceptarYCerrar.Name = "cmdAceptarYCerrar"
        Me.cmdAceptarYCerrar.Size = New System.Drawing.Size(100, 43)
        Me.cmdAceptarYCerrar.TabIndex = 63
        Me.cmdAceptarYCerrar.Text = "Aceptar y Cerrar (F6)"
        Me.cmdAceptarYCerrar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAceptarYCerrar.UseVisualStyleBackColor = True
        '
        'chkMultiseleccion
        '
        Me.chkMultiseleccion.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.chkMultiseleccion.AutoSize = True
        Me.chkMultiseleccion.Location = New System.Drawing.Point(313, 432)
        Me.chkMultiseleccion.Name = "chkMultiseleccion"
        Me.chkMultiseleccion.Size = New System.Drawing.Size(93, 17)
        Me.chkMultiseleccion.TabIndex = 64
        Me.chkMultiseleccion.Text = "Multiselección"
        Me.chkMultiseleccion.UseVisualStyleBackColor = True
        '
        'ComboBox1
        '
        Me.ComboBox1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ComboBox1.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest
        Me.ComboBox1.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems
        Me.ComboBox1.FormattingEnabled = True
        Me.ComboBox1.Location = New System.Drawing.Point(4, 546)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(392, 21)
        Me.ComboBox1.TabIndex = 65
        '
        'Button1
        '
        Me.Button1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button1.Image = CType(resources.GetObject("Button1.Image"), System.Drawing.Image)
        Me.Button1.Location = New System.Drawing.Point(427, 546)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(25, 23)
        Me.Button1.TabIndex = 66
        Me.Button1.UseVisualStyleBackColor = True
        '
        'DataGridView1
        '
        Me.DataGridView1.AllowUserToAddRows = False
        Me.DataGridView1.AllowUserToDeleteRows = False
        DataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(CType(CType(192, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.DataGridView1.AlternatingRowsDefaultCellStyle = DataGridViewCellStyle1
        Me.DataGridView1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridView1.BackgroundColor = System.Drawing.SystemColors.ControlLightLight
        Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridView1.Location = New System.Drawing.Point(4, 572)
        Me.DataGridView1.MultiSelect = False
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.Size = New System.Drawing.Size(467, 126)
        Me.DataGridView1.TabIndex = 67
        '
        'Button2
        '
        Me.Button2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button2.Image = CType(resources.GetObject("Button2.Image"), System.Drawing.Image)
        Me.Button2.Location = New System.Drawing.Point(398, 546)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(25, 23)
        Me.Button2.TabIndex = 68
        Me.Button2.UseVisualStyleBackColor = True
        '
        'ctrlClasificar
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.DataGridView1)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.ComboBox1)
        Me.Controls.Add(Me.chkMultiseleccion)
        Me.Controls.Add(Me.cmdAceptarYCerrar)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.cmdRechazarOperacion)
        Me.Controls.Add(Me.cmdAnularOperacion)
        Me.Controls.Add(Me.cmdIncidentarOperacion)
        Me.Controls.Add(Me.cmdAceptarOperacion)
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
        Me.Controls.Add(Me.SplitContainer1)
        Me.Name = "ctrlClasificar"
        Me.Size = New System.Drawing.Size(583, 779)
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        Me.SplitContainer1.Panel2.PerformLayout()
        Me.SplitContainer1.ResumeLayout(False)
        Me.SelectorCargaTab.ResumeLayout(False)
        Me.tpCanales.ResumeLayout(False)
        Me.tpCanales.PerformLayout()
        Me.tpTipoEntNeg.ResumeLayout(False)
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        CType(Me.dgvEntidades, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents SplitContainer1 As System.Windows.Forms.SplitContainer
    Friend WithEvents lstCanales As System.Windows.Forms.ListBox
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents ArbolNododeT1 As ControlesPGenericos.ArbolTConBusqueda
    Friend WithEvents cmdEliminarEntidad As System.Windows.Forms.Button
    Friend WithEvents dgvEntidades As System.Windows.Forms.DataGridView
    Friend WithEvents lblDocumentoCargado As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents txtvComentarioOperacion As ControlesPBase.txtValidable
    Friend WithEvents cmdAbrir As System.Windows.Forms.Button
    Friend WithEvents cmdCopiarRuta As System.Windows.Forms.Button
    Friend WithEvents cmdCopiarID As System.Windows.Forms.Button
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents cmdRechazarOperacion As System.Windows.Forms.Button
    Friend WithEvents cmdAnularOperacion As System.Windows.Forms.Button
    Friend WithEvents cmdIncidentarOperacion As System.Windows.Forms.Button
    Friend WithEvents cmdAceptarOperacion As System.Windows.Forms.Button
    Friend WithEvents cmdRecuperarSiguteOperacion As System.Windows.Forms.Button
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents chkCanales As System.Windows.Forms.CheckBox
    Friend WithEvents cmdAceptarYCerrar As System.Windows.Forms.Button
    Friend WithEvents chkMultiseleccion As System.Windows.Forms.CheckBox
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents DataGridView1 As System.Windows.Forms.DataGridView
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents SelectorCargaTab As System.Windows.Forms.TabControl
    Friend WithEvents tpCanales As System.Windows.Forms.TabPage
    Friend WithEvents tpTipoEntNeg As System.Windows.Forms.TabPage
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents cmdSeleccionarArbol As System.Windows.Forms.Button
    Friend WithEvents lblBusquedaArbol As System.Windows.Forms.Label
    Friend WithEvents Label7 As System.Windows.Forms.Label

End Class
