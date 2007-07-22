<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmImagen
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmImagen))
        Me.cmd_Aceptar = New ControlesPBase.BotonP
        Me.cmdCancelar = New ControlesPBase.BotonP
        Me.ctrlImagen = New FN.GestionPagos.IU.ctrlImagen
        Me.SuspendLayout()
        '
        'cmd_Aceptar
        '
        Me.cmd_Aceptar.Image = Global.FN.GestionPagos.IU.My.Resources.Resources.check_16
        Me.cmd_Aceptar.Location = New System.Drawing.Point(194, 353)
        Me.cmd_Aceptar.Name = "cmd_Aceptar"
        Me.cmd_Aceptar.Size = New System.Drawing.Size(80, 23)
        Me.cmd_Aceptar.TabIndex = 1
        Me.cmd_Aceptar.Text = "Aceptar"
        Me.cmd_Aceptar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmd_Aceptar.UseVisualStyleBackColor = True
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Image = Global.FN.GestionPagos.IU.My.Resources.Resources.delete_16
        Me.cmdCancelar.Location = New System.Drawing.Point(280, 353)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.Size = New System.Drawing.Size(80, 23)
        Me.cmdCancelar.TabIndex = 2
        Me.cmdCancelar.Text = "Cancelar"
        Me.cmdCancelar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'ctrlImagen
        '
        Me.ctrlImagen.AutoSize = True
        Me.ctrlImagen.ContenedorImagen = Nothing
        Me.ctrlImagen.Location = New System.Drawing.Point(13, 13)
        Me.ctrlImagen.MensajeError = "No se ha seleccionado ninguna imagen"
        Me.ctrlImagen.Name = "ctrlImagen"
        Me.ctrlImagen.Size = New System.Drawing.Size(338, 310)
        Me.ctrlImagen.TabIndex = 0
        Me.ctrlImagen.ToolTipText = Nothing
        '
        'frmImagen
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(372, 384)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.cmd_Aceptar)
        Me.Controls.Add(Me.ctrlImagen)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmImagen"
        Me.Text = "Imagen embebida"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ctrlImagen As ctrlImagen
    Friend WithEvents cmd_Aceptar As ControlesPBase.BotonP
    Friend WithEvents cmdCancelar As ControlesPBase.BotonP
End Class
