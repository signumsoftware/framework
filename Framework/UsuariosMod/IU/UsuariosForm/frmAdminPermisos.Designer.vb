<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmAdminPermisos
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmAdminPermisos))
        Me.cmdCancelar = New System.Windows.Forms.Button
        Me.cmdAceptar = New System.Windows.Forms.Button
        Me.lsbRoles = New System.Windows.Forms.ListBox
        Me.gbRoles = New System.Windows.Forms.GroupBox
        Me.gbPermisos = New System.Windows.Forms.GroupBox
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
        Me.clbCasosUso = New System.Windows.Forms.CheckedListBox
        Me.clbMetodosSistema = New System.Windows.Forms.CheckedListBox
        Me.gbAgregarRol = New System.Windows.Forms.GroupBox
        Me.lblNombreRol = New System.Windows.Forms.Label
        Me.btnNuevoRol = New System.Windows.Forms.Button
        Me.txtNombreRol = New System.Windows.Forms.TextBox
        Me.gbAgregarCasoUso = New System.Windows.Forms.GroupBox
        Me.Label1 = New System.Windows.Forms.Label
        Me.btnNuevoCasoUso = New System.Windows.Forms.Button
        Me.txtNombreCasoUso = New System.Windows.Forms.TextBox
        Me.Button1 = New System.Windows.Forms.Button
        Me.gbRoles.SuspendLayout()
        Me.gbPermisos.SuspendLayout()
        Me.TableLayoutPanel1.SuspendLayout()
        Me.gbAgregarRol.SuspendLayout()
        Me.gbAgregarCasoUso.SuspendLayout()
        Me.SuspendLayout()
        '
        'cmdCancelar
        '
        Me.cmdCancelar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdCancelar.Image = CType(resources.GetObject("cmdCancelar.Image"), System.Drawing.Image)
        Me.cmdCancelar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdCancelar.Location = New System.Drawing.Point(681, 387)
        Me.cmdCancelar.Name = "cmdCancelar"
        Me.cmdCancelar.Size = New System.Drawing.Size(79, 24)
        Me.cmdCancelar.TabIndex = 5
        Me.cmdCancelar.Text = "Cancelar"
        Me.cmdCancelar.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.cmdCancelar.UseVisualStyleBackColor = True
        '
        'cmdAceptar
        '
        Me.cmdAceptar.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAceptar.Image = CType(resources.GetObject("cmdAceptar.Image"), System.Drawing.Image)
        Me.cmdAceptar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdAceptar.Location = New System.Drawing.Point(596, 387)
        Me.cmdAceptar.Name = "cmdAceptar"
        Me.cmdAceptar.Size = New System.Drawing.Size(79, 24)
        Me.cmdAceptar.TabIndex = 4
        Me.cmdAceptar.Text = "Aceptar"
        Me.cmdAceptar.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.cmdAceptar.UseVisualStyleBackColor = True
        '
        'lsbRoles
        '
        Me.lsbRoles.DisplayMember = "ToString()"
        Me.lsbRoles.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lsbRoles.FormattingEnabled = True
        Me.lsbRoles.Location = New System.Drawing.Point(3, 16)
        Me.lsbRoles.Name = "lsbRoles"
        Me.lsbRoles.Size = New System.Drawing.Size(472, 108)
        Me.lsbRoles.TabIndex = 6
        '
        'gbRoles
        '
        Me.gbRoles.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gbRoles.Controls.Add(Me.lsbRoles)
        Me.gbRoles.Location = New System.Drawing.Point(12, 1)
        Me.gbRoles.Name = "gbRoles"
        Me.gbRoles.Size = New System.Drawing.Size(478, 138)
        Me.gbRoles.TabIndex = 7
        Me.gbRoles.TabStop = False
        Me.gbRoles.Text = "Roles"
        '
        'gbPermisos
        '
        Me.gbPermisos.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gbPermisos.Controls.Add(Me.TableLayoutPanel1)
        Me.gbPermisos.Location = New System.Drawing.Point(12, 145)
        Me.gbPermisos.Name = "gbPermisos"
        Me.gbPermisos.Size = New System.Drawing.Size(478, 230)
        Me.gbPermisos.TabIndex = 8
        Me.gbPermisos.TabStop = False
        Me.gbPermisos.Text = "Casos de uso - Métodos de sistema"
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.ColumnCount = 2
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.Controls.Add(Me.clbCasosUso, 0, 0)
        Me.TableLayoutPanel1.Controls.Add(Me.clbMetodosSistema, 1, 0)
        Me.TableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(3, 16)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 1
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 211.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(472, 211)
        Me.TableLayoutPanel1.TabIndex = 0
        '
        'clbCasosUso
        '
        Me.clbCasosUso.BackColor = System.Drawing.SystemColors.Window
        Me.clbCasosUso.Dock = System.Windows.Forms.DockStyle.Fill
        Me.clbCasosUso.FormattingEnabled = True
        Me.clbCasosUso.Location = New System.Drawing.Point(3, 3)
        Me.clbCasosUso.Name = "clbCasosUso"
        Me.clbCasosUso.Size = New System.Drawing.Size(230, 199)
        Me.clbCasosUso.TabIndex = 0
        '
        'clbMetodosSistema
        '
        Me.clbMetodosSistema.Dock = System.Windows.Forms.DockStyle.Fill
        Me.clbMetodosSistema.FormattingEnabled = True
        Me.clbMetodosSistema.Location = New System.Drawing.Point(239, 3)
        Me.clbMetodosSistema.Name = "clbMetodosSistema"
        Me.clbMetodosSistema.Size = New System.Drawing.Size(230, 199)
        Me.clbMetodosSistema.TabIndex = 1
        '
        'gbAgregarRol
        '
        Me.gbAgregarRol.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gbAgregarRol.Controls.Add(Me.lblNombreRol)
        Me.gbAgregarRol.Controls.Add(Me.btnNuevoRol)
        Me.gbAgregarRol.Controls.Add(Me.txtNombreRol)
        Me.gbAgregarRol.Location = New System.Drawing.Point(495, 1)
        Me.gbAgregarRol.Name = "gbAgregarRol"
        Me.gbAgregarRol.Size = New System.Drawing.Size(264, 75)
        Me.gbAgregarRol.TabIndex = 9
        Me.gbAgregarRol.TabStop = False
        Me.gbAgregarRol.Text = "Nuevo rol"
        '
        'lblNombreRol
        '
        Me.lblNombreRol.AutoSize = True
        Me.lblNombreRol.Location = New System.Drawing.Point(6, 20)
        Me.lblNombreRol.Name = "lblNombreRol"
        Me.lblNombreRol.Size = New System.Drawing.Size(44, 13)
        Me.lblNombreRol.TabIndex = 6
        Me.lblNombreRol.Text = "Nombre"
        '
        'btnNuevoRol
        '
        Me.btnNuevoRol.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnNuevoRol.Image = CType(resources.GetObject("btnNuevoRol.Image"), System.Drawing.Image)
        Me.btnNuevoRol.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnNuevoRol.Location = New System.Drawing.Point(185, 43)
        Me.btnNuevoRol.Name = "btnNuevoRol"
        Me.btnNuevoRol.Size = New System.Drawing.Size(73, 24)
        Me.btnNuevoRol.TabIndex = 5
        Me.btnNuevoRol.Text = "Agregar"
        Me.btnNuevoRol.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnNuevoRol.UseVisualStyleBackColor = True
        '
        'txtNombreRol
        '
        Me.txtNombreRol.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtNombreRol.Location = New System.Drawing.Point(53, 17)
        Me.txtNombreRol.Name = "txtNombreRol"
        Me.txtNombreRol.Size = New System.Drawing.Size(205, 20)
        Me.txtNombreRol.TabIndex = 0
        '
        'gbAgregarCasoUso
        '
        Me.gbAgregarCasoUso.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gbAgregarCasoUso.Controls.Add(Me.Label1)
        Me.gbAgregarCasoUso.Controls.Add(Me.btnNuevoCasoUso)
        Me.gbAgregarCasoUso.Controls.Add(Me.txtNombreCasoUso)
        Me.gbAgregarCasoUso.Location = New System.Drawing.Point(495, 145)
        Me.gbAgregarCasoUso.Name = "gbAgregarCasoUso"
        Me.gbAgregarCasoUso.Size = New System.Drawing.Size(264, 76)
        Me.gbAgregarCasoUso.TabIndex = 10
        Me.gbAgregarCasoUso.TabStop = False
        Me.gbAgregarCasoUso.Text = "Nuevo Caso de uso"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(6, 19)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(44, 13)
        Me.Label1.TabIndex = 6
        Me.Label1.Text = "Nombre"
        '
        'btnNuevoCasoUso
        '
        Me.btnNuevoCasoUso.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnNuevoCasoUso.Image = CType(resources.GetObject("btnNuevoCasoUso.Image"), System.Drawing.Image)
        Me.btnNuevoCasoUso.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnNuevoCasoUso.Location = New System.Drawing.Point(185, 45)
        Me.btnNuevoCasoUso.Name = "btnNuevoCasoUso"
        Me.btnNuevoCasoUso.Size = New System.Drawing.Size(73, 24)
        Me.btnNuevoCasoUso.TabIndex = 5
        Me.btnNuevoCasoUso.Text = "Agregar"
        Me.btnNuevoCasoUso.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnNuevoCasoUso.UseVisualStyleBackColor = True
        '
        'txtNombreCasoUso
        '
        Me.txtNombreCasoUso.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtNombreCasoUso.Location = New System.Drawing.Point(53, 19)
        Me.txtNombreCasoUso.Name = "txtNombreCasoUso"
        Me.txtNombreCasoUso.Size = New System.Drawing.Size(205, 20)
        Me.txtNombreCasoUso.TabIndex = 0
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(678, 227)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(75, 23)
        Me.Button1.TabIndex = 11
        Me.Button1.Text = "ToXml"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'frmAdminPermisos
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(771, 417)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.gbAgregarCasoUso)
        Me.Controls.Add(Me.gbAgregarRol)
        Me.Controls.Add(Me.gbPermisos)
        Me.Controls.Add(Me.gbRoles)
        Me.Controls.Add(Me.cmdCancelar)
        Me.Controls.Add(Me.cmdAceptar)
        Me.Name = "frmAdminPermisos"
        Me.Text = "Administración de permisos"
        Me.gbRoles.ResumeLayout(False)
        Me.gbPermisos.ResumeLayout(False)
        Me.TableLayoutPanel1.ResumeLayout(False)
        Me.gbAgregarRol.ResumeLayout(False)
        Me.gbAgregarRol.PerformLayout()
        Me.gbAgregarCasoUso.ResumeLayout(False)
        Me.gbAgregarCasoUso.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents cmdCancelar As System.Windows.Forms.Button
    Friend WithEvents cmdAceptar As System.Windows.Forms.Button
    Friend WithEvents lsbRoles As System.Windows.Forms.ListBox
    Friend WithEvents gbRoles As System.Windows.Forms.GroupBox
    Friend WithEvents gbPermisos As System.Windows.Forms.GroupBox
    Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents clbCasosUso As System.Windows.Forms.CheckedListBox
    Friend WithEvents clbMetodosSistema As System.Windows.Forms.CheckedListBox
    Friend WithEvents gbAgregarRol As System.Windows.Forms.GroupBox
    Friend WithEvents txtNombreRol As System.Windows.Forms.TextBox
    Friend WithEvents lblNombreRol As System.Windows.Forms.Label
    Friend WithEvents btnNuevoRol As System.Windows.Forms.Button
    Friend WithEvents gbAgregarCasoUso As System.Windows.Forms.GroupBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents btnNuevoCasoUso As System.Windows.Forms.Button
    Friend WithEvents txtNombreCasoUso As System.Windows.Forms.TextBox
    Friend WithEvents Button1 As System.Windows.Forms.Button
End Class
