<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmPlantillaCarta
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmPlantillaCarta))
        Me.ctrlPlantillaTextoCarta1 = New FN.GestionPagos.IU.ctrlPlantillaTextoCarta
        Me.cmdAceptar = New ControlesPBase.BotonP
        Me.cmdCancelar = New ControlesPBase.BotonP
        Me.cmdGuardar = New ControlesPBase.BotonP
        Me.SuspendLayout()
        '
        'ctrlPlantillaTextoCarta1
        '
        Me.ctrlPlantillaTextoCarta1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ctrlPlantillaTextoCarta1.Location = New System.Drawing.Point(1, 2)
        Me.ctrlPlantillaTextoCarta1.MensajeError = "No se ha definido un nombre para la plantilla"
        Me.ctrlPlantillaTextoCarta1.Name = "ctrlPlantillaTextoCarta1"
        Me.ctrlPlantillaTextoCarta1.PlantillaCarta = Nothing
        Me.ctrlPlantillaTextoCarta1.Size = New System.Drawing.Size(521, 338)
        Me.ctrlPlantillaTextoCarta1.TabIndex = 0
        Me.ctrlPlantillaTextoCarta1.ToolTipText = Nothing
        '
        'cmdAceptar
        '
        Me.cmdAceptar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAceptar.Image = CType(resources.GetObject("cmdAceptar.Image"), System.Drawing.Image)
        Me.cmdAceptar.Location = New System.Drawing.Point(334, 346)
        Me.cmdAceptar.Name = "cmdAceptar"
        Me.cmdAceptar.Size = New System.Drawing.Size(84, 23)
        Me.cmdAceptar.TabIndex = 3
        Me.cmdAceptar.Text = "Aceptar"
        Me.cmdAceptar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdAceptar.UseVisualStyleBackColor = True
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCancelar.Image = CType(resources.GetObject("cmdCancelar.Image"), System.Drawing.Image)
        Me.cmdCancelar.Location = New System.Drawing.Point(424, 346)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.Size = New System.Drawing.Size(84, 23)
        Me.cmdCancelar.TabIndex = 4
        Me.cmdCancelar.Text = "Cancelar"
        Me.cmdCancelar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'cmdGuardar
        '
        Me.cmdGuardar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdGuardar.Image = CType(resources.GetObject("cmdGuardar.Image"), System.Drawing.Image)
        Me.cmdGuardar.Location = New System.Drawing.Point(244, 346)
        Me.cmdGuardar.Name = "cmdGuardar"
        Me.cmdGuardar.Size = New System.Drawing.Size(84, 23)
        Me.cmdGuardar.TabIndex = 5
        Me.cmdGuardar.Text = "Guardar"
        Me.cmdGuardar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.cmdGuardar.UseVisualStyleBackColor = True
        '
        'frmPlantillaCarta
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(520, 377)
        Me.Controls.Add(Me.cmdGuardar)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.cmdAceptar)
        Me.Controls.Add(Me.ctrlPlantillaTextoCarta1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmPlantillaCarta"
        Me.Text = "Plantilla de Carta Modelo"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ctrlPlantillaTextoCarta1 As ctrlPlantillaTextoCarta
    Friend WithEvents cmdAceptar As ControlesPBase.BotonP
    Friend WithEvents cmdCancelar As ControlesPBase.BotonP
    Friend WithEvents cmdGuardar As ControlesPBase.BotonP
End Class
