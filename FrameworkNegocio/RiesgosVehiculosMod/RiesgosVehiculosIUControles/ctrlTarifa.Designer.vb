<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlTarifa
    Inherits MotorIU.ControlesP.BaseControlP

    'UserControl1 overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle
        Dim DataGridViewCellStyle2 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ctrlTarifa))
        Me.dtgProductos = New System.Windows.Forms.DataGridView
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer
        Me.cmdTarificar = New ControlesPBase.BotonP
        Me.Label4 = New System.Windows.Forms.Label
        Me.txtvImporte = New ControlesPBase.txtValidable
        Me.Label2 = New System.Windows.Forms.Label
        Me.Label1 = New System.Windows.Forms.Label
        Me.Label3 = New System.Windows.Forms.Label
        Me.dtgPagos = New System.Windows.Forms.DataGridView
        Me.Label5 = New System.Windows.Forms.Label
        Me.Label6 = New System.Windows.Forms.Label
        Me.Label7 = New System.Windows.Forms.Label
        Me.Label8 = New System.Windows.Forms.Label
        Me.dtpFechaEfecto = New System.Windows.Forms.DateTimePicker
        Me.lblAnosDiasMeses = New System.Windows.Forms.Label
        Me.lblRiesgo = New System.Windows.Forms.Label
        Me.cmdNavegarRiesgo = New System.Windows.Forms.Button
        Me.cmdConductoresAdicionales = New System.Windows.Forms.Button
        CType(Me.dtgProductos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        CType(Me.dtgPagos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'dtgProductos
        '
        Me.dtgProductos.AllowUserToAddRows = False
        Me.dtgProductos.AllowUserToDeleteRows = False
        DataGridViewCellStyle1.BackColor = System.Drawing.Color.Azure
        Me.dtgProductos.AlternatingRowsDefaultCellStyle = DataGridViewCellStyle1
        Me.dtgProductos.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dtgProductos.BackgroundColor = System.Drawing.Color.White
        Me.dtgProductos.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dtgProductos.Location = New System.Drawing.Point(12, 34)
        Me.dtgProductos.MultiSelect = False
        Me.dtgProductos.Name = "dtgProductos"
        Me.dtgProductos.RowHeadersVisible = False
        Me.dtgProductos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dtgProductos.Size = New System.Drawing.Size(402, 154)
        Me.dtgProductos.TabIndex = 0
        '
        'SplitContainer1
        '
        Me.SplitContainer1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.SplitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.SplitContainer1.Location = New System.Drawing.Point(3, 122)
        Me.SplitContainer1.Name = "SplitContainer1"
        Me.SplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal
        '
        'SplitContainer1.Panel1
        '
        Me.SplitContainer1.Panel1.Controls.Add(Me.cmdTarificar)
        Me.SplitContainer1.Panel1.Controls.Add(Me.Label4)
        Me.SplitContainer1.Panel1.Controls.Add(Me.txtvImporte)
        Me.SplitContainer1.Panel1.Controls.Add(Me.Label2)
        Me.SplitContainer1.Panel1.Controls.Add(Me.Label1)
        Me.SplitContainer1.Panel1.Controls.Add(Me.dtgProductos)
        '
        'SplitContainer1.Panel2
        '
        Me.SplitContainer1.Panel2.Controls.Add(Me.Label3)
        Me.SplitContainer1.Panel2.Controls.Add(Me.dtgPagos)
        Me.SplitContainer1.Size = New System.Drawing.Size(431, 420)
        Me.SplitContainer1.SplitterDistance = 243
        Me.SplitContainer1.TabIndex = 1
        '
        'cmdTarificar
        '
        Me.cmdTarificar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdTarificar.Location = New System.Drawing.Point(35, 201)
        Me.cmdTarificar.Name = "cmdTarificar"
        Me.cmdTarificar.Size = New System.Drawing.Size(88, 23)
        Me.cmdTarificar.TabIndex = 4
        Me.cmdTarificar.Text = "Tarificar"
        Me.cmdTarificar.UseVisualStyleBackColor = True
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(15, 7)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(55, 13)
        Me.Label4.TabIndex = 3
        Me.Label4.Text = "Productos"
        '
        'txtvImporte
        '
        Me.txtvImporte.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtvImporte.BackColor = System.Drawing.Color.White
        Me.txtvImporte.Location = New System.Drawing.Point(205, 203)
        Me.txtvImporte.MensajeErrorValidacion = Nothing
        Me.txtvImporte.Name = "txtvImporte"
        Me.txtvImporte.ReadOnly = True
        Me.txtvImporte.Size = New System.Drawing.Size(95, 20)
        Me.txtvImporte.TabIndex = 2
        Me.txtvImporte.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtvImporte.ToolTipText = Nothing
        Me.txtvImporte.TrimText = False
        '
        'Label2
        '
        Me.Label2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(306, 206)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(13, 13)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "€"
        '
        'Label1
        '
        Me.Label1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(142, 206)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(42, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Importe"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(12, 7)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(84, 13)
        Me.Label3.TabIndex = 1
        Me.Label3.Text = "Formas de Pago"
        '
        'dtgPagos
        '
        Me.dtgPagos.AllowUserToAddRows = False
        Me.dtgPagos.AllowUserToDeleteRows = False
        DataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.dtgPagos.AlternatingRowsDefaultCellStyle = DataGridViewCellStyle2
        Me.dtgPagos.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dtgPagos.BackgroundColor = System.Drawing.Color.White
        Me.dtgPagos.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dtgPagos.Location = New System.Drawing.Point(12, 31)
        Me.dtgPagos.Name = "dtgPagos"
        Me.dtgPagos.ReadOnly = True
        Me.dtgPagos.RowHeadersVisible = False
        Me.dtgPagos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dtgPagos.Size = New System.Drawing.Size(402, 123)
        Me.dtgPagos.TabIndex = 0
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(16, 16)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(85, 13)
        Me.Label5.TabIndex = 2
        Me.Label5.Text = "Fecha de efecto"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(10, 89)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(91, 13)
        Me.Label6.TabIndex = 3
        Me.Label6.Text = "Cond. adicionales"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(61, 54)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(40, 13)
        Me.Label7.TabIndex = 4
        Me.Label7.Text = "Riesgo"
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(257, 16)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(152, 13)
        Me.Label8.TabIndex = 5
        Me.Label8.Text = "Periodo de cobertura tarificado"
        '
        'dtpFechaEfecto
        '
        Me.dtpFechaEfecto.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpFechaEfecto.Location = New System.Drawing.Point(119, 12)
        Me.dtpFechaEfecto.Name = "dtpFechaEfecto"
        Me.dtpFechaEfecto.Size = New System.Drawing.Size(86, 20)
        Me.dtpFechaEfecto.TabIndex = 6
        '
        'lblAnosDiasMeses
        '
        Me.lblAnosDiasMeses.AutoSize = True
        Me.lblAnosDiasMeses.BackColor = System.Drawing.Color.White
        Me.lblAnosDiasMeses.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblAnosDiasMeses.Location = New System.Drawing.Point(415, 16)
        Me.lblAnosDiasMeses.Name = "lblAnosDiasMeses"
        Me.lblAnosDiasMeses.Size = New System.Drawing.Size(12, 15)
        Me.lblAnosDiasMeses.TabIndex = 8
        Me.lblAnosDiasMeses.Text = "-"
        '
        'lblRiesgo
        '
        Me.lblRiesgo.AutoSize = True
        Me.lblRiesgo.BackColor = System.Drawing.Color.White
        Me.lblRiesgo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblRiesgo.Location = New System.Drawing.Point(116, 54)
        Me.lblRiesgo.Name = "lblRiesgo"
        Me.lblRiesgo.Size = New System.Drawing.Size(12, 15)
        Me.lblRiesgo.TabIndex = 9
        Me.lblRiesgo.Text = "-"
        '
        'cmdNavegarRiesgo
        '
        Me.cmdNavegarRiesgo.Image = CType(resources.GetObject("cmdNavegarRiesgo.Image"), System.Drawing.Image)
        Me.cmdNavegarRiesgo.Location = New System.Drawing.Point(134, 49)
        Me.cmdNavegarRiesgo.Name = "cmdNavegarRiesgo"
        Me.cmdNavegarRiesgo.Size = New System.Drawing.Size(39, 23)
        Me.cmdNavegarRiesgo.TabIndex = 10
        Me.cmdNavegarRiesgo.UseVisualStyleBackColor = True
        '
        'cmdConductoresAdicionales
        '
        Me.cmdConductoresAdicionales.Image = Global.FN.RiesgosVehiculos.IU.Controles.My.Resources.Resources.view16
        Me.cmdConductoresAdicionales.Location = New System.Drawing.Point(119, 84)
        Me.cmdConductoresAdicionales.Name = "cmdConductoresAdicionales"
        Me.cmdConductoresAdicionales.Size = New System.Drawing.Size(39, 23)
        Me.cmdConductoresAdicionales.TabIndex = 7
        Me.cmdConductoresAdicionales.UseVisualStyleBackColor = True
        '
        'ctrlTarifa
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.cmdNavegarRiesgo)
        Me.Controls.Add(Me.lblRiesgo)
        Me.Controls.Add(Me.lblAnosDiasMeses)
        Me.Controls.Add(Me.cmdConductoresAdicionales)
        Me.Controls.Add(Me.dtpFechaEfecto)
        Me.Controls.Add(Me.Label8)
        Me.Controls.Add(Me.Label7)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.SplitContainer1)
        Me.Name = "ctrlTarifa"
        Me.Size = New System.Drawing.Size(437, 545)
        CType(Me.dtgProductos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel1.PerformLayout()
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        Me.SplitContainer1.Panel2.PerformLayout()
        Me.SplitContainer1.ResumeLayout(False)
        CType(Me.dtgPagos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents dtgProductos As System.Windows.Forms.DataGridView
    Friend WithEvents SplitContainer1 As System.Windows.Forms.SplitContainer
    Friend WithEvents txtvImporte As ControlesPBase.txtValidable
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents dtgPagos As System.Windows.Forms.DataGridView
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents dtpFechaEfecto As System.Windows.Forms.DateTimePicker
    Friend WithEvents cmdConductoresAdicionales As System.Windows.Forms.Button
    Friend WithEvents lblAnosDiasMeses As System.Windows.Forms.Label
    Friend WithEvents lblRiesgo As System.Windows.Forms.Label
    Friend WithEvents cmdTarificar As ControlesPBase.BotonP
    Friend WithEvents cmdNavegarRiesgo As System.Windows.Forms.Button

End Class
