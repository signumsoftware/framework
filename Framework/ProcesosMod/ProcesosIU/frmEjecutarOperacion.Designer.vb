<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmEjecutarOperacion
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmEjecutarOperacion))
        Me.DataGridView1 = New System.Windows.Forms.DataGridView
        Me.ProgressBar1 = New System.Windows.Forms.ProgressBar
        Me.Label1 = New System.Windows.Forms.Label
        Me.lblOperacion = New System.Windows.Forms.Label
        Me.Panel1 = New System.Windows.Forms.Panel
        Me.lblPasoActual = New System.Windows.Forms.Label
        Me.Label3 = New System.Windows.Forms.Label
        Me.cmdCancelar = New System.Windows.Forms.Button
        Me.cmdEjecutar = New System.Windows.Forms.Button
        Me.cmdEliminarElemento = New System.Windows.Forms.Button
        Me.cmdSalir = New System.Windows.Forms.Button
        Me.cmdExcel = New System.Windows.Forms.Button
        Me.pnlBotones = New System.Windows.Forms.Panel
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel1.SuspendLayout()
        Me.pnlBotones.SuspendLayout()
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
        Me.DataGridView1.Location = New System.Drawing.Point(13, 57)
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.ReadOnly = True
        Me.DataGridView1.Size = New System.Drawing.Size(665, 390)
        Me.DataGridView1.TabIndex = 0
        '
        'ProgressBar1
        '
        Me.ProgressBar1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ProgressBar1.Location = New System.Drawing.Point(2, 49)
        Me.ProgressBar1.Name = "ProgressBar1"
        Me.ProgressBar1.Size = New System.Drawing.Size(663, 23)
        Me.ProgressBar1.TabIndex = 1
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(12, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(59, 13)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "Operación:"
        '
        'lblOperacion
        '
        Me.lblOperacion.AutoSize = True
        Me.lblOperacion.Location = New System.Drawing.Point(77, 9)
        Me.lblOperacion.Name = "lblOperacion"
        Me.lblOperacion.Size = New System.Drawing.Size(14, 13)
        Me.lblOperacion.TabIndex = 3
        Me.lblOperacion.Text = "#"
        '
        'Panel1
        '
        Me.Panel1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Panel1.Controls.Add(Me.lblPasoActual)
        Me.Panel1.Controls.Add(Me.Label3)
        Me.Panel1.Controls.Add(Me.ProgressBar1)
        Me.Panel1.Location = New System.Drawing.Point(13, 370)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(665, 77)
        Me.Panel1.TabIndex = 4
        Me.Panel1.Visible = False
        '
        'lblPasoActual
        '
        Me.lblPasoActual.AutoSize = True
        Me.lblPasoActual.Location = New System.Drawing.Point(7, 26)
        Me.lblPasoActual.Name = "lblPasoActual"
        Me.lblPasoActual.Size = New System.Drawing.Size(14, 13)
        Me.lblPasoActual.TabIndex = 2
        Me.lblPasoActual.Text = "#"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(4, 4)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(127, 13)
        Me.Label3.TabIndex = 0
        Me.Label3.Text = "Progreso de la ejecución:"
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCancelar.Image = CType(resources.GetObject("cmdCancelar.Image"), System.Drawing.Image)
        Me.cmdCancelar.Location = New System.Drawing.Point(603, 459)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.Size = New System.Drawing.Size(75, 23)
        Me.cmdCancelar.TabIndex = 5
        Me.cmdCancelar.Text = "Cancelar"
        Me.cmdCancelar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'cmdEjecutar
        '
        Me.cmdEjecutar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdEjecutar.Image = CType(resources.GetObject("cmdEjecutar.Image"), System.Drawing.Image)
        Me.cmdEjecutar.Location = New System.Drawing.Point(443, 6)
        Me.cmdEjecutar.Name = "cmdEjecutar"
        Me.cmdEjecutar.Size = New System.Drawing.Size(136, 23)
        Me.cmdEjecutar.TabIndex = 6
        Me.cmdEjecutar.Text = "Ejecutar Operación"
        Me.cmdEjecutar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdEjecutar.UseVisualStyleBackColor = True
        '
        'cmdEliminarElemento
        '
        Me.cmdEliminarElemento.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdEliminarElemento.Image = CType(resources.GetObject("cmdEliminarElemento.Image"), System.Drawing.Image)
        Me.cmdEliminarElemento.Location = New System.Drawing.Point(642, 28)
        Me.cmdEliminarElemento.Name = "cmdEliminarElemento"
        Me.cmdEliminarElemento.Size = New System.Drawing.Size(31, 23)
        Me.cmdEliminarElemento.TabIndex = 7
        Me.cmdEliminarElemento.UseVisualStyleBackColor = True
        '
        'cmdSalir
        '
        Me.cmdSalir.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdSalir.Image = CType(resources.GetObject("cmdSalir.Image"), System.Drawing.Image)
        Me.cmdSalir.Location = New System.Drawing.Point(603, 459)
        Me.cmdSalir.Name = "cmdSalir"
        Me.cmdSalir.Size = New System.Drawing.Size(75, 23)
        Me.cmdSalir.TabIndex = 8
        Me.cmdSalir.Text = "Salir"
        Me.cmdSalir.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdSalir.UseVisualStyleBackColor = True
        Me.cmdSalir.Visible = False
        '
        'cmdExcel
        '
        Me.cmdExcel.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdExcel.Image = CType(resources.GetObject("cmdExcel.Image"), System.Drawing.Image)
        Me.cmdExcel.Location = New System.Drawing.Point(314, 6)
        Me.cmdExcel.Name = "cmdExcel"
        Me.cmdExcel.Size = New System.Drawing.Size(123, 23)
        Me.cmdExcel.TabIndex = 9
        Me.cmdExcel.Text = "Exportar Informe"
        Me.cmdExcel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdExcel.UseVisualStyleBackColor = True
        Me.cmdExcel.Visible = False
        '
        'pnlBotones
        '
        Me.pnlBotones.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pnlBotones.Controls.Add(Me.cmdExcel)
        Me.pnlBotones.Controls.Add(Me.cmdEjecutar)
        Me.pnlBotones.Location = New System.Drawing.Point(13, 453)
        Me.pnlBotones.Name = "pnlBotones"
        Me.pnlBotones.Size = New System.Drawing.Size(584, 33)
        Me.pnlBotones.TabIndex = 10
        '
        'frmEjecutarOperacion
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(690, 498)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.pnlBotones)
        Me.Controls.Add(Me.cmdSalir)
        Me.Controls.Add(Me.cmdEliminarElemento)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.lblOperacion)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.DataGridView1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmEjecutarOperacion"
        Me.Text = "Ejecutar Operación"
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        Me.pnlBotones.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents DataGridView1 As System.Windows.Forms.DataGridView
    Friend WithEvents ProgressBar1 As System.Windows.Forms.ProgressBar
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents lblOperacion As System.Windows.Forms.Label
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents lblPasoActual As System.Windows.Forms.Label
    Friend WithEvents cmdCancelar As System.Windows.Forms.Button
    Friend WithEvents cmdEjecutar As System.Windows.Forms.Button
    Friend WithEvents cmdEliminarElemento As System.Windows.Forms.Button
    Friend WithEvents cmdSalir As System.Windows.Forms.Button
    Friend WithEvents cmdExcel As System.Windows.Forms.Button
    Friend WithEvents pnlBotones As System.Windows.Forms.Panel

End Class
