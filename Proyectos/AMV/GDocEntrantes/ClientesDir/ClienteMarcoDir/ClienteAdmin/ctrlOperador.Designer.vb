<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlOperador
    Inherits MotorIU.ControlesP.BaseControlP

    'UserControl overrides dispose to clean up the component list.
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ctrlOperador))
        Me.ArbolNododeTxLista1 = New ControlesPGenericos.ArbolNododeTxLista
        Me.gbDatosOperador = New System.Windows.Forms.GroupBox
        Me.btnAgregarMail = New System.Windows.Forms.Button
        Me.lsbMails = New System.Windows.Forms.ListBox
        Me.btnEliminarMail = New System.Windows.Forms.Button
        Me.lblMail = New System.Windows.Forms.Label
        Me.txtMail = New System.Windows.Forms.TextBox
        Me.txtFechaBaja = New System.Windows.Forms.TextBox
        Me.txtFechaAlta = New System.Windows.Forms.TextBox
        Me.cbBaja = New System.Windows.Forms.CheckBox
        Me.txtNombre = New System.Windows.Forms.TextBox
        Me.lblFBaja = New System.Windows.Forms.Label
        Me.lblFAlta = New System.Windows.Forms.Label
        Me.lblNombre = New System.Windows.Forms.Label
        Me.gbEntidadesNegocio = New System.Windows.Forms.GroupBox
        Me.gbDatosOperador.SuspendLayout()
        Me.gbEntidadesNegocio.SuspendLayout()
        Me.SuspendLayout()
        '
        'ArbolNododeTxLista1
        '
        Me.ArbolNododeTxLista1.BackColor = System.Drawing.SystemColors.Control
        Me.ArbolNododeTxLista1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ArbolNododeTxLista1.Location = New System.Drawing.Point(3, 16)
        Me.ArbolNododeTxLista1.MensajeError = ""
        Me.ArbolNododeTxLista1.Name = "ArbolNododeTxLista1"
        Me.ArbolNododeTxLista1.Size = New System.Drawing.Size(651, 372)
        Me.ArbolNododeTxLista1.TabIndex = 5
        Me.ArbolNododeTxLista1.ToolTipText = Nothing
        '
        'gbDatosOperador
        '
        Me.gbDatosOperador.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gbDatosOperador.Controls.Add(Me.btnAgregarMail)
        Me.gbDatosOperador.Controls.Add(Me.lsbMails)
        Me.gbDatosOperador.Controls.Add(Me.btnEliminarMail)
        Me.gbDatosOperador.Controls.Add(Me.lblMail)
        Me.gbDatosOperador.Controls.Add(Me.txtMail)
        Me.gbDatosOperador.Controls.Add(Me.txtFechaBaja)
        Me.gbDatosOperador.Controls.Add(Me.txtFechaAlta)
        Me.gbDatosOperador.Controls.Add(Me.cbBaja)
        Me.gbDatosOperador.Controls.Add(Me.txtNombre)
        Me.gbDatosOperador.Controls.Add(Me.lblFBaja)
        Me.gbDatosOperador.Controls.Add(Me.lblFAlta)
        Me.gbDatosOperador.Controls.Add(Me.lblNombre)
        Me.gbDatosOperador.Location = New System.Drawing.Point(3, 2)
        Me.gbDatosOperador.Name = "gbDatosOperador"
        Me.gbDatosOperador.Size = New System.Drawing.Size(657, 126)
        Me.gbDatosOperador.TabIndex = 1
        Me.gbDatosOperador.TabStop = False
        Me.gbDatosOperador.Text = "Datos del operador"
        '
        'btnAgregarMail
        '
        Me.btnAgregarMail.Image = CType(resources.GetObject("btnAgregarMail.Image"), System.Drawing.Image)
        Me.btnAgregarMail.Location = New System.Drawing.Point(613, 19)
        Me.btnAgregarMail.Name = "btnAgregarMail"
        Me.btnAgregarMail.Size = New System.Drawing.Size(29, 23)
        Me.btnAgregarMail.TabIndex = 11
        Me.btnAgregarMail.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText
        Me.ToolTip.SetToolTip(Me.btnAgregarMail, "Agregar eMail")
        Me.btnAgregarMail.UseVisualStyleBackColor = True
        '
        'lsbMails
        '
        Me.lsbMails.FormattingEnabled = True
        Me.lsbMails.Location = New System.Drawing.Point(376, 47)
        Me.lsbMails.Name = "lsbMails"
        Me.lsbMails.Size = New System.Drawing.Size(234, 69)
        Me.lsbMails.TabIndex = 7
        '
        'btnEliminarMail
        '
        Me.btnEliminarMail.Image = CType(resources.GetObject("btnEliminarMail.Image"), System.Drawing.Image)
        Me.btnEliminarMail.Location = New System.Drawing.Point(613, 46)
        Me.btnEliminarMail.Name = "btnEliminarMail"
        Me.btnEliminarMail.Size = New System.Drawing.Size(29, 23)
        Me.btnEliminarMail.TabIndex = 12
        Me.ToolTip.SetToolTip(Me.btnEliminarMail, "Eliminar eMail")
        Me.btnEliminarMail.UseVisualStyleBackColor = True
        '
        'lblMail
        '
        Me.lblMail.AutoSize = True
        Me.lblMail.Location = New System.Drawing.Point(335, 23)
        Me.lblMail.Name = "lblMail"
        Me.lblMail.Size = New System.Drawing.Size(35, 13)
        Me.lblMail.TabIndex = 6
        Me.lblMail.Text = "e-Mail"
        '
        'txtMail
        '
        Me.txtMail.Location = New System.Drawing.Point(376, 20)
        Me.txtMail.Name = "txtMail"
        Me.txtMail.Size = New System.Drawing.Size(234, 20)
        Me.txtMail.TabIndex = 5
        '
        'txtFechaBaja
        '
        Me.txtFechaBaja.Location = New System.Drawing.Point(85, 71)
        Me.txtFechaBaja.Name = "txtFechaBaja"
        Me.txtFechaBaja.ReadOnly = True
        Me.txtFechaBaja.Size = New System.Drawing.Size(234, 20)
        Me.txtFechaBaja.TabIndex = 3
        '
        'txtFechaAlta
        '
        Me.txtFechaAlta.Location = New System.Drawing.Point(85, 46)
        Me.txtFechaAlta.Name = "txtFechaAlta"
        Me.txtFechaAlta.ReadOnly = True
        Me.txtFechaAlta.Size = New System.Drawing.Size(234, 20)
        Me.txtFechaAlta.TabIndex = 2
        '
        'cbBaja
        '
        Me.cbBaja.AutoCheck = False
        Me.cbBaja.AutoSize = True
        Me.cbBaja.Location = New System.Drawing.Point(85, 98)
        Me.cbBaja.Name = "cbBaja"
        Me.cbBaja.Size = New System.Drawing.Size(47, 17)
        Me.cbBaja.TabIndex = 4
        Me.cbBaja.Text = "Baja"
        Me.cbBaja.UseVisualStyleBackColor = True
        '
        'txtNombre
        '
        Me.txtNombre.Location = New System.Drawing.Point(85, 20)
        Me.txtNombre.Name = "txtNombre"
        Me.txtNombre.Size = New System.Drawing.Size(234, 20)
        Me.txtNombre.TabIndex = 1
        '
        'lblFBaja
        '
        Me.lblFBaja.AutoSize = True
        Me.lblFBaja.Location = New System.Drawing.Point(15, 74)
        Me.lblFBaja.Name = "lblFBaja"
        Me.lblFBaja.Size = New System.Drawing.Size(60, 13)
        Me.lblFBaja.TabIndex = 2
        Me.lblFBaja.Text = "Fecha baja"
        '
        'lblFAlta
        '
        Me.lblFAlta.AutoSize = True
        Me.lblFAlta.Location = New System.Drawing.Point(18, 49)
        Me.lblFAlta.Name = "lblFAlta"
        Me.lblFAlta.Size = New System.Drawing.Size(57, 13)
        Me.lblFAlta.TabIndex = 1
        Me.lblFAlta.Text = "Fecha alta"
        '
        'lblNombre
        '
        Me.lblNombre.AutoSize = True
        Me.lblNombre.Location = New System.Drawing.Point(31, 23)
        Me.lblNombre.Name = "lblNombre"
        Me.lblNombre.Size = New System.Drawing.Size(44, 13)
        Me.lblNombre.TabIndex = 0
        Me.lblNombre.Text = "Nombre"
        '
        'gbEntidadesNegocio
        '
        Me.gbEntidadesNegocio.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gbEntidadesNegocio.Controls.Add(Me.ArbolNododeTxLista1)
        Me.gbEntidadesNegocio.Location = New System.Drawing.Point(3, 134)
        Me.gbEntidadesNegocio.Name = "gbEntidadesNegocio"
        Me.gbEntidadesNegocio.Size = New System.Drawing.Size(657, 391)
        Me.gbEntidadesNegocio.TabIndex = 2
        Me.gbEntidadesNegocio.TabStop = False
        Me.gbEntidadesNegocio.Text = "Entidades de negocio del operador"
        '
        'ctrlOperador
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.gbEntidadesNegocio)
        Me.Controls.Add(Me.gbDatosOperador)
        Me.Name = "ctrlOperador"
        Me.Size = New System.Drawing.Size(663, 528)
        Me.gbDatosOperador.ResumeLayout(False)
        Me.gbDatosOperador.PerformLayout()
        Me.gbEntidadesNegocio.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ArbolNododeTxLista1 As ControlesPGenericos.ArbolNododeTxLista
    Friend WithEvents gbDatosOperador As System.Windows.Forms.GroupBox
    Friend WithEvents gbEntidadesNegocio As System.Windows.Forms.GroupBox
    Friend WithEvents txtNombre As System.Windows.Forms.TextBox
    Friend WithEvents lblFBaja As System.Windows.Forms.Label
    Friend WithEvents lblFAlta As System.Windows.Forms.Label
    Friend WithEvents lblNombre As System.Windows.Forms.Label
    Friend WithEvents cbBaja As System.Windows.Forms.CheckBox
    Friend WithEvents txtFechaBaja As System.Windows.Forms.TextBox
    Friend WithEvents txtFechaAlta As System.Windows.Forms.TextBox
    Friend WithEvents txtMail As System.Windows.Forms.TextBox
    Friend WithEvents lsbMails As System.Windows.Forms.ListBox
    Friend WithEvents lblMail As System.Windows.Forms.Label
    Friend WithEvents btnAgregarMail As System.Windows.Forms.Button
    Friend WithEvents btnEliminarMail As System.Windows.Forms.Button

End Class
