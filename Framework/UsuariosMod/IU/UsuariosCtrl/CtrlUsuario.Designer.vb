<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class CtrlUsuario
    Inherits MotorIU.ControlesP.BaseControlP

    'UserControl1 overrides dispose to clean up the component list.
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(CtrlUsuario))
        Me.lblLogin = New System.Windows.Forms.Label
        Me.lblPassword = New System.Windows.Forms.Label
        Me.lblUsuario = New System.Windows.Forms.Label
        Me.txtLogin = New System.Windows.Forms.TextBox
        Me.txtPassword = New System.Windows.Forms.TextBox
        Me.txtUsuario = New System.Windows.Forms.TextBox
        Me.clbRoles = New System.Windows.Forms.CheckedListBox
        Me.txtConfirmaPassword = New System.Windows.Forms.TextBox
        Me.Label1 = New System.Windows.Forms.Label
        Me.btnAsignarEditarUsuario = New System.Windows.Forms.Button
        Me.btnRolesLimpiar = New System.Windows.Forms.Button
        Me.btnRolesTodos = New System.Windows.Forms.Button
        Me.btnBorrarUsuario = New System.Windows.Forms.Button
        Me.gbDatosUsuario = New System.Windows.Forms.GroupBox
        Me.btnModificarPassword = New System.Windows.Forms.Button
        Me.chbBaja = New System.Windows.Forms.CheckBox
        Me.gbRoles = New System.Windows.Forms.GroupBox
        Me.gbAdminUsuarios = New System.Windows.Forms.GroupBox
        Me.gbDatosUsuario.SuspendLayout()
        Me.gbRoles.SuspendLayout()
        Me.gbAdminUsuarios.SuspendLayout()
        Me.SuspendLayout()
        '
        'lblLogin
        '
        Me.lblLogin.AutoSize = True
        Me.lblLogin.Location = New System.Drawing.Point(70, 23)
        Me.lblLogin.Name = "lblLogin"
        Me.lblLogin.Size = New System.Drawing.Size(33, 13)
        Me.lblLogin.TabIndex = 0
        Me.lblLogin.Text = "Login"
        '
        'lblPassword
        '
        Me.lblPassword.AutoSize = True
        Me.lblPassword.Location = New System.Drawing.Point(50, 47)
        Me.lblPassword.Name = "lblPassword"
        Me.lblPassword.Size = New System.Drawing.Size(53, 13)
        Me.lblPassword.TabIndex = 1
        Me.lblPassword.Text = "Password"
        '
        'lblUsuario
        '
        Me.lblUsuario.AutoSize = True
        Me.lblUsuario.Location = New System.Drawing.Point(60, 97)
        Me.lblUsuario.Name = "lblUsuario"
        Me.lblUsuario.Size = New System.Drawing.Size(43, 13)
        Me.lblUsuario.TabIndex = 2
        Me.lblUsuario.Text = "Usuario"
        '
        'txtLogin
        '
        Me.txtLogin.Location = New System.Drawing.Point(109, 20)
        Me.txtLogin.Name = "txtLogin"
        Me.txtLogin.Size = New System.Drawing.Size(160, 20)
        Me.txtLogin.TabIndex = 1
        '
        'txtPassword
        '
        Me.txtPassword.Location = New System.Drawing.Point(109, 44)
        Me.txtPassword.Name = "txtPassword"
        Me.txtPassword.PasswordChar = Global.Microsoft.VisualBasic.ChrW(42)
        Me.txtPassword.Size = New System.Drawing.Size(160, 20)
        Me.txtPassword.TabIndex = 2
        '
        'txtUsuario
        '
        Me.txtUsuario.Location = New System.Drawing.Point(109, 94)
        Me.txtUsuario.Name = "txtUsuario"
        Me.txtUsuario.ReadOnly = True
        Me.txtUsuario.Size = New System.Drawing.Size(272, 20)
        Me.txtUsuario.TabIndex = 4
        '
        'clbRoles
        '
        Me.clbRoles.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.clbRoles.FormattingEnabled = True
        Me.clbRoles.Location = New System.Drawing.Point(21, 19)
        Me.clbRoles.Name = "clbRoles"
        Me.clbRoles.Size = New System.Drawing.Size(360, 229)
        Me.clbRoles.TabIndex = 8
        '
        'txtConfirmaPassword
        '
        Me.txtConfirmaPassword.Location = New System.Drawing.Point(109, 68)
        Me.txtConfirmaPassword.Name = "txtConfirmaPassword"
        Me.txtConfirmaPassword.PasswordChar = Global.Microsoft.VisualBasic.ChrW(42)
        Me.txtConfirmaPassword.Size = New System.Drawing.Size(160, 20)
        Me.txtConfirmaPassword.TabIndex = 3
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(3, 71)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(100, 13)
        Me.Label1.TabIndex = 9
        Me.Label1.Text = "Confirmar Password"
        '
        'btnAsignarEditarUsuario
        '
        Me.btnAsignarEditarUsuario.Image = CType(resources.GetObject("btnAsignarEditarUsuario.Image"), System.Drawing.Image)
        Me.btnAsignarEditarUsuario.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnAsignarEditarUsuario.Location = New System.Drawing.Point(387, 92)
        Me.btnAsignarEditarUsuario.Name = "btnAsignarEditarUsuario"
        Me.btnAsignarEditarUsuario.Size = New System.Drawing.Size(67, 24)
        Me.btnAsignarEditarUsuario.TabIndex = 5
        Me.btnAsignarEditarUsuario.Text = "Asignar"
        Me.btnAsignarEditarUsuario.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnAsignarEditarUsuario.UseVisualStyleBackColor = True
        '
        'btnRolesLimpiar
        '
        Me.btnRolesLimpiar.Image = CType(resources.GetObject("btnRolesLimpiar.Image"), System.Drawing.Image)
        Me.btnRolesLimpiar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnRolesLimpiar.Location = New System.Drawing.Point(387, 49)
        Me.btnRolesLimpiar.Name = "btnRolesLimpiar"
        Me.btnRolesLimpiar.Size = New System.Drawing.Size(66, 24)
        Me.btnRolesLimpiar.TabIndex = 10
        Me.btnRolesLimpiar.Text = "Limpiar"
        Me.btnRolesLimpiar.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnRolesLimpiar.UseVisualStyleBackColor = True
        '
        'btnRolesTodos
        '
        Me.btnRolesTodos.ForeColor = System.Drawing.SystemColors.ControlText
        Me.btnRolesTodos.Image = CType(resources.GetObject("btnRolesTodos.Image"), System.Drawing.Image)
        Me.btnRolesTodos.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnRolesTodos.Location = New System.Drawing.Point(387, 19)
        Me.btnRolesTodos.Name = "btnRolesTodos"
        Me.btnRolesTodos.Size = New System.Drawing.Size(66, 24)
        Me.btnRolesTodos.TabIndex = 9
        Me.btnRolesTodos.Text = "Todos"
        Me.btnRolesTodos.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnRolesTodos.UseVisualStyleBackColor = True
        '
        'btnBorrarUsuario
        '
        Me.btnBorrarUsuario.Image = CType(resources.GetObject("btnBorrarUsuario.Image"), System.Drawing.Image)
        Me.btnBorrarUsuario.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnBorrarUsuario.Location = New System.Drawing.Point(460, 92)
        Me.btnBorrarUsuario.Name = "btnBorrarUsuario"
        Me.btnBorrarUsuario.Size = New System.Drawing.Size(67, 24)
        Me.btnBorrarUsuario.TabIndex = 6
        Me.btnBorrarUsuario.Text = "Borrar"
        Me.btnBorrarUsuario.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnBorrarUsuario.UseVisualStyleBackColor = True
        '
        'gbDatosUsuario
        '
        Me.gbDatosUsuario.Controls.Add(Me.btnModificarPassword)
        Me.gbDatosUsuario.Controls.Add(Me.chbBaja)
        Me.gbDatosUsuario.Controls.Add(Me.txtPassword)
        Me.gbDatosUsuario.Controls.Add(Me.btnBorrarUsuario)
        Me.gbDatosUsuario.Controls.Add(Me.lblLogin)
        Me.gbDatosUsuario.Controls.Add(Me.lblPassword)
        Me.gbDatosUsuario.Controls.Add(Me.txtLogin)
        Me.gbDatosUsuario.Controls.Add(Me.btnAsignarEditarUsuario)
        Me.gbDatosUsuario.Controls.Add(Me.txtConfirmaPassword)
        Me.gbDatosUsuario.Controls.Add(Me.Label1)
        Me.gbDatosUsuario.Controls.Add(Me.txtUsuario)
        Me.gbDatosUsuario.Controls.Add(Me.lblUsuario)
        Me.gbDatosUsuario.Location = New System.Drawing.Point(4, 15)
        Me.gbDatosUsuario.Name = "gbDatosUsuario"
        Me.gbDatosUsuario.Size = New System.Drawing.Size(534, 148)
        Me.gbDatosUsuario.TabIndex = 14
        Me.gbDatosUsuario.TabStop = False
        Me.gbDatosUsuario.Text = "Datos del usuario"
        '
        'btnModificarPassword
        '
        Me.btnModificarPassword.Image = Global.Framework.Usuarios.IUWin.Controles.My.Resources.Resources.user1_lock
        Me.btnModificarPassword.Location = New System.Drawing.Point(275, 18)
        Me.btnModificarPassword.Name = "btnModificarPassword"
        Me.btnModificarPassword.Size = New System.Drawing.Size(29, 24)
        Me.btnModificarPassword.TabIndex = 10
        Me.btnModificarPassword.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnModificarPassword.UseVisualStyleBackColor = True
        '
        'chbBaja
        '
        Me.chbBaja.AutoCheck = False
        Me.chbBaja.AutoSize = True
        Me.chbBaja.Enabled = False
        Me.chbBaja.Location = New System.Drawing.Point(109, 121)
        Me.chbBaja.Name = "chbBaja"
        Me.chbBaja.Size = New System.Drawing.Size(47, 17)
        Me.chbBaja.TabIndex = 7
        Me.chbBaja.Text = "Baja"
        Me.chbBaja.UseVisualStyleBackColor = True
        '
        'gbRoles
        '
        Me.gbRoles.Controls.Add(Me.clbRoles)
        Me.gbRoles.Controls.Add(Me.btnRolesTodos)
        Me.gbRoles.Controls.Add(Me.btnRolesLimpiar)
        Me.gbRoles.Location = New System.Drawing.Point(4, 169)
        Me.gbRoles.Name = "gbRoles"
        Me.gbRoles.Size = New System.Drawing.Size(534, 263)
        Me.gbRoles.TabIndex = 15
        Me.gbRoles.TabStop = False
        Me.gbRoles.Text = "Roles"
        '
        'gbAdminUsuarios
        '
        Me.gbAdminUsuarios.Controls.Add(Me.gbDatosUsuario)
        Me.gbAdminUsuarios.Controls.Add(Me.gbRoles)
        Me.gbAdminUsuarios.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gbAdminUsuarios.Location = New System.Drawing.Point(0, 0)
        Me.gbAdminUsuarios.Name = "gbAdminUsuarios"
        Me.gbAdminUsuarios.Size = New System.Drawing.Size(544, 437)
        Me.gbAdminUsuarios.TabIndex = 16
        Me.gbAdminUsuarios.TabStop = False
        Me.gbAdminUsuarios.Text = "Usuario"
        '
        'CtrlUsuario
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.gbAdminUsuarios)
        Me.Name = "CtrlUsuario"
        Me.Size = New System.Drawing.Size(544, 437)
        Me.gbDatosUsuario.ResumeLayout(False)
        Me.gbDatosUsuario.PerformLayout()
        Me.gbRoles.ResumeLayout(False)
        Me.gbAdminUsuarios.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents lblLogin As System.Windows.Forms.Label
    Friend WithEvents lblPassword As System.Windows.Forms.Label
    Friend WithEvents lblUsuario As System.Windows.Forms.Label
    Friend WithEvents txtLogin As System.Windows.Forms.TextBox
    Friend WithEvents txtPassword As System.Windows.Forms.TextBox
    Friend WithEvents txtUsuario As System.Windows.Forms.TextBox
    Friend WithEvents clbRoles As System.Windows.Forms.CheckedListBox
    Friend WithEvents txtConfirmaPassword As System.Windows.Forms.TextBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents btnAsignarEditarUsuario As System.Windows.Forms.Button
    Friend WithEvents btnRolesLimpiar As System.Windows.Forms.Button
    Friend WithEvents btnRolesTodos As System.Windows.Forms.Button
    Friend WithEvents btnBorrarUsuario As System.Windows.Forms.Button
    Friend WithEvents gbDatosUsuario As System.Windows.Forms.GroupBox
    Friend WithEvents gbRoles As System.Windows.Forms.GroupBox
    Friend WithEvents gbAdminUsuarios As System.Windows.Forms.GroupBox
    Friend WithEvents chbBaja As System.Windows.Forms.CheckBox
    Friend WithEvents btnModificarPassword As System.Windows.Forms.Button

End Class
