<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmContenedorRTF
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
        Me.ctrlContenedorRTF = New FN.GestionPagos.IU.ctrlContenedorRTF
        Me.cmd_Cancelar = New System.Windows.Forms.Button
        Me.cmd_Aceptar = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'ctrlContenedorRTF
        '
        Me.ctrlContenedorRTF.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ctrlContenedorRTF.ContenedorRTF = Nothing
        Me.ctrlContenedorRTF.Location = New System.Drawing.Point(3, 2)
        Me.ctrlContenedorRTF.MensajeError = "No hay contenido en el cuadro de texto"
        Me.ctrlContenedorRTF.Name = "ctrlContenedorRTF"
        Me.ctrlContenedorRTF.Size = New System.Drawing.Size(466, 378)
        Me.ctrlContenedorRTF.TabIndex = 0
        Me.ctrlContenedorRTF.ToolTipText = Nothing
        '
        'cmd_Cancelar
        '
        Me.cmd_Cancelar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmd_Cancelar.Image = Global.FN.GestionPagos.IU.My.Resources.Resources.delete_16
        Me.cmd_Cancelar.Location = New System.Drawing.Point(385, 395)
        Me.cmd_Cancelar.Name = "cmd_Cancelar"
        Me.cmd_Cancelar.Size = New System.Drawing.Size(75, 23)
        Me.cmd_Cancelar.TabIndex = 1
        Me.cmd_Cancelar.Text = "Cancelar"
        Me.cmd_Cancelar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmd_Cancelar.UseVisualStyleBackColor = True
        '
        'cmd_Aceptar
        '
        Me.cmd_Aceptar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmd_Aceptar.Image = Global.FN.GestionPagos.IU.My.Resources.Resources.check_16
        Me.cmd_Aceptar.Location = New System.Drawing.Point(304, 395)
        Me.cmd_Aceptar.Name = "cmd_Aceptar"
        Me.cmd_Aceptar.Size = New System.Drawing.Size(75, 23)
        Me.cmd_Aceptar.TabIndex = 2
        Me.cmd_Aceptar.Text = "Aceptar"
        Me.cmd_Aceptar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmd_Aceptar.UseVisualStyleBackColor = True
        '
        'frmContenedorRTF
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(472, 430)
        Me.Controls.Add(Me.cmd_Aceptar)
        Me.Controls.Add(Me.cmd_Cancelar)
        Me.Controls.Add(Me.ctrlContenedorRTF)
        Me.Name = "frmContenedorRTF"
        Me.Text = "Contenido RTF"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ctrlContenedorRTF As ctrlContenedorRTF
    Friend WithEvents cmd_Cancelar As System.Windows.Forms.Button
    Friend WithEvents cmd_Aceptar As System.Windows.Forms.Button
End Class
