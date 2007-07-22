<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ArbolTConBusqueda
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
        Me.components = New System.ComponentModel.Container
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ArbolTConBusqueda))
        Me.Label1 = New System.Windows.Forms.Label
        Me.pnlOpciones = New System.Windows.Forms.Panel
        Me.optContengan = New System.Windows.Forms.RadioButton
        Me.optComiencenPor = New System.Windows.Forms.RadioButton
        Me.lbltitulo = New System.Windows.Forms.Label
        Me.txtvBusqueda = New ControlesPBase.txtValidable
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.PictureBox1 = New System.Windows.Forms.PictureBox
        Me.ArbolNododeT = New ControlesPGenericos.ArbolNododeT
        Me.pnlOpciones.SuspendLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(83, 7)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(55, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Búsqueda"
        '
        'pnlOpciones
        '
        Me.pnlOpciones.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pnlOpciones.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pnlOpciones.Controls.Add(Me.optContengan)
        Me.pnlOpciones.Controls.Add(Me.optComiencenPor)
        Me.pnlOpciones.Controls.Add(Me.lbltitulo)
        Me.pnlOpciones.ForeColor = System.Drawing.SystemColors.ControlText
        Me.pnlOpciones.Location = New System.Drawing.Point(80, 3)
        Me.pnlOpciones.Name = "pnlOpciones"
        Me.pnlOpciones.Size = New System.Drawing.Size(218, 83)
        Me.pnlOpciones.TabIndex = 3
        Me.pnlOpciones.Visible = False
        '
        'optContengan
        '
        Me.optContengan.AutoSize = True
        Me.optContengan.Location = New System.Drawing.Point(39, 50)
        Me.optContengan.Name = "optContengan"
        Me.optContengan.Size = New System.Drawing.Size(108, 17)
        Me.optContengan.TabIndex = 2
        Me.optContengan.Text = "Que contengan..."
        Me.optContengan.UseVisualStyleBackColor = True
        '
        'optComiencenPor
        '
        Me.optComiencenPor.AutoSize = True
        Me.optComiencenPor.Location = New System.Drawing.Point(39, 26)
        Me.optComiencenPor.Name = "optComiencenPor"
        Me.optComiencenPor.Size = New System.Drawing.Size(127, 17)
        Me.optComiencenPor.TabIndex = 1
        Me.optComiencenPor.Text = "Que comiencen por..."
        Me.optComiencenPor.UseVisualStyleBackColor = True
        '
        'lbltitulo
        '
        Me.lbltitulo.AutoSize = True
        Me.lbltitulo.Location = New System.Drawing.Point(3, 0)
        Me.lbltitulo.Name = "lbltitulo"
        Me.lbltitulo.Size = New System.Drawing.Size(117, 13)
        Me.lbltitulo.TabIndex = 0
        Me.lbltitulo.Tag = "lbltitulo"
        Me.lbltitulo.Text = "Opciones de búsqueda"
        '
        'txtvBusqueda
        '
        Me.txtvBusqueda.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtvBusqueda.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtvBusqueda.Location = New System.Drawing.Point(143, 5)
        Me.txtvBusqueda.MensajeError = Nothing
        Me.txtvBusqueda.MensajeErrorValidacion = Nothing
        Me.txtvBusqueda.Name = "txtvBusqueda"
        Me.txtvBusqueda.Size = New System.Drawing.Size(128, 20)
        Me.txtvBusqueda.SoloDouble = False
        Me.txtvBusqueda.SoloInteger = False
        Me.txtvBusqueda.TabIndex = 4
        Me.txtvBusqueda.ToolTipText = Nothing
        '
        'Timer1
        '
        Me.Timer1.Interval = 2000
        '
        'PictureBox1
        '
        Me.PictureBox1.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.PictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PictureBox1.Image = CType(resources.GetObject("PictureBox1.Image"), System.Drawing.Image)
        Me.PictureBox1.Location = New System.Drawing.Point(275, 6)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(18, 18)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize
        Me.PictureBox1.TabIndex = 5
        Me.PictureBox1.TabStop = False
        Me.ToolTip.SetToolTip(Me.PictureBox1, "Opciones de búsqueda")
        '
        'ArbolNododeT
        '
        Me.ArbolNododeT.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ArbolNododeT.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.ArbolNododeT.Location = New System.Drawing.Point(3, 29)
        Me.ArbolNododeT.MensajeError = ""
        Me.ArbolNododeT.Name = "ArbolNododeT"
        Me.ArbolNododeT.Size = New System.Drawing.Size(295, 274)
        Me.ArbolNododeT.TabIndex = 0
        Me.ArbolNododeT.ToolTipText = Nothing
        '
        'ArbolTConBusqueda
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Me.txtvBusqueda)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.ArbolNododeT)
        Me.Controls.Add(Me.pnlOpciones)
        Me.Name = "ArbolTConBusqueda"
        Me.Size = New System.Drawing.Size(301, 306)
        Me.pnlOpciones.ResumeLayout(False)
        Me.pnlOpciones.PerformLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ArbolNododeT As ArbolNododeT
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents pnlOpciones As System.Windows.Forms.Panel
    Friend WithEvents lbltitulo As System.Windows.Forms.Label
    Friend WithEvents optContengan As System.Windows.Forms.RadioButton
    Friend WithEvents optComiencenPor As System.Windows.Forms.RadioButton
    Friend WithEvents txtvBusqueda As ControlesPBase.txtValidable
    Friend WithEvents Timer1 As System.Windows.Forms.Timer
    Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox

End Class
