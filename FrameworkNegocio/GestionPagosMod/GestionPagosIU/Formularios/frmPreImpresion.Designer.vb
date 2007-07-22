<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmPreImpresion
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmPreImpresion))
        Me.DataGridView1 = New System.Windows.Forms.DataGridView
        Me.Label1 = New System.Windows.Forms.Label
        Me.optAutomatica = New System.Windows.Forms.RadioButton
        Me.optManual = New System.Windows.Forms.RadioButton
        Me.grpConfiguracion = New System.Windows.Forms.GroupBox
        Me.cboConfiguracionImpresion = New System.Windows.Forms.ComboBox
        Me.Label3 = New System.Windows.Forms.Label
        Me.Label2 = New System.Windows.Forms.Label
        Me.cmdNumerosSerie = New System.Windows.Forms.Button
        Me.cmd_Aceptar = New System.Windows.Forms.Button
        Me.cmdCancelar = New System.Windows.Forms.Button
        Me.grpProgreso = New System.Windows.Forms.GroupBox
        Me.lblOperacionEnCurso = New System.Windows.Forms.Label
        Me.ProgressBar1 = New System.Windows.Forms.ProgressBar
        Me.BackgroundWorker1 = New System.ComponentModel.BackgroundWorker
        Me.PrintDialog1 = New System.Windows.Forms.PrintDialog
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.grpConfiguracion.SuspendLayout()
        Me.grpProgreso.SuspendLayout()
        Me.SuspendLayout()
        '
        'DataGridView1
        '
        Me.DataGridView1.AllowUserToAddRows = False
        Me.DataGridView1.AllowUserToDeleteRows = False
        Me.DataGridView1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridView1.Location = New System.Drawing.Point(12, 33)
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.DataGridView1.Size = New System.Drawing.Size(674, 312)
        Me.DataGridView1.TabIndex = 0
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(12, 17)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(151, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Talones que se van a  Imprimir"
        '
        'optAutomatica
        '
        Me.optAutomatica.AutoSize = True
        Me.optAutomatica.Checked = True
        Me.optAutomatica.Location = New System.Drawing.Point(296, 44)
        Me.optAutomatica.Name = "optAutomatica"
        Me.optAutomatica.Size = New System.Drawing.Size(78, 17)
        Me.optAutomatica.TabIndex = 3
        Me.optAutomatica.TabStop = True
        Me.optAutomatica.Text = "Automática"
        Me.optAutomatica.UseVisualStyleBackColor = True
        '
        'optManual
        '
        Me.optManual.AutoSize = True
        Me.optManual.Location = New System.Drawing.Point(296, 68)
        Me.optManual.Name = "optManual"
        Me.optManual.Size = New System.Drawing.Size(60, 17)
        Me.optManual.TabIndex = 4
        Me.optManual.Text = "Manual"
        Me.optManual.UseVisualStyleBackColor = True
        '
        'grpConfiguracion
        '
        Me.grpConfiguracion.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.grpConfiguracion.Controls.Add(Me.cboConfiguracionImpresion)
        Me.grpConfiguracion.Controls.Add(Me.Label3)
        Me.grpConfiguracion.Controls.Add(Me.Label2)
        Me.grpConfiguracion.Controls.Add(Me.cmdNumerosSerie)
        Me.grpConfiguracion.Controls.Add(Me.optAutomatica)
        Me.grpConfiguracion.Controls.Add(Me.optManual)
        Me.grpConfiguracion.Location = New System.Drawing.Point(12, 364)
        Me.grpConfiguracion.Name = "grpConfiguracion"
        Me.grpConfiguracion.Size = New System.Drawing.Size(615, 94)
        Me.grpConfiguracion.TabIndex = 6
        Me.grpConfiguracion.TabStop = False
        Me.grpConfiguracion.Text = "Configuración"
        '
        'cboConfiguracionImpresion
        '
        Me.cboConfiguracionImpresion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboConfiguracionImpresion.FormattingEnabled = True
        Me.cboConfiguracionImpresion.Location = New System.Drawing.Point(12, 43)
        Me.cboConfiguracionImpresion.Name = "cboConfiguracionImpresion"
        Me.cboConfiguracionImpresion.Size = New System.Drawing.Size(177, 21)
        Me.cboConfiguracionImpresion.TabIndex = 8
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(9, 25)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(134, 13)
        Me.Label3.TabIndex = 7
        Me.Label3.Text = "Configuración de impresión"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(237, 25)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(94, 13)
        Me.Label2.TabIndex = 6
        Me.Label2.Text = "Tipo de Impresión:"
        '
        'cmdNumerosSerie
        '
        Me.cmdNumerosSerie.Image = CType(resources.GetObject("cmdNumerosSerie.Image"), System.Drawing.Image)
        Me.cmdNumerosSerie.Location = New System.Drawing.Point(476, 31)
        Me.cmdNumerosSerie.Name = "cmdNumerosSerie"
        Me.cmdNumerosSerie.Size = New System.Drawing.Size(130, 41)
        Me.cmdNumerosSerie.TabIndex = 5
        Me.cmdNumerosSerie.Text = "Números de Serie Automáticos"
        Me.cmdNumerosSerie.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdNumerosSerie.UseVisualStyleBackColor = True
        '
        'cmd_Aceptar
        '
        Me.cmd_Aceptar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmd_Aceptar.Image = Global.FN.GestionPagos.IU.My.Resources.Resources.check_16
        Me.cmd_Aceptar.Location = New System.Drawing.Point(527, 481)
        Me.cmd_Aceptar.Name = "cmd_Aceptar"
        Me.cmd_Aceptar.Size = New System.Drawing.Size(75, 23)
        Me.cmd_Aceptar.TabIndex = 9
        Me.cmd_Aceptar.Text = "Aceptar"
        Me.cmd_Aceptar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmd_Aceptar.UseVisualStyleBackColor = True
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCancelar.Image = Global.FN.GestionPagos.IU.My.Resources.Resources.delete_16
        Me.cmdCancelar.Location = New System.Drawing.Point(611, 481)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.Size = New System.Drawing.Size(75, 23)
        Me.cmdCancelar.TabIndex = 8
        Me.cmdCancelar.Text = "Cancelar"
        Me.cmdCancelar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'grpProgreso
        '
        Me.grpProgreso.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.grpProgreso.Controls.Add(Me.lblOperacionEnCurso)
        Me.grpProgreso.Controls.Add(Me.ProgressBar1)
        Me.grpProgreso.Location = New System.Drawing.Point(11, 364)
        Me.grpProgreso.Name = "grpProgreso"
        Me.grpProgreso.Size = New System.Drawing.Size(615, 94)
        Me.grpProgreso.TabIndex = 6
        Me.grpProgreso.TabStop = False
        Me.grpProgreso.Text = "Imprimiendo..."
        Me.grpProgreso.Visible = False
        '
        'lblOperacionEnCurso
        '
        Me.lblOperacionEnCurso.AutoSize = True
        Me.lblOperacionEnCurso.Location = New System.Drawing.Point(7, 30)
        Me.lblOperacionEnCurso.Name = "lblOperacionEnCurso"
        Me.lblOperacionEnCurso.Size = New System.Drawing.Size(16, 13)
        Me.lblOperacionEnCurso.TabIndex = 1
        Me.lblOperacionEnCurso.Text = "---"
        '
        'ProgressBar1
        '
        Me.ProgressBar1.Location = New System.Drawing.Point(7, 53)
        Me.ProgressBar1.Name = "ProgressBar1"
        Me.ProgressBar1.Size = New System.Drawing.Size(599, 31)
        Me.ProgressBar1.TabIndex = 0
        '
        'BackgroundWorker1
        '
        Me.BackgroundWorker1.WorkerReportsProgress = True
        '
        'PrintDialog1
        '
        Me.PrintDialog1.AllowPrintToFile = False
        Me.PrintDialog1.UseEXDialog = True
        '
        'frmPreImpresion
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(694, 510)
        Me.Controls.Add(Me.cmd_Aceptar)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.DataGridView1)
        Me.Controls.Add(Me.grpConfiguracion)
        Me.Controls.Add(Me.grpProgreso)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MinimumSize = New System.Drawing.Size(644, 514)
        Me.Name = "frmPreImpresion"
        Me.Text = "Impresión de Talones"
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.grpConfiguracion.ResumeLayout(False)
        Me.grpConfiguracion.PerformLayout()
        Me.grpProgreso.ResumeLayout(False)
        Me.grpProgreso.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents DataGridView1 As System.Windows.Forms.DataGridView
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents optAutomatica As System.Windows.Forms.RadioButton
    Friend WithEvents optManual As System.Windows.Forms.RadioButton
    Friend WithEvents cmdNumerosSerie As System.Windows.Forms.Button
    Friend WithEvents grpConfiguracion As System.Windows.Forms.GroupBox
    Friend WithEvents cmdCancelar As System.Windows.Forms.Button
    Friend WithEvents cmd_Aceptar As System.Windows.Forms.Button
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents grpProgreso As System.Windows.Forms.GroupBox
    Friend WithEvents lblOperacionEnCurso As System.Windows.Forms.Label
    Friend WithEvents ProgressBar1 As System.Windows.Forms.ProgressBar
    Friend WithEvents cboConfiguracionImpresion As System.Windows.Forms.ComboBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents BackgroundWorker1 As System.ComponentModel.BackgroundWorker
    Friend WithEvents PrintDialog1 As System.Windows.Forms.PrintDialog
End Class
