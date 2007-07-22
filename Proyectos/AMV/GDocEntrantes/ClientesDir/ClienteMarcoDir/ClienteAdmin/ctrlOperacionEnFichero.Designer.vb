<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlOperacionEnFichero
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ctrlOperacionEnFichero))
        Me.Label1 = New System.Windows.Forms.Label
        Me.txtFI = New System.Windows.Forms.TextBox
        Me.txtFF = New System.Windows.Forms.TextBox
        Me.Label2 = New System.Windows.Forms.Label
        Me.txtComentarioOperacion = New System.Windows.Forms.TextBox
        Me.Label4 = New System.Windows.Forms.Label
        Me.txtNombreOperador = New System.Windows.Forms.TextBox
        Me.Label5 = New System.Windows.Forms.Label
        Me.txtTipoOperacion = New System.Windows.Forms.TextBox
        Me.Label6 = New System.Windows.Forms.Label
        Me.txtNombreFicheroOriginal = New System.Windows.Forms.TextBox
        Me.Label7 = New System.Windows.Forms.Label
        Me.txtEstado = New System.Windows.Forms.TextBox
        Me.Label8 = New System.Windows.Forms.Label
        Me.txtEntidadNegocio = New System.Windows.Forms.TextBox
        Me.Label9 = New System.Windows.Forms.Label
        Me.txtComentarioEntidad = New System.Windows.Forms.TextBox
        Me.Label3 = New System.Windows.Forms.Label
        Me.cmdAbrir = New System.Windows.Forms.Button
        Me.cmdAbrirFichero = New System.Windows.Forms.Button
        Me.cmdCopiarRuta = New System.Windows.Forms.Button
        Me.cmdCopiarID = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(60, 95)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(67, 13)
        Me.Label1.TabIndex = 23
        Me.Label1.Text = "Fecha Inicial"
        '
        'txtFI
        '
        Me.txtFI.Location = New System.Drawing.Point(133, 92)
        Me.txtFI.Name = "txtFI"
        Me.txtFI.ReadOnly = True
        Me.txtFI.Size = New System.Drawing.Size(174, 20)
        Me.txtFI.TabIndex = 4
        '
        'txtFF
        '
        Me.txtFF.Location = New System.Drawing.Point(385, 92)
        Me.txtFF.Name = "txtFF"
        Me.txtFF.ReadOnly = True
        Me.txtFF.Size = New System.Drawing.Size(174, 20)
        Me.txtFF.TabIndex = 5
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(317, 95)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(62, 13)
        Me.Label2.TabIndex = 24
        Me.Label2.Text = "Fecha Final"
        '
        'txtComentarioOperacion
        '
        Me.txtComentarioOperacion.Location = New System.Drawing.Point(133, 118)
        Me.txtComentarioOperacion.Multiline = True
        Me.txtComentarioOperacion.Name = "txtComentarioOperacion"
        Me.txtComentarioOperacion.ReadOnly = True
        Me.txtComentarioOperacion.Size = New System.Drawing.Size(426, 86)
        Me.txtComentarioOperacion.TabIndex = 6
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(67, 118)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(60, 13)
        Me.Label4.TabIndex = 25
        Me.Label4.Text = "Comentario"
        '
        'txtNombreOperador
        '
        Me.txtNombreOperador.Location = New System.Drawing.Point(133, 40)
        Me.txtNombreOperador.Name = "txtNombreOperador"
        Me.txtNombreOperador.ReadOnly = True
        Me.txtNombreOperador.Size = New System.Drawing.Size(281, 20)
        Me.txtNombreOperador.TabIndex = 2
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(76, 43)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(51, 13)
        Me.Label5.TabIndex = 21
        Me.Label5.Text = "Operador"
        '
        'txtTipoOperacion
        '
        Me.txtTipoOperacion.Location = New System.Drawing.Point(133, 66)
        Me.txtTipoOperacion.Name = "txtTipoOperacion"
        Me.txtTipoOperacion.ReadOnly = True
        Me.txtTipoOperacion.Size = New System.Drawing.Size(281, 20)
        Me.txtTipoOperacion.TabIndex = 3
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Cursor = System.Windows.Forms.Cursors.Default
        Me.Label6.Location = New System.Drawing.Point(49, 69)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(78, 13)
        Me.Label6.TabIndex = 22
        Me.Label6.Text = "Tipo operación"
        '
        'txtNombreFicheroOriginal
        '
        Me.txtNombreFicheroOriginal.Location = New System.Drawing.Point(133, 210)
        Me.txtNombreFicheroOriginal.Name = "txtNombreFicheroOriginal"
        Me.txtNombreFicheroOriginal.ReadOnly = True
        Me.txtNombreFicheroOriginal.Size = New System.Drawing.Size(281, 20)
        Me.txtNombreFicheroOriginal.TabIndex = 7
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(12, 213)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(115, 13)
        Me.Label7.TabIndex = 26
        Me.Label7.Text = "Nombre fichero original"
        '
        'txtEstado
        '
        Me.txtEstado.Location = New System.Drawing.Point(133, 236)
        Me.txtEstado.Name = "txtEstado"
        Me.txtEstado.ReadOnly = True
        Me.txtEstado.Size = New System.Drawing.Size(281, 20)
        Me.txtEstado.TabIndex = 9
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(20, 239)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(107, 13)
        Me.Label8.TabIndex = 27
        Me.Label8.Text = "Estado actual fichero"
        '
        'txtEntidadNegocio
        '
        Me.txtEntidadNegocio.Location = New System.Drawing.Point(133, 14)
        Me.txtEntidadNegocio.Name = "txtEntidadNegocio"
        Me.txtEntidadNegocio.ReadOnly = True
        Me.txtEntidadNegocio.Size = New System.Drawing.Size(281, 20)
        Me.txtEntidadNegocio.TabIndex = 1
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(28, 17)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(99, 13)
        Me.Label9.TabIndex = 20
        Me.Label9.Text = "Entidad de negocio"
        '
        'txtComentarioEntidad
        '
        Me.txtComentarioEntidad.Location = New System.Drawing.Point(133, 262)
        Me.txtComentarioEntidad.Multiline = True
        Me.txtComentarioEntidad.Name = "txtComentarioEntidad"
        Me.txtComentarioEntidad.ReadOnly = True
        Me.txtComentarioEntidad.Size = New System.Drawing.Size(426, 86)
        Me.txtComentarioEntidad.TabIndex = 10
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(67, 262)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(60, 13)
        Me.Label3.TabIndex = 28
        Me.Label3.Text = "Comentario"
        '
        'cmdAbrir
        '
        Me.cmdAbrir.Image = CType(resources.GetObject("cmdAbrir.Image"), System.Drawing.Image)
        Me.cmdAbrir.Location = New System.Drawing.Point(420, 208)
        Me.cmdAbrir.Name = "cmdAbrir"
        Me.cmdAbrir.Size = New System.Drawing.Size(35, 25)
        Me.cmdAbrir.TabIndex = 8
        Me.ToolTip.SetToolTip(Me.cmdAbrir, "Abrir fichero")
        Me.cmdAbrir.UseVisualStyleBackColor = True
        '
        'cmdAbrirFichero
        '
        Me.cmdAbrirFichero.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.cmdAbrirFichero.Image = CType(resources.GetObject("cmdAbrirFichero.Image"), System.Drawing.Image)
        Me.cmdAbrirFichero.Location = New System.Drawing.Point(335, 362)
        Me.cmdAbrirFichero.Name = "cmdAbrirFichero"
        Me.cmdAbrirFichero.Size = New System.Drawing.Size(90, 41)
        Me.cmdAbrirFichero.TabIndex = 49
        Me.cmdAbrirFichero.Text = "Abrir (F4)"
        Me.cmdAbrirFichero.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAbrirFichero.UseVisualStyleBackColor = True
        '
        'cmdCopiarRuta
        '
        Me.cmdCopiarRuta.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.cmdCopiarRuta.Image = CType(resources.GetObject("cmdCopiarRuta.Image"), System.Drawing.Image)
        Me.cmdCopiarRuta.Location = New System.Drawing.Point(226, 362)
        Me.cmdCopiarRuta.Name = "cmdCopiarRuta"
        Me.cmdCopiarRuta.Size = New System.Drawing.Size(103, 41)
        Me.cmdCopiarRuta.TabIndex = 51
        Me.cmdCopiarRuta.Text = "Ruta (F3)"
        Me.cmdCopiarRuta.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCopiarRuta.UseVisualStyleBackColor = True
        '
        'cmdCopiarID
        '
        Me.cmdCopiarID.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.cmdCopiarID.Image = CType(resources.GetObject("cmdCopiarID.Image"), System.Drawing.Image)
        Me.cmdCopiarID.Location = New System.Drawing.Point(133, 362)
        Me.cmdCopiarID.Name = "cmdCopiarID"
        Me.cmdCopiarID.Size = New System.Drawing.Size(87, 41)
        Me.cmdCopiarID.TabIndex = 50
        Me.cmdCopiarID.Text = "ID (F2)"
        Me.cmdCopiarID.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCopiarID.UseVisualStyleBackColor = True
        '
        'ctrlOperacionEnFichero
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.cmdAbrirFichero)
        Me.Controls.Add(Me.cmdCopiarRuta)
        Me.Controls.Add(Me.cmdCopiarID)
        Me.Controls.Add(Me.cmdAbrir)
        Me.Controls.Add(Me.txtComentarioEntidad)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.txtEntidadNegocio)
        Me.Controls.Add(Me.Label9)
        Me.Controls.Add(Me.txtEstado)
        Me.Controls.Add(Me.Label8)
        Me.Controls.Add(Me.txtNombreFicheroOriginal)
        Me.Controls.Add(Me.Label7)
        Me.Controls.Add(Me.txtTipoOperacion)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.txtNombreOperador)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.txtComentarioOperacion)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.txtFF)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.txtFI)
        Me.Controls.Add(Me.Label1)
        Me.Name = "ctrlOperacionEnFichero"
        Me.Size = New System.Drawing.Size(576, 415)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents txtFI As System.Windows.Forms.TextBox
    Friend WithEvents txtFF As System.Windows.Forms.TextBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents txtComentarioOperacion As System.Windows.Forms.TextBox
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents txtNombreOperador As System.Windows.Forms.TextBox
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents txtTipoOperacion As System.Windows.Forms.TextBox
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents txtNombreFicheroOriginal As System.Windows.Forms.TextBox
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents txtEstado As System.Windows.Forms.TextBox
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents txtEntidadNegocio As System.Windows.Forms.TextBox
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents txtComentarioEntidad As System.Windows.Forms.TextBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents cmdAbrir As System.Windows.Forms.Button
    Friend WithEvents cmdAbrirFichero As System.Windows.Forms.Button
    Friend WithEvents cmdCopiarRuta As System.Windows.Forms.Button
    Friend WithEvents cmdCopiarID As System.Windows.Forms.Button

End Class
