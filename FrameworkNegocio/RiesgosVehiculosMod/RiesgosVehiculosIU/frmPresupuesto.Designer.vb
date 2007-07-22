<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmPresupuesto
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
        Me.ctrlPresupuesto1 = New FN.RiesgosVehiculos.IU.Controles.ctrlPresupuesto
        Me.cmd_Aceptar = New ControlesPBase.BotonP
        Me.cmd_Cancelar = New ControlesPBase.BotonP
        Me.SuspendLayout()
        '
        'ctrlPresupuesto1
        '
        Me.ctrlPresupuesto1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ctrlPresupuesto1.Location = New System.Drawing.Point(10, 3)
        Me.ctrlPresupuesto1.MensajeError = ""
        Me.ctrlPresupuesto1.Name = "ctrlPresupuesto1"
        Me.ctrlPresupuesto1.Size = New System.Drawing.Size(682, 653)
        Me.ctrlPresupuesto1.TabIndex = 0
        Me.ctrlPresupuesto1.ToolTipText = Nothing
        '
        'cmd_Aceptar
        '
        Me.cmd_Aceptar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmd_Aceptar.Location = New System.Drawing.Point(608, 662)
        Me.cmd_Aceptar.Name = "cmd_Aceptar"
        Me.cmd_Aceptar.Size = New System.Drawing.Size(75, 23)
        Me.cmd_Aceptar.TabIndex = 1
        Me.cmd_Aceptar.Text = "Aceptar"
        Me.cmd_Aceptar.UseVisualStyleBackColor = True
        '
        'cmd_Cancelar
        '
        Me.cmd_Cancelar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmd_Cancelar.Location = New System.Drawing.Point(527, 662)
        Me.cmd_Cancelar.Name = "cmd_Cancelar"
        Me.cmd_Cancelar.Size = New System.Drawing.Size(75, 23)
        Me.cmd_Cancelar.TabIndex = 2
        Me.cmd_Cancelar.Text = "Cancelar"
        Me.cmd_Cancelar.UseVisualStyleBackColor = True
        '
        'frmPresupuesto
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(695, 697)
        Me.Controls.Add(Me.cmd_Cancelar)
        Me.Controls.Add(Me.cmd_Aceptar)
        Me.Controls.Add(Me.ctrlPresupuesto1)
        Me.Name = "frmPresupuesto"
        Me.Text = "Presupuesto"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ctrlPresupuesto1 As RiesgosVehiculos.IU.Controles.ctrlPresupuesto
    Friend WithEvents cmd_Aceptar As ControlesPBase.BotonP
    Friend WithEvents cmd_Cancelar As ControlesPBase.BotonP

End Class
