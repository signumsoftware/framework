<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmListaElementos
    Inherits MotorIU.FormulariosP.FormularioBase

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.ListaResizeable1 = New ControlesPGenericos.ListaResizeable
        Me.cmdAceptar = New ControlesPBase.BotonP
        Me.cmdCancelar = New ControlesPBase.BotonP
        Me.SuspendLayout()
        '
        'ListaResizeable1
        '
        Me.ListaResizeable1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ListaResizeable1.Location = New System.Drawing.Point(13, 13)
        Me.ListaResizeable1.MensajeError = ""
        Me.ListaResizeable1.Name = "ListaResizeable1"
        Me.ListaResizeable1.Size = New System.Drawing.Size(326, 159)
        Me.ListaResizeable1.TabIndex = 0
        Me.ListaResizeable1.ToolTipText = Nothing
        '
        'cmdAceptar
        '
        Me.cmdAceptar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAceptar.Location = New System.Drawing.Point(263, 182)
        Me.cmdAceptar.Name = "cmdAceptar"
        Me.cmdAceptar.Size = New System.Drawing.Size(75, 23)
        Me.cmdAceptar.TabIndex = 1
        Me.cmdAceptar.Text = "Aceptar"
        Me.cmdAceptar.UseVisualStyleBackColor = True
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCancelar.Location = New System.Drawing.Point(182, 182)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.OcultarEnSalida = True
        Me.cmdCancelar.Size = New System.Drawing.Size(75, 23)
        Me.cmdCancelar.TabIndex = 2
        Me.cmdCancelar.Text = "Cancelar"
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'frmTelefonos
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(351, 217)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.cmdAceptar)
        Me.Controls.Add(Me.ListaResizeable1)
        Me.Name = "frmTelefonos"
        Me.Text = "Lista de Elementos"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ListaResizeable1 As ControlesPGenericos.ListaResizeable
    Friend WithEvents cmdAceptar As ControlesPBase.BotonP
    Friend WithEvents cmdCancelar As ControlesPBase.BotonP
End Class
