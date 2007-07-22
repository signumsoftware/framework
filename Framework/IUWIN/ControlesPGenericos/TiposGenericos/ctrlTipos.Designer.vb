<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Public Class ctrlTipos
    Inherits MotorIU.ControlesP.BaseControlP

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
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
        Me.CtrlListadoTiposBaja = New ControlesPGenericos.ctrlListadoTipos
        Me.CtrlListadoTiposAlta = New ControlesPGenericos.ctrlListadoTipos
        Me.CtrlTiposDetalle1 = New ControlesPGenericos.ctrlTiposDetalle
        Me.lblTitulo = New System.Windows.Forms.Label
        Me.btnNuevoTipo = New System.Windows.Forms.Button
        Me.btnBaja = New System.Windows.Forms.Button
        Me.btnAltaTipo = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'CtrlListadoTiposBaja
        '
        Me.CtrlListadoTiposBaja.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.CtrlListadoTiposBaja.BackColor = System.Drawing.SystemColors.Control
        Me.CtrlListadoTiposBaja.Location = New System.Drawing.Point(9, 242)
        Me.CtrlListadoTiposBaja.MensajeError = ""
        Me.CtrlListadoTiposBaja.Name = "CtrlListadoTiposBaja"
        Me.CtrlListadoTiposBaja.Size = New System.Drawing.Size(191, 211)
        Me.CtrlListadoTiposBaja.TabIndex = 0
        Me.CtrlListadoTiposBaja.ToolTipText = Nothing
        '
        'CtrlListadoTiposAlta
        '
        Me.CtrlListadoTiposAlta.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.CtrlListadoTiposAlta.BackColor = System.Drawing.SystemColors.Control
        Me.CtrlListadoTiposAlta.Location = New System.Drawing.Point(9, 19)
        Me.CtrlListadoTiposAlta.MensajeError = ""
        Me.CtrlListadoTiposAlta.Name = "CtrlListadoTiposAlta"
        Me.CtrlListadoTiposAlta.Size = New System.Drawing.Size(191, 211)
        Me.CtrlListadoTiposAlta.TabIndex = 1
        Me.CtrlListadoTiposAlta.ToolTipText = Nothing
        '
        'CtrlTiposDetalle1
        '
        Me.CtrlTiposDetalle1.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.CtrlTiposDetalle1.BackColor = System.Drawing.SystemColors.Control
        Me.CtrlTiposDetalle1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.CtrlTiposDetalle1.Location = New System.Drawing.Point(280, 19)
        Me.CtrlTiposDetalle1.MensajeError = ""
        Me.CtrlTiposDetalle1.Name = "CtrlTiposDetalle1"
        Me.CtrlTiposDetalle1.Size = New System.Drawing.Size(236, 74)
        Me.CtrlTiposDetalle1.TabIndex = 2
        Me.CtrlTiposDetalle1.ToolTipText = Nothing
        '
        'lblTitulo
        '
        Me.lblTitulo.AutoSize = True
        Me.lblTitulo.Location = New System.Drawing.Point(3, 0)
        Me.lblTitulo.Name = "lblTitulo"
        Me.lblTitulo.Size = New System.Drawing.Size(28, 13)
        Me.lblTitulo.TabIndex = 50
        Me.lblTitulo.Text = "Tipo"
        '
        'btnNuevoTipo
        '
        Me.btnNuevoTipo.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnNuevoTipo.Image = Global.ControlesPGenericos.My.Resources.Resources.cube_yellow_add
        Me.btnNuevoTipo.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnNuevoTipo.Location = New System.Drawing.Point(206, 19)
        Me.btnNuevoTipo.Name = "btnNuevoTipo"
        Me.btnNuevoTipo.Size = New System.Drawing.Size(68, 23)
        Me.btnNuevoTipo.TabIndex = 51
        Me.btnNuevoTipo.Text = "Nuevo"
        Me.btnNuevoTipo.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnNuevoTipo.UseVisualStyleBackColor = True
        '
        'btnBaja
        '
        Me.btnBaja.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBaja.Image = Global.ControlesPGenericos.My.Resources.Resources.cube_yellow_delete
        Me.btnBaja.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnBaja.Location = New System.Drawing.Point(206, 48)
        Me.btnBaja.Name = "btnBaja"
        Me.btnBaja.Size = New System.Drawing.Size(68, 23)
        Me.btnBaja.TabIndex = 52
        Me.btnBaja.Text = "Baja"
        Me.btnBaja.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnBaja.UseVisualStyleBackColor = True
        '
        'btnAltaTipo
        '
        Me.btnAltaTipo.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnAltaTipo.Image = Global.ControlesPGenericos.My.Resources.Resources.cube_yellow_preferences
        Me.btnAltaTipo.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnAltaTipo.Location = New System.Drawing.Point(206, 242)
        Me.btnAltaTipo.Name = "btnAltaTipo"
        Me.btnAltaTipo.Size = New System.Drawing.Size(68, 23)
        Me.btnAltaTipo.TabIndex = 53
        Me.btnAltaTipo.Text = "Alta"
        Me.btnAltaTipo.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnAltaTipo.UseVisualStyleBackColor = True
        '
        'ctrlTipos
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.btnAltaTipo)
        Me.Controls.Add(Me.btnBaja)
        Me.Controls.Add(Me.btnNuevoTipo)
        Me.Controls.Add(Me.lblTitulo)
        Me.Controls.Add(Me.CtrlTiposDetalle1)
        Me.Controls.Add(Me.CtrlListadoTiposAlta)
        Me.Controls.Add(Me.CtrlListadoTiposBaja)
        Me.Name = "ctrlTipos"
        Me.Size = New System.Drawing.Size(531, 467)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents CtrlListadoTiposBaja As ctrlListadoTipos
    Friend WithEvents CtrlListadoTiposAlta As ctrlListadoTipos
    Friend WithEvents CtrlTiposDetalle1 As ctrlTiposDetalle
    Friend WithEvents lblTitulo As System.Windows.Forms.Label
    Friend WithEvents btnNuevoTipo As System.Windows.Forms.Button
    Friend WithEvents btnBaja As System.Windows.Forms.Button
    Friend WithEvents btnAltaTipo As System.Windows.Forms.Button

End Class
