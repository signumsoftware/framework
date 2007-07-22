<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmConfiguracionPuntosImpresion
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmConfiguracionPuntosImpresion))
        Me.ctrlConfiguracionPuntosImpresion1 = New FN.GestionPagos.IU.ctrlConfiguracionPuntosImpresion
        Me.cmdGuardar = New ControlesPBase.BotonP
        Me.cmdCancelar = New ControlesPBase.BotonP
        Me.cmdAceptar = New ControlesPBase.BotonP
        Me.cmdVistaPreliminar = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'ctrlConfiguracionPuntosImpresion1
        '
        Me.ctrlConfiguracionPuntosImpresion1.ConfiguracionImpresion = Nothing
        Me.ctrlConfiguracionPuntosImpresion1.Location = New System.Drawing.Point(13, 13)
        Me.ctrlConfiguracionPuntosImpresion1.MensajeError = "Debe seleccionar una fuente"
        Me.ctrlConfiguracionPuntosImpresion1.Name = "ctrlConfiguracionPuntosImpresion1"
        Me.ctrlConfiguracionPuntosImpresion1.Size = New System.Drawing.Size(641, 443)
        Me.ctrlConfiguracionPuntosImpresion1.TabIndex = 0
        Me.ctrlConfiguracionPuntosImpresion1.ToolTipText = Nothing
        '
        'cmdGuardar
        '
        Me.cmdGuardar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdGuardar.Image = CType(resources.GetObject("cmdGuardar.Image"), System.Drawing.Image)
        Me.cmdGuardar.Location = New System.Drawing.Point(400, 460)
        Me.cmdGuardar.Name = "cmdGuardar"
        Me.cmdGuardar.OcultarEnSalida = True
        Me.cmdGuardar.Size = New System.Drawing.Size(84, 23)
        Me.cmdGuardar.TabIndex = 8
        Me.cmdGuardar.Text = "Guardar"
        Me.cmdGuardar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdGuardar.UseVisualStyleBackColor = True
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCancelar.Image = CType(resources.GetObject("cmdCancelar.Image"), System.Drawing.Image)
        Me.cmdCancelar.Location = New System.Drawing.Point(580, 460)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.OcultarEnSalida = True
        Me.cmdCancelar.Size = New System.Drawing.Size(84, 23)
        Me.cmdCancelar.TabIndex = 7
        Me.cmdCancelar.Text = "Cancelar"
        Me.cmdCancelar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'cmdAceptar
        '
        Me.cmdAceptar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAceptar.Image = CType(resources.GetObject("cmdAceptar.Image"), System.Drawing.Image)
        Me.cmdAceptar.Location = New System.Drawing.Point(490, 460)
        Me.cmdAceptar.Name = "cmdAceptar"
        Me.cmdAceptar.OcultarEnSalida = True
        Me.cmdAceptar.Size = New System.Drawing.Size(84, 23)
        Me.cmdAceptar.TabIndex = 6
        Me.cmdAceptar.Text = "Aceptar"
        Me.cmdAceptar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAceptar.UseVisualStyleBackColor = True
        '
        'cmdVistaPreliminar
        '
        Me.cmdVistaPreliminar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdVistaPreliminar.Image = CType(resources.GetObject("cmdVistaPreliminar.Image"), System.Drawing.Image)
        Me.cmdVistaPreliminar.Location = New System.Drawing.Point(13, 450)
        Me.cmdVistaPreliminar.Name = "cmdVistaPreliminar"
        Me.cmdVistaPreliminar.Size = New System.Drawing.Size(115, 43)
        Me.cmdVistaPreliminar.TabIndex = 38
        Me.cmdVistaPreliminar.Text = "Probar Configuración"
        Me.cmdVistaPreliminar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdVistaPreliminar.UseVisualStyleBackColor = True
        '
        'frmConfiguracionPuntosImpresion
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(668, 495)
        Me.Controls.Add(Me.cmdVistaPreliminar)
        Me.Controls.Add(Me.cmdGuardar)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.cmdAceptar)
        Me.Controls.Add(Me.ctrlConfiguracionPuntosImpresion1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MinimumSize = New System.Drawing.Size(676, 529)
        Me.Name = "frmConfiguracionPuntosImpresion"
        Me.Text = "Configuración de Impresión para Talones"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ctrlConfiguracionPuntosImpresion1 As ctrlConfiguracionPuntosImpresion
    Friend WithEvents cmdGuardar As ControlesPBase.BotonP
    Friend WithEvents cmdCancelar As ControlesPBase.BotonP
    Friend WithEvents cmdAceptar As ControlesPBase.BotonP
    Friend WithEvents cmdVistaPreliminar As System.Windows.Forms.Button
End Class
