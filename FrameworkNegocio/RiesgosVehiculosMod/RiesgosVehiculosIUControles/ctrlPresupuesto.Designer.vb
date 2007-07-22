<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlPresupuesto
    Inherits MotorIU.ControlesP.BaseControlP

    'UserControl overrides dispose to clean up the component list.
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
        Me.Label1 = New System.Windows.Forms.Label
        Me.Label2 = New System.Windows.Forms.Label
        Me.Label3 = New System.Windows.Forms.Label
        Me.Label4 = New System.Windows.Forms.Label
        Me.dtpValidezDesde = New System.Windows.Forms.DateTimePicker
        Me.lblFuturoTomador = New System.Windows.Forms.Label
        Me.lblEstado = New System.Windows.Forms.Label
        Me.lblEntidadEmisora = New System.Windows.Forms.Label
        Me.Label8 = New System.Windows.Forms.Label
        Me.dtpValidezHasta = New System.Windows.Forms.DateTimePicker
        Me.lblfechaAnulacionTitulo = New System.Windows.Forms.Label
        Me.lblFechaAnulacion = New System.Windows.Forms.Label
        Me.Label5 = New System.Windows.Forms.Label
        Me.lstDocumentos = New System.Windows.Forms.ListBox
        Me.ctrlTarifa1 = New FN.RiesgosVehiculos.IU.Controles.ctrlTarifa
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(72, 17)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(40, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Estado"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(18, 48)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(94, 13)
        Me.Label2.TabIndex = 2
        Me.Label2.Text = "Periodo de validez"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(30, 87)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(82, 13)
        Me.Label3.TabIndex = 3
        Me.Label3.Text = "Futuro Tomador"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(338, 48)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(82, 13)
        Me.Label4.TabIndex = 4
        Me.Label4.Text = "Entidad emisora"
        '
        'dtpValidezDesde
        '
        Me.dtpValidezDesde.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpValidezDesde.Location = New System.Drawing.Point(131, 44)
        Me.dtpValidezDesde.Name = "dtpValidezDesde"
        Me.dtpValidezDesde.Size = New System.Drawing.Size(85, 20)
        Me.dtpValidezDesde.TabIndex = 5
        '
        'lblFuturoTomador
        '
        Me.lblFuturoTomador.AutoSize = True
        Me.lblFuturoTomador.BackColor = System.Drawing.Color.White
        Me.lblFuturoTomador.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblFuturoTomador.Location = New System.Drawing.Point(128, 87)
        Me.lblFuturoTomador.Name = "lblFuturoTomador"
        Me.lblFuturoTomador.Size = New System.Drawing.Size(12, 15)
        Me.lblFuturoTomador.TabIndex = 6
        Me.lblFuturoTomador.Text = "-"
        '
        'lblEstado
        '
        Me.lblEstado.AutoSize = True
        Me.lblEstado.BackColor = System.Drawing.Color.White
        Me.lblEstado.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblEstado.Location = New System.Drawing.Point(128, 17)
        Me.lblEstado.Name = "lblEstado"
        Me.lblEstado.Size = New System.Drawing.Size(12, 15)
        Me.lblEstado.TabIndex = 7
        Me.lblEstado.Text = "-"
        '
        'lblEntidadEmisora
        '
        Me.lblEntidadEmisora.AutoSize = True
        Me.lblEntidadEmisora.BackColor = System.Drawing.Color.White
        Me.lblEntidadEmisora.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblEntidadEmisora.Location = New System.Drawing.Point(426, 48)
        Me.lblEntidadEmisora.Name = "lblEntidadEmisora"
        Me.lblEntidadEmisora.Size = New System.Drawing.Size(12, 15)
        Me.lblEntidadEmisora.TabIndex = 8
        Me.lblEntidadEmisora.Text = "-"
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(222, 48)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(15, 13)
        Me.Label8.TabIndex = 9
        Me.Label8.Text = "al"
        '
        'dtpValidezHasta
        '
        Me.dtpValidezHasta.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpValidezHasta.Location = New System.Drawing.Point(243, 44)
        Me.dtpValidezHasta.Name = "dtpValidezHasta"
        Me.dtpValidezHasta.Size = New System.Drawing.Size(85, 20)
        Me.dtpValidezHasta.TabIndex = 10
        '
        'lblfechaAnulacionTitulo
        '
        Me.lblfechaAnulacionTitulo.AutoSize = True
        Me.lblfechaAnulacionTitulo.Location = New System.Drawing.Point(319, 17)
        Me.lblfechaAnulacionTitulo.Name = "lblfechaAnulacionTitulo"
        Me.lblfechaAnulacionTitulo.Size = New System.Drawing.Size(101, 13)
        Me.lblfechaAnulacionTitulo.TabIndex = 11
        Me.lblfechaAnulacionTitulo.Text = "Fecha de anulación"
        '
        'lblFechaAnulacion
        '
        Me.lblFechaAnulacion.AutoSize = True
        Me.lblFechaAnulacion.BackColor = System.Drawing.Color.White
        Me.lblFechaAnulacion.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblFechaAnulacion.Location = New System.Drawing.Point(426, 17)
        Me.lblFechaAnulacion.Name = "lblFechaAnulacion"
        Me.lblFechaAnulacion.Size = New System.Drawing.Size(12, 15)
        Me.lblFechaAnulacion.TabIndex = 12
        Me.lblFechaAnulacion.Text = "-"
        '
        'Label5
        '
        Me.Label5.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(30, 585)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(124, 13)
        Me.Label5.TabIndex = 13
        Me.Label5.Text = "Documentos Requeridos"
        '
        'lstDocumentos
        '
        Me.lstDocumentos.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstDocumentos.FormattingEnabled = True
        Me.lstDocumentos.Location = New System.Drawing.Point(21, 601)
        Me.lstDocumentos.Name = "lstDocumentos"
        Me.lstDocumentos.Size = New System.Drawing.Size(570, 56)
        Me.lstDocumentos.TabIndex = 14
        '
        'ctrlTarifa1
        '
        Me.ctrlTarifa1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ctrlTarifa1.Location = New System.Drawing.Point(12, 103)
        Me.ctrlTarifa1.MensajeError = ""
        Me.ctrlTarifa1.Name = "ctrlTarifa1"
        Me.ctrlTarifa1.Size = New System.Drawing.Size(601, 473)
        Me.ctrlTarifa1.TabIndex = 0
        Me.ctrlTarifa1.ToolTipText = Nothing
        '
        'ctrlPresupuesto
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.lstDocumentos)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.lblFechaAnulacion)
        Me.Controls.Add(Me.lblfechaAnulacionTitulo)
        Me.Controls.Add(Me.dtpValidezHasta)
        Me.Controls.Add(Me.Label8)
        Me.Controls.Add(Me.lblEntidadEmisora)
        Me.Controls.Add(Me.lblEstado)
        Me.Controls.Add(Me.lblFuturoTomador)
        Me.Controls.Add(Me.dtpValidezDesde)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.ctrlTarifa1)
        Me.Name = "ctrlPresupuesto"
        Me.Size = New System.Drawing.Size(623, 669)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ctrlTarifa1 As ctrlTarifa
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents dtpValidezDesde As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblFuturoTomador As System.Windows.Forms.Label
    Friend WithEvents lblEstado As System.Windows.Forms.Label
    Friend WithEvents lblEntidadEmisora As System.Windows.Forms.Label
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents dtpValidezHasta As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblfechaAnulacionTitulo As System.Windows.Forms.Label
    Friend WithEvents lblFechaAnulacion As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents lstDocumentos As System.Windows.Forms.ListBox

End Class
