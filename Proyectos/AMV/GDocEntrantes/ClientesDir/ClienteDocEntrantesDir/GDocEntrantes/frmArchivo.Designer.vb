<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmArchivo
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmArchivo))
        Me.Label1 = New System.Windows.Forms.Label
        Me.Label2 = New System.Windows.Forms.Label
        Me.Label3 = New System.Windows.Forms.Label
        Me.Label4 = New System.Windows.Forms.Label
        Me.Label5 = New System.Windows.Forms.Label
        Me.Label6 = New System.Windows.Forms.Label
        Me.Button1 = New System.Windows.Forms.Button
        Me.cmdAbrirArchivo = New System.Windows.Forms.Button
        Me.txtNombreArchivo = New System.Windows.Forms.TextBox
        Me.txtRuta = New System.Windows.Forms.TextBox
        Me.txtNombreOriginal = New System.Windows.Forms.TextBox
        Me.txtExtension = New System.Windows.Forms.TextBox
        Me.txtIDEntidadNegocio = New System.Windows.Forms.TextBox
        Me.txtTipoEntidadNegocio = New System.Windows.Forms.TextBox
        Me.cmdRecuperarOperacion = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(12, 15)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(86, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Nombre Archivo:"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(65, 52)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(33, 13)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Ruta:"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(13, 86)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(85, 13)
        Me.Label3.TabIndex = 2
        Me.Label3.Text = "Nombre Original:"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(42, 117)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(56, 13)
        Me.Label4.TabIndex = 3
        Me.Label4.Text = "Extensión:"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(23, 151)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(103, 13)
        Me.Label5.TabIndex = 4
        Me.Label5.Text = "ID Entidad Negocio:"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(13, 189)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(113, 13)
        Me.Label6.TabIndex = 5
        Me.Label6.Text = "Tipo Entidad Negocio:"
        '
        'Button1
        '
        Me.Button1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button1.Image = CType(resources.GetObject("Button1.Image"), System.Drawing.Image)
        Me.Button1.Location = New System.Drawing.Point(301, 264)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(76, 34)
        Me.Button1.TabIndex = 12
        Me.Button1.Text = "Aceptar"
        Me.Button1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.Button1.UseVisualStyleBackColor = True
        '
        'cmdAbrirArchivo
        '
        Me.cmdAbrirArchivo.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.cmdAbrirArchivo.Image = Global.GDocEntrantes.My.Resources.Resources.documento_ver_32
        Me.cmdAbrirArchivo.Location = New System.Drawing.Point(16, 230)
        Me.cmdAbrirArchivo.Name = "cmdAbrirArchivo"
        Me.cmdAbrirArchivo.Size = New System.Drawing.Size(81, 68)
        Me.cmdAbrirArchivo.TabIndex = 13
        Me.cmdAbrirArchivo.Text = "Abrir Archivo"
        Me.cmdAbrirArchivo.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        ' Me.ToolTip.SetToolTip(Me.cmdAbrirArchivo, "Abre el archivo de este documento")
        Me.cmdAbrirArchivo.UseVisualStyleBackColor = True
        '
        'txtNombreArchivo
        '
        Me.txtNombreArchivo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtNombreArchivo.Location = New System.Drawing.Point(104, 12)
        Me.txtNombreArchivo.Name = "txtNombreArchivo"
        Me.txtNombreArchivo.ReadOnly = True
        Me.txtNombreArchivo.Size = New System.Drawing.Size(273, 20)
        Me.txtNombreArchivo.TabIndex = 14
        '
        'txtRuta
        '
        Me.txtRuta.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtRuta.Location = New System.Drawing.Point(104, 49)
        Me.txtRuta.Name = "txtRuta"
        Me.txtRuta.ReadOnly = True
        Me.txtRuta.Size = New System.Drawing.Size(273, 20)
        Me.txtRuta.TabIndex = 15
        '
        'txtNombreOriginal
        '
        Me.txtNombreOriginal.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtNombreOriginal.Location = New System.Drawing.Point(104, 83)
        Me.txtNombreOriginal.Name = "txtNombreOriginal"
        Me.txtNombreOriginal.ReadOnly = True
        Me.txtNombreOriginal.Size = New System.Drawing.Size(273, 20)
        Me.txtNombreOriginal.TabIndex = 16
        '
        'txtExtension
        '
        Me.txtExtension.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtExtension.Location = New System.Drawing.Point(104, 114)
        Me.txtExtension.Name = "txtExtension"
        Me.txtExtension.ReadOnly = True
        Me.txtExtension.Size = New System.Drawing.Size(273, 20)
        Me.txtExtension.TabIndex = 17
        '
        'txtIDEntidadNegocio
        '
        Me.txtIDEntidadNegocio.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtIDEntidadNegocio.Location = New System.Drawing.Point(132, 148)
        Me.txtIDEntidadNegocio.Name = "txtIDEntidadNegocio"
        Me.txtIDEntidadNegocio.ReadOnly = True
        Me.txtIDEntidadNegocio.Size = New System.Drawing.Size(245, 20)
        Me.txtIDEntidadNegocio.TabIndex = 18
        '
        'txtTipoEntidadNegocio
        '
        Me.txtTipoEntidadNegocio.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtTipoEntidadNegocio.Location = New System.Drawing.Point(132, 186)
        Me.txtTipoEntidadNegocio.Name = "txtTipoEntidadNegocio"
        Me.txtTipoEntidadNegocio.ReadOnly = True
        Me.txtTipoEntidadNegocio.Size = New System.Drawing.Size(245, 20)
        Me.txtTipoEntidadNegocio.TabIndex = 19
        '
        'cmdRecuperarOperacion
        '
        Me.cmdRecuperarOperacion.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.cmdRecuperarOperacion.Image = CType(resources.GetObject("cmdRecuperarOperacion.Image"), System.Drawing.Image)
        Me.cmdRecuperarOperacion.Location = New System.Drawing.Point(103, 230)
        Me.cmdRecuperarOperacion.Name = "cmdRecuperarOperacion"
        Me.cmdRecuperarOperacion.Size = New System.Drawing.Size(82, 68)
        Me.cmdRecuperarOperacion.TabIndex = 20
        Me.cmdRecuperarOperacion.Text = "Recuperar Operación"
        Me.cmdRecuperarOperacion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        '    Me.ToolTip.SetToolTip(Me.cmdRecuperarOperacion, "Recupera la Operación Activa que tenga este Documento para postclasificarla")
        Me.cmdRecuperarOperacion.UseVisualStyleBackColor = True
        '
        'frmArchivo
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(405, 306)
        Me.Controls.Add(Me.cmdRecuperarOperacion)
        Me.Controls.Add(Me.txtTipoEntidadNegocio)
        Me.Controls.Add(Me.txtIDEntidadNegocio)
        Me.Controls.Add(Me.txtExtension)
        Me.Controls.Add(Me.txtNombreOriginal)
        Me.Controls.Add(Me.txtRuta)
        Me.Controls.Add(Me.txtNombreArchivo)
        Me.Controls.Add(Me.cmdAbrirArchivo)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MinimumSize = New System.Drawing.Size(413, 340)
        Me.Name = "frmArchivo"
        Me.Text = "Ver Documento"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents cmdAbrirArchivo As System.Windows.Forms.Button
    Friend WithEvents txtNombreArchivo As System.Windows.Forms.TextBox
    Friend WithEvents txtRuta As System.Windows.Forms.TextBox
    Friend WithEvents txtNombreOriginal As System.Windows.Forms.TextBox
    Friend WithEvents txtExtension As System.Windows.Forms.TextBox
    Friend WithEvents txtIDEntidadNegocio As System.Windows.Forms.TextBox
    Friend WithEvents txtTipoEntidadNegocio As System.Windows.Forms.TextBox
    Friend WithEvents cmdRecuperarOperacion As System.Windows.Forms.Button
End Class
