<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmAdministracionArbol
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
        Me.components = New System.ComponentModel.Container
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmAdministracionArbol))
        Me.ArbolNododeT1 = New ControlesPGenericos.ArbolNododeT
        Me.cmdAgregarCarpeta = New System.Windows.Forms.Button
        Me.cmdAgregarElemento = New System.Windows.Forms.Button
        Me.cmdEliminar = New System.Windows.Forms.Button
        Me.cmd_Aceptar = New System.Windows.Forms.Button
        Me.cmdCancelar = New System.Windows.Forms.Button
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.SuspendLayout()
        '
        'ArbolNododeT1
        '
        Me.ArbolNododeT1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ArbolNododeT1.BackColor = System.Drawing.SystemColors.Control
        Me.ArbolNododeT1.Location = New System.Drawing.Point(12, 12)
        Me.ArbolNododeT1.MensajeError = ""
        Me.ArbolNododeT1.Name = "ArbolNododeT1"
        Me.ArbolNododeT1.Size = New System.Drawing.Size(294, 271)
        Me.ArbolNododeT1.TabIndex = 0
        Me.ArbolNododeT1.ToolTipText = Nothing
        '
        'cmdAgregarCarpeta
        '
        Me.cmdAgregarCarpeta.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAgregarCarpeta.Image = CType(resources.GetObject("cmdAgregarCarpeta.Image"), System.Drawing.Image)
        Me.cmdAgregarCarpeta.Location = New System.Drawing.Point(312, 13)
        Me.cmdAgregarCarpeta.Name = "cmdAgregarCarpeta"
        Me.cmdAgregarCarpeta.Size = New System.Drawing.Size(88, 41)
        Me.cmdAgregarCarpeta.TabIndex = 1
        Me.cmdAgregarCarpeta.Text = "Agregar Carpeta"
        Me.cmdAgregarCarpeta.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAgregarCarpeta.UseVisualStyleBackColor = True
        '
        'cmdAgregarElemento
        '
        Me.cmdAgregarElemento.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAgregarElemento.Image = CType(resources.GetObject("cmdAgregarElemento.Image"), System.Drawing.Image)
        Me.cmdAgregarElemento.Location = New System.Drawing.Point(312, 67)
        Me.cmdAgregarElemento.Name = "cmdAgregarElemento"
        Me.cmdAgregarElemento.Size = New System.Drawing.Size(88, 41)
        Me.cmdAgregarElemento.TabIndex = 2
        Me.cmdAgregarElemento.Text = "Agregar Elemento"
        Me.cmdAgregarElemento.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAgregarElemento.UseVisualStyleBackColor = True
        '
        'cmdEliminar
        '
        Me.cmdEliminar.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdEliminar.Image = CType(resources.GetObject("cmdEliminar.Image"), System.Drawing.Image)
        Me.cmdEliminar.Location = New System.Drawing.Point(312, 121)
        Me.cmdEliminar.Name = "cmdEliminar"
        Me.cmdEliminar.Size = New System.Drawing.Size(88, 41)
        Me.cmdEliminar.TabIndex = 3
        Me.cmdEliminar.Text = "Eliminar"
        Me.cmdEliminar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdEliminar.UseVisualStyleBackColor = True
        '
        'cmdAceptar
        '
        Me.cmd_Aceptar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmd_Aceptar.Image = CType(resources.GetObject("cmdAceptar.Image"), System.Drawing.Image)
        Me.cmd_Aceptar.Location = New System.Drawing.Point(231, 301)
        Me.cmd_Aceptar.Name = "cmdAceptar"
        Me.cmd_Aceptar.Size = New System.Drawing.Size(75, 23)
        Me.cmd_Aceptar.TabIndex = 4
        Me.cmd_Aceptar.Text = "Aceptar"
        Me.cmd_Aceptar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmd_Aceptar.UseVisualStyleBackColor = True
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCancelar.Image = CType(resources.GetObject("cmdCancelar.Image"), System.Drawing.Image)
        Me.cmdCancelar.Location = New System.Drawing.Point(322, 301)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.Size = New System.Drawing.Size(75, 23)
        Me.cmdCancelar.TabIndex = 5
        Me.cmdCancelar.Text = "Cancelar"
        Me.cmdCancelar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'ImageList1
        '
        Me.ImageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit
        Me.ImageList1.ImageSize = New System.Drawing.Size(16, 16)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        '
        'frmAdministracionArbol
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(409, 336)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.cmd_Aceptar)
        Me.Controls.Add(Me.cmdEliminar)
        Me.Controls.Add(Me.cmdAgregarElemento)
        Me.Controls.Add(Me.cmdAgregarCarpeta)
        Me.Controls.Add(Me.ArbolNododeT1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MinimumSize = New System.Drawing.Size(400, 370)
        Me.Name = "frmAdministracionArbol"
        Me.Text = "Administración del Árbol de Elementos"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ArbolNododeT1 As ControlesPGenericos.ArbolNododeT
    Friend WithEvents cmdAgregarCarpeta As System.Windows.Forms.Button
    Friend WithEvents cmdAgregarElemento As System.Windows.Forms.Button
    Friend WithEvents cmdEliminar As System.Windows.Forms.Button
    Friend WithEvents cmd_Aceptar As System.Windows.Forms.Button
    Friend WithEvents cmdCancelar As System.Windows.Forms.Button
    Friend WithEvents ImageList1 As System.Windows.Forms.ImageList
End Class
