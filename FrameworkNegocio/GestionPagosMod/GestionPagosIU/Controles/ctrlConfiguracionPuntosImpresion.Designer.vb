<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlConfiguracionPuntosImpresion
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ctrlConfiguracionPuntosImpresion))
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle
        Me.GroupBox6 = New System.Windows.Forms.GroupBox
        Me.GroupBox5 = New System.Windows.Forms.GroupBox
        Me.SignoGX = New System.Windows.Forms.Label
        Me.SignoGY = New System.Windows.Forms.Label
        Me.txtGeneralY = New ControlesPBase.txtValidable
        Me.Label31 = New System.Windows.Forms.Label
        Me.Label32 = New System.Windows.Forms.Label
        Me.txtGeneralX = New ControlesPBase.txtValidable
        Me.Label33 = New System.Windows.Forms.Label
        Me.Label34 = New System.Windows.Forms.Label
        Me.GroupBox4 = New System.Windows.Forms.GroupBox
        Me.txtFechaY = New ControlesPBase.txtValidable
        Me.Label27 = New System.Windows.Forms.Label
        Me.Label28 = New System.Windows.Forms.Label
        Me.txtFechaX = New ControlesPBase.txtValidable
        Me.Label29 = New System.Windows.Forms.Label
        Me.Label30 = New System.Windows.Forms.Label
        Me.GroupBox1 = New System.Windows.Forms.GroupBox
        Me.txtCantidadY = New ControlesPBase.txtValidable
        Me.Label17 = New System.Windows.Forms.Label
        Me.Label18 = New System.Windows.Forms.Label
        Me.txtCantidadX = New ControlesPBase.txtValidable
        Me.Label6 = New System.Windows.Forms.Label
        Me.Label5 = New System.Windows.Forms.Label
        Me.GroupBox3 = New System.Windows.Forms.GroupBox
        Me.txtDestinatarioY = New ControlesPBase.txtValidable
        Me.Label23 = New System.Windows.Forms.Label
        Me.Label24 = New System.Windows.Forms.Label
        Me.txtDestinatarioX = New ControlesPBase.txtValidable
        Me.Label25 = New System.Windows.Forms.Label
        Me.Label26 = New System.Windows.Forms.Label
        Me.GroupBox2 = New System.Windows.Forms.GroupBox
        Me.txtCantidadLetrasY = New ControlesPBase.txtValidable
        Me.Label19 = New System.Windows.Forms.Label
        Me.Label20 = New System.Windows.Forms.Label
        Me.txtCantidadLetrasX = New ControlesPBase.txtValidable
        Me.Label21 = New System.Windows.Forms.Label
        Me.Label22 = New System.Windows.Forms.Label
        Me.cmdConfiguracionPagina = New System.Windows.Forms.Button
        Me.Label1 = New System.Windows.Forms.Label
        Me.txtNombre = New System.Windows.Forms.TextBox
        Me.cmdReset = New System.Windows.Forms.Button
        Me.PageSetupDialog1 = New System.Windows.Forms.PageSetupDialog
        Me.FontDialog1 = New System.Windows.Forms.FontDialog
        Me.Label2 = New System.Windows.Forms.Label
        Me.txtFuente = New System.Windows.Forms.TextBox
        Me.cmdFuente = New System.Windows.Forms.Button
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog
        Me.DataGridView1 = New System.Windows.Forms.DataGridView
        Me.cmdEliminarImagen = New System.Windows.Forms.Button
        Me.cmdAgregarImagen = New System.Windows.Forms.Button
        Me.GroupBox7 = New System.Windows.Forms.GroupBox
        Me.cmdNavegarImagen = New System.Windows.Forms.Button
        Me.GroupBox6.SuspendLayout()
        Me.GroupBox5.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox7.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox6
        '
        Me.GroupBox6.Controls.Add(Me.GroupBox5)
        Me.GroupBox6.Controls.Add(Me.GroupBox4)
        Me.GroupBox6.Controls.Add(Me.GroupBox1)
        Me.GroupBox6.Controls.Add(Me.GroupBox3)
        Me.GroupBox6.Controls.Add(Me.GroupBox2)
        Me.GroupBox6.Location = New System.Drawing.Point(4, 304)
        Me.GroupBox6.Name = "GroupBox6"
        Me.GroupBox6.Size = New System.Drawing.Size(634, 100)
        Me.GroupBox6.TabIndex = 36
        Me.GroupBox6.TabStop = False
        Me.GroupBox6.Text = "Puntos de Impresión del Cheque"
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.SignoGX)
        Me.GroupBox5.Controls.Add(Me.SignoGY)
        Me.GroupBox5.Controls.Add(Me.txtGeneralY)
        Me.GroupBox5.Controls.Add(Me.Label31)
        Me.GroupBox5.Controls.Add(Me.Label32)
        Me.GroupBox5.Controls.Add(Me.txtGeneralX)
        Me.GroupBox5.Controls.Add(Me.Label33)
        Me.GroupBox5.Controls.Add(Me.Label34)
        Me.GroupBox5.Location = New System.Drawing.Point(6, 16)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(119, 78)
        Me.GroupBox5.TabIndex = 31
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "Ajustes Desviación"
        '
        'SignoGX
        '
        Me.SignoGX.AutoSize = True
        Me.SignoGX.BackColor = System.Drawing.Color.LightSteelBlue
        Me.SignoGX.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.SignoGX.ForeColor = System.Drawing.Color.White
        Me.SignoGX.Location = New System.Drawing.Point(26, 24)
        Me.SignoGX.Name = "SignoGX"
        Me.SignoGX.Size = New System.Drawing.Size(23, 15)
        Me.SignoGX.TabIndex = 35
        Me.SignoGX.Text = "+/-"
        '
        'SignoGY
        '
        Me.SignoGY.AutoSize = True
        Me.SignoGY.BackColor = System.Drawing.Color.LightSteelBlue
        Me.SignoGY.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.SignoGY.ForeColor = System.Drawing.Color.White
        Me.SignoGY.Location = New System.Drawing.Point(26, 50)
        Me.SignoGY.Name = "SignoGY"
        Me.SignoGY.Size = New System.Drawing.Size(23, 15)
        Me.SignoGY.TabIndex = 34
        Me.SignoGY.Text = "+/-"
        '
        'txtGeneralY
        '
        Me.txtGeneralY.Location = New System.Drawing.Point(54, 48)
        Me.txtGeneralY.MensajeErrorValidacion = Nothing
        Me.txtGeneralY.Name = "txtGeneralY"
        Me.txtGeneralY.Size = New System.Drawing.Size(39, 20)
        Me.txtGeneralY.SoloDouble = True
        Me.txtGeneralY.TabIndex = 13
        Me.txtGeneralY.Text = "0"
        Me.txtGeneralY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtGeneralY.ToolTipText = Nothing
        Me.txtGeneralY.TrimText = False
        '
        'Label31
        '
        Me.Label31.AutoSize = True
        Me.Label31.Location = New System.Drawing.Point(10, 50)
        Me.Label31.Name = "Label31"
        Me.Label31.Size = New System.Drawing.Size(14, 13)
        Me.Label31.TabIndex = 12
        Me.Label31.Text = "Y"
        '
        'Label32
        '
        Me.Label32.AutoSize = True
        Me.Label32.Location = New System.Drawing.Point(94, 50)
        Me.Label32.Name = "Label32"
        Me.Label32.Size = New System.Drawing.Size(23, 13)
        Me.Label32.TabIndex = 14
        Me.Label32.Text = "mm"
        '
        'txtGeneralX
        '
        Me.txtGeneralX.Location = New System.Drawing.Point(54, 22)
        Me.txtGeneralX.MensajeErrorValidacion = Nothing
        Me.txtGeneralX.Name = "txtGeneralX"
        Me.txtGeneralX.Size = New System.Drawing.Size(39, 20)
        Me.txtGeneralX.SoloDouble = True
        Me.txtGeneralX.TabIndex = 10
        Me.txtGeneralX.Text = "0"
        Me.txtGeneralX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtGeneralX.ToolTipText = Nothing
        Me.txtGeneralX.TrimText = False
        '
        'Label33
        '
        Me.Label33.AutoSize = True
        Me.Label33.Location = New System.Drawing.Point(10, 24)
        Me.Label33.Name = "Label33"
        Me.Label33.Size = New System.Drawing.Size(14, 13)
        Me.Label33.TabIndex = 9
        Me.Label33.Text = "X"
        '
        'Label34
        '
        Me.Label34.AutoSize = True
        Me.Label34.Location = New System.Drawing.Point(94, 24)
        Me.Label34.Name = "Label34"
        Me.Label34.Size = New System.Drawing.Size(23, 13)
        Me.Label34.TabIndex = 11
        Me.Label34.Text = "mm"
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.txtFechaY)
        Me.GroupBox4.Controls.Add(Me.Label27)
        Me.GroupBox4.Controls.Add(Me.Label28)
        Me.GroupBox4.Controls.Add(Me.txtFechaX)
        Me.GroupBox4.Controls.Add(Me.Label29)
        Me.GroupBox4.Controls.Add(Me.Label30)
        Me.GroupBox4.Location = New System.Drawing.Point(506, 16)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(119, 78)
        Me.GroupBox4.TabIndex = 30
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "Fecha"
        '
        'txtFechaY
        '
        Me.txtFechaY.Location = New System.Drawing.Point(37, 48)
        Me.txtFechaY.MensajeErrorValidacion = Nothing
        Me.txtFechaY.Name = "txtFechaY"
        Me.txtFechaY.Size = New System.Drawing.Size(43, 20)
        Me.txtFechaY.SoloDouble = True
        Me.txtFechaY.TabIndex = 13
        Me.txtFechaY.Text = "0"
        Me.txtFechaY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtFechaY.ToolTipText = Nothing
        Me.txtFechaY.TrimText = False
        '
        'Label27
        '
        Me.Label27.AutoSize = True
        Me.Label27.Location = New System.Drawing.Point(10, 50)
        Me.Label27.Name = "Label27"
        Me.Label27.Size = New System.Drawing.Size(14, 13)
        Me.Label27.TabIndex = 12
        Me.Label27.Text = "Y"
        '
        'Label28
        '
        Me.Label28.AutoSize = True
        Me.Label28.Location = New System.Drawing.Point(82, 50)
        Me.Label28.Name = "Label28"
        Me.Label28.Size = New System.Drawing.Size(23, 13)
        Me.Label28.TabIndex = 14
        Me.Label28.Text = "mm"
        '
        'txtFechaX
        '
        Me.txtFechaX.Location = New System.Drawing.Point(37, 22)
        Me.txtFechaX.MensajeErrorValidacion = Nothing
        Me.txtFechaX.Name = "txtFechaX"
        Me.txtFechaX.Size = New System.Drawing.Size(43, 20)
        Me.txtFechaX.SoloDouble = True
        Me.txtFechaX.TabIndex = 10
        Me.txtFechaX.Text = "0"
        Me.txtFechaX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtFechaX.ToolTipText = Nothing
        Me.txtFechaX.TrimText = False
        '
        'Label29
        '
        Me.Label29.AutoSize = True
        Me.Label29.Location = New System.Drawing.Point(10, 24)
        Me.Label29.Name = "Label29"
        Me.Label29.Size = New System.Drawing.Size(14, 13)
        Me.Label29.TabIndex = 9
        Me.Label29.Text = "X"
        '
        'Label30
        '
        Me.Label30.AutoSize = True
        Me.Label30.Location = New System.Drawing.Point(82, 24)
        Me.Label30.Name = "Label30"
        Me.Label30.Size = New System.Drawing.Size(23, 13)
        Me.Label30.TabIndex = 11
        Me.Label30.Text = "mm"
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.txtCantidadY)
        Me.GroupBox1.Controls.Add(Me.Label17)
        Me.GroupBox1.Controls.Add(Me.Label18)
        Me.GroupBox1.Controls.Add(Me.txtCantidadX)
        Me.GroupBox1.Controls.Add(Me.Label6)
        Me.GroupBox1.Controls.Add(Me.Label5)
        Me.GroupBox1.Location = New System.Drawing.Point(131, 16)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(119, 78)
        Me.GroupBox1.TabIndex = 27
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Cantidad"
        '
        'txtCantidadY
        '
        Me.txtCantidadY.Location = New System.Drawing.Point(33, 48)
        Me.txtCantidadY.MensajeErrorValidacion = Nothing
        Me.txtCantidadY.Name = "txtCantidadY"
        Me.txtCantidadY.Size = New System.Drawing.Size(48, 20)
        Me.txtCantidadY.SoloDouble = True
        Me.txtCantidadY.TabIndex = 13
        Me.txtCantidadY.Text = "0"
        Me.txtCantidadY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtCantidadY.ToolTipText = Nothing
        Me.txtCantidadY.TrimText = False
        '
        'Label17
        '
        Me.Label17.AutoSize = True
        Me.Label17.Location = New System.Drawing.Point(10, 50)
        Me.Label17.Name = "Label17"
        Me.Label17.Size = New System.Drawing.Size(14, 13)
        Me.Label17.TabIndex = 12
        Me.Label17.Text = "Y"
        '
        'Label18
        '
        Me.Label18.AutoSize = True
        Me.Label18.Location = New System.Drawing.Point(82, 50)
        Me.Label18.Name = "Label18"
        Me.Label18.Size = New System.Drawing.Size(23, 13)
        Me.Label18.TabIndex = 14
        Me.Label18.Text = "mm"
        '
        'txtCantidadX
        '
        Me.txtCantidadX.Location = New System.Drawing.Point(33, 22)
        Me.txtCantidadX.MensajeErrorValidacion = Nothing
        Me.txtCantidadX.Name = "txtCantidadX"
        Me.txtCantidadX.Size = New System.Drawing.Size(48, 20)
        Me.txtCantidadX.SoloDouble = True
        Me.txtCantidadX.TabIndex = 10
        Me.txtCantidadX.Text = "0"
        Me.txtCantidadX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtCantidadX.ToolTipText = Nothing
        Me.txtCantidadX.TrimText = False
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(10, 24)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(14, 13)
        Me.Label6.TabIndex = 9
        Me.Label6.Text = "X"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(82, 24)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(23, 13)
        Me.Label5.TabIndex = 11
        Me.Label5.Text = "mm"
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.txtDestinatarioY)
        Me.GroupBox3.Controls.Add(Me.Label23)
        Me.GroupBox3.Controls.Add(Me.Label24)
        Me.GroupBox3.Controls.Add(Me.txtDestinatarioX)
        Me.GroupBox3.Controls.Add(Me.Label25)
        Me.GroupBox3.Controls.Add(Me.Label26)
        Me.GroupBox3.Location = New System.Drawing.Point(256, 16)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(119, 78)
        Me.GroupBox3.TabIndex = 29
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Destinatario"
        '
        'txtDestinatarioY
        '
        Me.txtDestinatarioY.Location = New System.Drawing.Point(35, 48)
        Me.txtDestinatarioY.MensajeErrorValidacion = Nothing
        Me.txtDestinatarioY.Name = "txtDestinatarioY"
        Me.txtDestinatarioY.Size = New System.Drawing.Size(47, 20)
        Me.txtDestinatarioY.SoloDouble = True
        Me.txtDestinatarioY.TabIndex = 13
        Me.txtDestinatarioY.Text = "0"
        Me.txtDestinatarioY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtDestinatarioY.ToolTipText = Nothing
        Me.txtDestinatarioY.TrimText = False
        '
        'Label23
        '
        Me.Label23.AutoSize = True
        Me.Label23.Location = New System.Drawing.Point(10, 50)
        Me.Label23.Name = "Label23"
        Me.Label23.Size = New System.Drawing.Size(14, 13)
        Me.Label23.TabIndex = 12
        Me.Label23.Text = "Y"
        '
        'Label24
        '
        Me.Label24.AutoSize = True
        Me.Label24.Location = New System.Drawing.Point(83, 50)
        Me.Label24.Name = "Label24"
        Me.Label24.Size = New System.Drawing.Size(23, 13)
        Me.Label24.TabIndex = 14
        Me.Label24.Text = "mm"
        '
        'txtDestinatarioX
        '
        Me.txtDestinatarioX.Location = New System.Drawing.Point(35, 22)
        Me.txtDestinatarioX.MensajeErrorValidacion = Nothing
        Me.txtDestinatarioX.Name = "txtDestinatarioX"
        Me.txtDestinatarioX.Size = New System.Drawing.Size(47, 20)
        Me.txtDestinatarioX.SoloDouble = True
        Me.txtDestinatarioX.TabIndex = 10
        Me.txtDestinatarioX.Text = "0"
        Me.txtDestinatarioX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtDestinatarioX.ToolTipText = Nothing
        Me.txtDestinatarioX.TrimText = False
        '
        'Label25
        '
        Me.Label25.AutoSize = True
        Me.Label25.Location = New System.Drawing.Point(10, 24)
        Me.Label25.Name = "Label25"
        Me.Label25.Size = New System.Drawing.Size(14, 13)
        Me.Label25.TabIndex = 9
        Me.Label25.Text = "X"
        '
        'Label26
        '
        Me.Label26.AutoSize = True
        Me.Label26.Location = New System.Drawing.Point(83, 24)
        Me.Label26.Name = "Label26"
        Me.Label26.Size = New System.Drawing.Size(23, 13)
        Me.Label26.TabIndex = 11
        Me.Label26.Text = "mm"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.txtCantidadLetrasY)
        Me.GroupBox2.Controls.Add(Me.Label19)
        Me.GroupBox2.Controls.Add(Me.Label20)
        Me.GroupBox2.Controls.Add(Me.txtCantidadLetrasX)
        Me.GroupBox2.Controls.Add(Me.Label21)
        Me.GroupBox2.Controls.Add(Me.Label22)
        Me.GroupBox2.Location = New System.Drawing.Point(381, 16)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(119, 78)
        Me.GroupBox2.TabIndex = 28
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Cantidad En Letras"
        '
        'txtCantidadLetrasY
        '
        Me.txtCantidadLetrasY.Location = New System.Drawing.Point(32, 48)
        Me.txtCantidadLetrasY.MensajeErrorValidacion = Nothing
        Me.txtCantidadLetrasY.Name = "txtCantidadLetrasY"
        Me.txtCantidadLetrasY.Size = New System.Drawing.Size(48, 20)
        Me.txtCantidadLetrasY.SoloDouble = True
        Me.txtCantidadLetrasY.TabIndex = 13
        Me.txtCantidadLetrasY.Text = "0"
        Me.txtCantidadLetrasY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtCantidadLetrasY.ToolTipText = Nothing
        Me.txtCantidadLetrasY.TrimText = False
        '
        'Label19
        '
        Me.Label19.AutoSize = True
        Me.Label19.Location = New System.Drawing.Point(10, 50)
        Me.Label19.Name = "Label19"
        Me.Label19.Size = New System.Drawing.Size(14, 13)
        Me.Label19.TabIndex = 12
        Me.Label19.Text = "Y"
        '
        'Label20
        '
        Me.Label20.AutoSize = True
        Me.Label20.Location = New System.Drawing.Point(82, 50)
        Me.Label20.Name = "Label20"
        Me.Label20.Size = New System.Drawing.Size(23, 13)
        Me.Label20.TabIndex = 14
        Me.Label20.Text = "mm"
        '
        'txtCantidadLetrasX
        '
        Me.txtCantidadLetrasX.Location = New System.Drawing.Point(32, 22)
        Me.txtCantidadLetrasX.MensajeErrorValidacion = Nothing
        Me.txtCantidadLetrasX.Name = "txtCantidadLetrasX"
        Me.txtCantidadLetrasX.Size = New System.Drawing.Size(47, 20)
        Me.txtCantidadLetrasX.SoloDouble = True
        Me.txtCantidadLetrasX.TabIndex = 10
        Me.txtCantidadLetrasX.Text = "0"
        Me.txtCantidadLetrasX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtCantidadLetrasX.ToolTipText = Nothing
        Me.txtCantidadLetrasX.TrimText = False
        '
        'Label21
        '
        Me.Label21.AutoSize = True
        Me.Label21.Location = New System.Drawing.Point(10, 24)
        Me.Label21.Name = "Label21"
        Me.Label21.Size = New System.Drawing.Size(14, 13)
        Me.Label21.TabIndex = 9
        Me.Label21.Text = "X"
        '
        'Label22
        '
        Me.Label22.AutoSize = True
        Me.Label22.Location = New System.Drawing.Point(82, 24)
        Me.Label22.Name = "Label22"
        Me.Label22.Size = New System.Drawing.Size(23, 13)
        Me.Label22.TabIndex = 11
        Me.Label22.Text = "mm"
        '
        'cmdConfiguracionPagina
        '
        Me.cmdConfiguracionPagina.Image = CType(resources.GetObject("cmdConfiguracionPagina.Image"), System.Drawing.Image)
        Me.cmdConfiguracionPagina.Location = New System.Drawing.Point(452, 56)
        Me.cmdConfiguracionPagina.Name = "cmdConfiguracionPagina"
        Me.cmdConfiguracionPagina.Size = New System.Drawing.Size(177, 34)
        Me.cmdConfiguracionPagina.TabIndex = 37
        Me.cmdConfiguracionPagina.Text = "Configuracion de Página"
        Me.cmdConfiguracionPagina.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdConfiguracionPagina.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(9, 24)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(138, 13)
        Me.Label1.TabIndex = 38
        Me.Label1.Text = "Nombre de la Configuración"
        '
        'txtNombre
        '
        Me.txtNombre.Location = New System.Drawing.Point(152, 21)
        Me.txtNombre.Name = "txtNombre"
        Me.txtNombre.Size = New System.Drawing.Size(258, 20)
        Me.txtNombre.TabIndex = 39
        '
        'cmdReset
        '
        Me.cmdReset.Image = CType(resources.GetObject("cmdReset.Image"), System.Drawing.Image)
        Me.cmdReset.Location = New System.Drawing.Point(438, 410)
        Me.cmdReset.Name = "cmdReset"
        Me.cmdReset.Size = New System.Drawing.Size(200, 23)
        Me.cmdReset.TabIndex = 40
        Me.cmdReset.Text = "Restablecer Valores por Defecto"
        Me.cmdReset.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdReset.UseVisualStyleBackColor = True
        '
        'PageSetupDialog1
        '
        Me.PageSetupDialog1.AllowPrinter = False
        Me.PageSetupDialog1.EnableMetric = True
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(9, 67)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(40, 13)
        Me.Label2.TabIndex = 41
        Me.Label2.Text = "Fuente"
        '
        'txtFuente
        '
        Me.txtFuente.Location = New System.Drawing.Point(54, 64)
        Me.txtFuente.Name = "txtFuente"
        Me.txtFuente.ReadOnly = True
        Me.txtFuente.Size = New System.Drawing.Size(315, 20)
        Me.txtFuente.TabIndex = 42
        '
        'cmdFuente
        '
        Me.cmdFuente.Image = CType(resources.GetObject("cmdFuente.Image"), System.Drawing.Image)
        Me.cmdFuente.Location = New System.Drawing.Point(379, 60)
        Me.cmdFuente.Name = "cmdFuente"
        Me.cmdFuente.Size = New System.Drawing.Size(31, 26)
        Me.cmdFuente.TabIndex = 43
        Me.cmdFuente.UseVisualStyleBackColor = True
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.Filter = "Imagen GIF|*.gif|Imagen JPG|*.jpg|Imagen JPEG|*.jpeg|Imagenes PNG|*.png|Todos los" & _
            " archivos|*.*"
        Me.OpenFileDialog1.Title = "Seleccionar Imagen embebida"
        '
        'DataGridView1
        '
        Me.DataGridView1.AllowUserToAddRows = False
        Me.DataGridView1.AllowUserToDeleteRows = False
        Me.DataGridView1.BackgroundColor = System.Drawing.Color.White
        Me.DataGridView1.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None
        Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        DataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window
        DataGridViewCellStyle1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText
        DataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.LightCyan
        DataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.Black
        DataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
        Me.DataGridView1.DefaultCellStyle = DataGridViewCellStyle1
        Me.DataGridView1.Location = New System.Drawing.Point(7, 40)
        Me.DataGridView1.MultiSelect = False
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.ReadOnly = True
        Me.DataGridView1.RowHeadersVisible = False
        Me.DataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.DataGridView1.Size = New System.Drawing.Size(621, 140)
        Me.DataGridView1.TabIndex = 44
        '
        'cmdEliminarImagen
        '
        Me.cmdEliminarImagen.Image = CType(resources.GetObject("cmdEliminarImagen.Image"), System.Drawing.Image)
        Me.cmdEliminarImagen.Location = New System.Drawing.Point(595, 11)
        Me.cmdEliminarImagen.Name = "cmdEliminarImagen"
        Me.cmdEliminarImagen.Size = New System.Drawing.Size(33, 23)
        Me.cmdEliminarImagen.TabIndex = 46
        Me.cmdEliminarImagen.UseVisualStyleBackColor = True
        '
        'cmdAgregarImagen
        '
        Me.cmdAgregarImagen.Image = CType(resources.GetObject("cmdAgregarImagen.Image"), System.Drawing.Image)
        Me.cmdAgregarImagen.Location = New System.Drawing.Point(556, 11)
        Me.cmdAgregarImagen.Name = "cmdAgregarImagen"
        Me.cmdAgregarImagen.Size = New System.Drawing.Size(33, 23)
        Me.cmdAgregarImagen.TabIndex = 46
        Me.cmdAgregarImagen.UseVisualStyleBackColor = True
        '
        'GroupBox7
        '
        Me.GroupBox7.Controls.Add(Me.DataGridView1)
        Me.GroupBox7.Controls.Add(Me.cmdNavegarImagen)
        Me.GroupBox7.Controls.Add(Me.cmdAgregarImagen)
        Me.GroupBox7.Controls.Add(Me.cmdEliminarImagen)
        Me.GroupBox7.Location = New System.Drawing.Point(4, 102)
        Me.GroupBox7.Name = "GroupBox7"
        Me.GroupBox7.Size = New System.Drawing.Size(634, 186)
        Me.GroupBox7.TabIndex = 47
        Me.GroupBox7.TabStop = False
        Me.GroupBox7.Text = "Imágenes"
        '
        'cmdNavegarImagen
        '
        Me.cmdNavegarImagen.Image = CType(resources.GetObject("cmdNavegarImagen.Image"), System.Drawing.Image)
        Me.cmdNavegarImagen.Location = New System.Drawing.Point(519, 11)
        Me.cmdNavegarImagen.Name = "cmdNavegarImagen"
        Me.cmdNavegarImagen.Size = New System.Drawing.Size(33, 23)
        Me.cmdNavegarImagen.TabIndex = 46
        Me.cmdNavegarImagen.UseVisualStyleBackColor = True
        '
        'ctrlConfiguracionPuntosImpresion
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.GroupBox7)
        Me.Controls.Add(Me.cmdFuente)
        Me.Controls.Add(Me.txtFuente)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.cmdReset)
        Me.Controls.Add(Me.txtNombre)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.cmdConfiguracionPagina)
        Me.Controls.Add(Me.GroupBox6)
        Me.Name = "ctrlConfiguracionPuntosImpresion"
        Me.Size = New System.Drawing.Size(645, 448)
        Me.GroupBox6.ResumeLayout(False)
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox5.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox7.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents GroupBox6 As System.Windows.Forms.GroupBox
    Friend WithEvents GroupBox5 As System.Windows.Forms.GroupBox
    Friend WithEvents SignoGX As System.Windows.Forms.Label
    Friend WithEvents SignoGY As System.Windows.Forms.Label
    Friend WithEvents txtGeneralY As ControlesPBase.txtValidable
    Friend WithEvents Label31 As System.Windows.Forms.Label
    Friend WithEvents Label32 As System.Windows.Forms.Label
    Friend WithEvents txtGeneralX As ControlesPBase.txtValidable
    Friend WithEvents Label33 As System.Windows.Forms.Label
    Friend WithEvents Label34 As System.Windows.Forms.Label
    Friend WithEvents GroupBox4 As System.Windows.Forms.GroupBox
    Friend WithEvents txtFechaY As ControlesPBase.txtValidable
    Friend WithEvents Label27 As System.Windows.Forms.Label
    Friend WithEvents Label28 As System.Windows.Forms.Label
    Friend WithEvents txtFechaX As ControlesPBase.txtValidable
    Friend WithEvents Label29 As System.Windows.Forms.Label
    Friend WithEvents Label30 As System.Windows.Forms.Label
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents txtCantidadY As ControlesPBase.txtValidable
    Friend WithEvents Label17 As System.Windows.Forms.Label
    Friend WithEvents Label18 As System.Windows.Forms.Label
    Friend WithEvents txtCantidadX As ControlesPBase.txtValidable
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents GroupBox3 As System.Windows.Forms.GroupBox
    Friend WithEvents txtDestinatarioY As ControlesPBase.txtValidable
    Friend WithEvents Label23 As System.Windows.Forms.Label
    Friend WithEvents Label24 As System.Windows.Forms.Label
    Friend WithEvents txtDestinatarioX As ControlesPBase.txtValidable
    Friend WithEvents Label25 As System.Windows.Forms.Label
    Friend WithEvents Label26 As System.Windows.Forms.Label
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents txtCantidadLetrasY As ControlesPBase.txtValidable
    Friend WithEvents Label19 As System.Windows.Forms.Label
    Friend WithEvents Label20 As System.Windows.Forms.Label
    Friend WithEvents txtCantidadLetrasX As ControlesPBase.txtValidable
    Friend WithEvents Label21 As System.Windows.Forms.Label
    Friend WithEvents Label22 As System.Windows.Forms.Label
    Friend WithEvents cmdConfiguracionPagina As System.Windows.Forms.Button
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents txtNombre As System.Windows.Forms.TextBox
    Friend WithEvents cmdReset As System.Windows.Forms.Button
    Friend WithEvents PageSetupDialog1 As System.Windows.Forms.PageSetupDialog
    Friend WithEvents FontDialog1 As System.Windows.Forms.FontDialog
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents txtFuente As System.Windows.Forms.TextBox
    Friend WithEvents cmdFuente As System.Windows.Forms.Button
    Friend WithEvents OpenFileDialog1 As System.Windows.Forms.OpenFileDialog
    Friend WithEvents DataGridView1 As System.Windows.Forms.DataGridView
    Friend WithEvents cmdEliminarImagen As System.Windows.Forms.Button
    Friend WithEvents cmdAgregarImagen As System.Windows.Forms.Button
    Friend WithEvents GroupBox7 As System.Windows.Forms.GroupBox
    Friend WithEvents cmdNavegarImagen As System.Windows.Forms.Button

End Class
