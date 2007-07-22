<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlImagen
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ctrlImagen))
        Me.GroupBox7 = New System.Windows.Forms.GroupBox
        Me.cmdImagen = New System.Windows.Forms.Button
        Me.PictureBox1 = New System.Windows.Forms.PictureBox
        Me.txtImagenY = New ControlesPBase.txtValidable
        Me.Label8 = New System.Windows.Forms.Label
        Me.Label4 = New System.Windows.Forms.Label
        Me.Label9 = New System.Windows.Forms.Label
        Me.Label7 = New System.Windows.Forms.Label
        Me.txtImagenX = New ControlesPBase.txtValidable
        Me.GroupBox1 = New System.Windows.Forms.GroupBox
        Me.GroupBox2 = New System.Windows.Forms.GroupBox
        Me.txtNombre = New ControlesPBase.txtValidable
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog
        Me.chkDesviacion = New System.Windows.Forms.CheckBox
        Me.GroupBox7.SuspendLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox7
        '
        Me.GroupBox7.Controls.Add(Me.cmdImagen)
        Me.GroupBox7.Controls.Add(Me.PictureBox1)
        Me.GroupBox7.Location = New System.Drawing.Point(3, 61)
        Me.GroupBox7.Name = "GroupBox7"
        Me.GroupBox7.Size = New System.Drawing.Size(332, 192)
        Me.GroupBox7.TabIndex = 53
        Me.GroupBox7.TabStop = False
        Me.GroupBox7.Text = "Imagen"
        '
        'cmdImagen
        '
        Me.cmdImagen.Image = CType(resources.GetObject("cmdImagen.Image"), System.Drawing.Image)
        Me.cmdImagen.Location = New System.Drawing.Point(288, 13)
        Me.cmdImagen.Name = "cmdImagen"
        Me.cmdImagen.Size = New System.Drawing.Size(38, 24)
        Me.cmdImagen.TabIndex = 52
        Me.cmdImagen.UseVisualStyleBackColor = True
        '
        'PictureBox1
        '
        Me.PictureBox1.BackColor = System.Drawing.Color.FromArgb(CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer))
        Me.PictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PictureBox1.Location = New System.Drawing.Point(6, 43)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(320, 144)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.PictureBox1.TabIndex = 44
        Me.PictureBox1.TabStop = False
        '
        'txtImagenY
        '
        Me.txtImagenY.Location = New System.Drawing.Point(126, 18)
        Me.txtImagenY.MensajeErrorValidacion = Nothing
        Me.txtImagenY.Name = "txtImagenY"
        Me.txtImagenY.Size = New System.Drawing.Size(48, 20)
        Me.txtImagenY.SoloDouble = True
        Me.txtImagenY.TabIndex = 50
        Me.txtImagenY.Text = "0"
        Me.txtImagenY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtImagenY.ToolTipText = Nothing
        Me.txtImagenY.TrimText = False
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(3, 20)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(14, 13)
        Me.Label8.TabIndex = 46
        Me.Label8.Text = "X"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(104, 20)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(14, 13)
        Me.Label4.TabIndex = 49
        Me.Label4.Text = "Y"
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(75, 20)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(23, 13)
        Me.Label9.TabIndex = 48
        Me.Label9.Text = "mm"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(176, 20)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(23, 13)
        Me.Label7.TabIndex = 51
        Me.Label7.Text = "mm"
        '
        'txtImagenX
        '
        Me.txtImagenX.Location = New System.Drawing.Point(25, 18)
        Me.txtImagenX.MensajeErrorValidacion = Nothing
        Me.txtImagenX.Name = "txtImagenX"
        Me.txtImagenX.Size = New System.Drawing.Size(47, 20)
        Me.txtImagenX.SoloDouble = True
        Me.txtImagenX.TabIndex = 47
        Me.txtImagenX.Text = "0"
        Me.txtImagenX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtImagenX.ToolTipText = Nothing
        Me.txtImagenX.TrimText = False
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.chkDesviacion)
        Me.GroupBox1.Controls.Add(Me.Label8)
        Me.GroupBox1.Controls.Add(Me.txtImagenX)
        Me.GroupBox1.Controls.Add(Me.Label7)
        Me.GroupBox1.Controls.Add(Me.Label9)
        Me.GroupBox1.Controls.Add(Me.Label4)
        Me.GroupBox1.Controls.Add(Me.txtImagenY)
        Me.GroupBox1.Location = New System.Drawing.Point(3, 259)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(332, 48)
        Me.GroupBox1.TabIndex = 55
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Puntos de Impresión"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.txtNombre)
        Me.GroupBox2.Location = New System.Drawing.Point(3, 3)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(332, 52)
        Me.GroupBox2.TabIndex = 56
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Nombre"
        '
        'txtNombre
        '
        Me.txtNombre.Location = New System.Drawing.Point(57, 19)
        Me.txtNombre.MensajeErrorValidacion = Nothing
        Me.txtNombre.Name = "txtNombre"
        Me.txtNombre.Size = New System.Drawing.Size(268, 20)
        Me.txtNombre.TabIndex = 47
        Me.txtNombre.ToolTipText = Nothing
        Me.txtNombre.TrimText = False
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.Filter = "Imagen GIF|*.gif|Imagen JPG|*.jpg|Imagen JPEG|*.jpeg|Imagenes PNG|*.png|Todos los" & _
            " archivos|*.*"
        Me.OpenFileDialog1.Title = "Seleccionar Imagen embebida"
        '
        'chkDesviacion
        '
        Me.chkDesviacion.AutoSize = True
        Me.chkDesviacion.Location = New System.Drawing.Point(212, 21)
        Me.chkDesviacion.Name = "chkDesviacion"
        Me.chkDesviacion.Size = New System.Drawing.Size(114, 17)
        Me.chkDesviacion.TabIndex = 52
        Me.chkDesviacion.Text = "Aplicar Desviación"
        'Me.ToolTip.SetToolTip(Me.chkDesviacion, "Determina si a los puntos de impresión de estaimagen se le aplicarán o no los aju" & _
        '        "stes de corrección de desviación de la configuración de impresión")
        Me.chkDesviacion.UseVisualStyleBackColor = True
        '
        'ctrlImagen
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.GroupBox7)
        Me.Name = "ctrlImagen"
        Me.Size = New System.Drawing.Size(341, 313)
        Me.GroupBox7.ResumeLayout(False)
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents GroupBox7 As System.Windows.Forms.GroupBox
    Friend WithEvents cmdImagen As System.Windows.Forms.Button
    Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
    Friend WithEvents txtImagenY As ControlesPBase.txtValidable
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents txtImagenX As ControlesPBase.txtValidable
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents txtNombre As ControlesPBase.txtValidable
    Friend WithEvents OpenFileDialog1 As System.Windows.Forms.OpenFileDialog
    Friend WithEvents chkDesviacion As System.Windows.Forms.CheckBox

End Class
