<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmOperacionEnFichero
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmOperacionEnFichero))
        Me.CtrlOperacionEnFichero1 = New ClienteAdmin.ctrlOperacionEnFichero
        Me.cmdAceptar = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'CtrlOperacionEnFichero1
        '
        Me.CtrlOperacionEnFichero1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.CtrlOperacionEnFichero1.Location = New System.Drawing.Point(2, 2)
        Me.CtrlOperacionEnFichero1.MensajeError = ""
        Me.CtrlOperacionEnFichero1.Name = "CtrlOperacionEnFichero1"
        Me.CtrlOperacionEnFichero1.Size = New System.Drawing.Size(581, 414)
        Me.CtrlOperacionEnFichero1.TabIndex = 0
        Me.CtrlOperacionEnFichero1.ToolTipText = Nothing
        '
        'cmdAceptar
        '
        Me.cmdAceptar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAceptar.Image = CType(resources.GetObject("cmdAceptar.Image"), System.Drawing.Image)
        Me.cmdAceptar.Location = New System.Drawing.Point(483, 425)
        Me.cmdAceptar.Name = "cmdAceptar"
        Me.cmdAceptar.Size = New System.Drawing.Size(75, 23)
        Me.cmdAceptar.TabIndex = 1
        Me.cmdAceptar.Text = "Aceptar"
        Me.cmdAceptar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAceptar.UseVisualStyleBackColor = True
        '
        'frmOperacionEnFichero
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(580, 457)
        Me.Controls.Add(Me.cmdAceptar)
        Me.Controls.Add(Me.CtrlOperacionEnFichero1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.MaximizeBox = False
        Me.Name = "frmOperacionEnFichero"
        Me.Text = "Operación"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents CtrlOperacionEnFichero1 As ClienteAdmin.ctrlOperacionEnFichero
    Friend WithEvents cmdAceptar As System.Windows.Forms.Button
End Class
