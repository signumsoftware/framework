<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmArbol
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmArbol))
        Me.ArbolNododeT1 = New ControlesPGenericos.ArbolNododeT
        Me.cmdCancelar = New System.Windows.Forms.Button
        Me.cmd_Aceptar = New System.Windows.Forms.Button
        Me.Label1 = New System.Windows.Forms.Label
        Me.cmdTodos = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'ArbolNododeT1
        '
        Me.ArbolNododeT1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ArbolNododeT1.BackColor = System.Drawing.SystemColors.Control
        Me.ArbolNododeT1.Location = New System.Drawing.Point(13, 34)
        Me.ArbolNododeT1.MensajeError = ""
        Me.ArbolNododeT1.Name = "ArbolNododeT1"
        Me.ArbolNododeT1.Size = New System.Drawing.Size(338, 222)
        Me.ArbolNododeT1.TabIndex = 0
        Me.ArbolNododeT1.ToolTipText = Nothing
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.cmdCancelar.Image = CType(resources.GetObject("cmdCancelar.Image"), System.Drawing.Image)
        Me.cmdCancelar.Location = New System.Drawing.Point(272, 262)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.Size = New System.Drawing.Size(79, 23)
        Me.cmdCancelar.TabIndex = 2
        Me.cmdCancelar.Text = "Cancelar"
        Me.cmdCancelar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'cmd_Aceptar
        '
        Me.cmd_Aceptar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmd_Aceptar.Image = CType(resources.GetObject("cmd_Aceptar.Image"), System.Drawing.Image)
        Me.cmd_Aceptar.Location = New System.Drawing.Point(187, 262)
        Me.cmd_Aceptar.Name = "cmd_Aceptar"
        Me.cmd_Aceptar.Size = New System.Drawing.Size(79, 23)
        Me.cmd_Aceptar.TabIndex = 1
        Me.cmd_Aceptar.Text = "Aceptar"
        Me.cmd_Aceptar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmd_Aceptar.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(13, 13)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(167, 13)
        Me.Label1.TabIndex = 3
        Me.Label1.Text = "Seleccione un elemento del árbol:"
        '
        'cmdTodos
        '
        Me.cmdTodos.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.cmdTodos.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.cmdTodos.Image = CType(resources.GetObject("cmdTodos.Image"), System.Drawing.Image)
        Me.cmdTodos.Location = New System.Drawing.Point(12, 262)
        Me.cmdTodos.Name = "cmdTodos"
        Me.cmdTodos.Size = New System.Drawing.Size(125, 23)
        Me.cmdTodos.TabIndex = 4
        Me.cmdTodos.Text = "Selecionar Todos"
        Me.cmdTodos.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdTodos.UseVisualStyleBackColor = True
        '
        'frmArbol
        '
        Me.AcceptButton = Me.cmd_Aceptar
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.cmdCancelar
        Me.ClientSize = New System.Drawing.Size(363, 296)
        Me.Controls.Add(Me.cmdTodos)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.cmd_Aceptar)
        Me.Controls.Add(Me.ArbolNododeT1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmArbol"
        Me.Text = "Árbol de Categorías"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ArbolNododeT1 As ControlesPGenericos.ArbolNododeT
    Friend WithEvents cmd_Aceptar As System.Windows.Forms.Button
    Friend WithEvents cmdCancelar As System.Windows.Forms.Button
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents cmdTodos As System.Windows.Forms.Button
End Class
