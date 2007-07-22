#Region "Importaciones"

Imports Framework.Usuarios.DN

#End Region

Public Class CtrlUsuario

#Region "Inicializador"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = New Framework.Usuarios.IUWin.Controladores.ctrlUsuarioControlador(Me.Marco, Me)

        Try
            'Lista de los roles existentes
            Me.ActualizarListaRolesTotales()

            'Se actualizan los botones de asignar y borrar al usuario
            ActualizarBotonesHuellaUser()

        Catch ex As Exception
            Me.MostrarError(ex)
        End Try

    End Sub

#End Region

#Region "Atributos"

    Private mPrincipal As PrincipalDN
    Private mDatosIdentidad As DatosIdentidadDN
    Private mColRoles As ColRolDN
    Private mUsuario As UsuarioDN
    Private mHuellaUser As Framework.DatosNegocio.IHuellaEntidadDN

    Private mModoConsulta As Boolean
    Private mControlador As Framework.Usuarios.IUWin.Controladores.ctrlUsuarioControlador

    Private mModificarDI As Boolean

#End Region

#Region "Propiedades"

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property Principal() As PrincipalDN
        Get
            If IUaDN() Then
                Return Me.mPrincipal
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As PrincipalDN)
            Me.mPrincipal = value
            Me.DNaIU(value)
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public ReadOnly Property DatosIdentidad() As DatosIdentidadDN
        Get
            Return Me.mDatosIdentidad
        End Get
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public WriteOnly Property ModoConsulta() As Boolean
        Set(ByVal value As Boolean)
            mModoConsulta = value
            EstablecerModoEdicion()
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public ReadOnly Property ModificarDI() As Boolean
        Get
            Return mModificarDI
        End Get
    End Property

#End Region

#Region "Delegados eventos"

    Private Sub cmdAsignarEditarUsuario_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAsignarEditarUsuario.Click
        Try
            Me.AsignarEditarUsuario()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdBorrarUsuario_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBorrarUsuario.Click
        Try
            Me.BorrarUsuario()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdRolesTodos_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnRolesTodos.Click
        Try
            Me.SeleccionarTodosRoles()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdRolesLimpiar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnRolesLimpiar.Click
        Try
            Me.LimpiarRolesSeleccionados()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub btnModificarPassword_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnModificarPassword.Click
        Try
            ModificarEstadoPassword(True)
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

#End Region

#Region "Establecer y rellenar datos"

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Dim miPrincipal As PrincipalDN
        Dim miHuellaUser As Framework.DatosNegocio.IHuellaEntidadDN

        miPrincipal = pDN

        ActualizarBotonesHuellaUser()

        If miPrincipal Is Nothing Then
            Me.mPrincipal = Nothing
            Me.mColRoles = New ColRolDN()
            mHuellaUser = Nothing

            Me.txtLogin.Text = ""
            Me.txtUsuario.Text = ""

            ModificarEstadoPassword(True)

            Me.clbRoles.ClearSelected()

        Else
            Me.txtLogin.Text = miPrincipal.UsuarioDN.Nombre

            'Si se quisiera mostrar la contraseña, hay que recuperar el objeto datos identidad correspondiente
            ModificarEstadoPassword(False)

            Me.chbBaja.Checked = miPrincipal.Baja

            Me.mUsuario = miPrincipal.UsuarioDN
            miHuellaUser = Me.mUsuario.HuellaEntidadUserDN

            If Not miHuellaUser Is Nothing Then
                Me.txtUsuario.Text = miHuellaUser.ToStringEntidadReferida()
            End If

            'Seleccionamos lo roles que tenga asignados el principal
            Me.mColRoles = New ColRolDN()

            If miPrincipal.ColRoles IsNot Nothing AndAlso miPrincipal.ColRoles.Count > 0 Then
                Me.mColRoles.AddRange(miPrincipal.ColRoles)

                ActualizarEstadoRoles()
            End If

        End If

    End Sub

    Protected Overrides Function IUaDN() As Boolean
        If Me.ErroresValidadores.Count > 0 Then
            Return False
        End If

        'Se comprueba el password
        If mModificarDI Then
            If txtPassword.Text <> txtConfirmaPassword.Text Then
                Return False
            End If

            'Creamos el objeto datos identidad con el nick y la clave
            Me.mDatosIdentidad = New DatosIdentidadDN(Me.txtLogin.Text, Me.txtPassword.Text)
        End If

        

        Dim miRol As RolDN
        Me.mColRoles.Clear()
        For Each miRol In Me.clbRoles.CheckedItems
            Me.mColRoles.Add(miRol)
        Next

        If mPrincipal Is Nothing Then
            ' Me.mUsuario = New UsuarioDN(Me.txtLogin.Text, True, Me.mHuellaUser) ' TODO:alex modificación rara
            Me.mUsuario = New UsuarioDN(Me.txtLogin.Text, True)
            Me.mPrincipal = New PrincipalDN(Me.txtLogin.Text, mUsuario, Me.mColRoles)
        Else
            Me.mPrincipal.UsuarioDN.Nombre = Me.txtLogin.Text
            Me.mPrincipal.ColRolesPrincipal = Me.mColRoles
        End If

        Return True

    End Function
#End Region

#Region "Métodos"

    Private Sub ActualizarListaRolesTotales()
        Dim colRolesTotales As ColRolDN
        colRolesTotales = Me.mControlador.RecuperarColRoles()

        Me.clbRoles.Items.Clear()

        If colRolesTotales IsNot Nothing AndAlso colRolesTotales.Count > 0 Then
            Me.clbRoles.Items.AddRange(colRolesTotales.ToArray())
        End If
    End Sub

    Private Sub SeleccionarTodosRoles()
        For i As Integer = 0 To Me.clbRoles.Items.Count - 1
            Me.clbRoles.SetItemCheckState(i, CheckState.Checked)
        Next
    End Sub

    Private Sub LimpiarRolesSeleccionados()
        Do While (Me.clbRoles.CheckedIndices.Count > 0)
            Me.clbRoles.SetItemCheckState(Me.clbRoles.CheckedIndices(0), CheckState.Unchecked)
        Loop
    End Sub

    Private Sub AsignarEditarUsuario()
        'TODO: Revisar
        'Dim miPaquete As New Hashtable()

        'If mPrincipal IsNot Nothing AndAlso mPrincipal.UsuarioDN.HuellaEntidadUserDN IsNot Nothing Then
        '    miPaquete.Add("Modo", ModoAdmin.Modificar)

        '    Dim idEntidadUser As String = mPrincipal.UsuarioDN.HuellaEntidadUserDN.IdEntidadReferida

        '    miPaquete.Add("ID", idEntidadUser)

        '    Me.Marco.Navegar("EntidadUsuario", Me.ParentForm, Nothing, MotorIU.Motor.TipoNavegacion.Modal, Me.mControlador.ControladorForm.FormularioContenedor.GenerarDatosCarga(), miPaquete)
        'Else
        '    Dim miParametroCargaEst As New ParametroCargaEstructuraDN()

        '    miParametroCargaEst.NombreVistaSel = My.Settings.VistaListaEntidadUserSel
        '    miParametroCargaEst.NombreVistaVis = My.Settings.VistaListaEntidadUserVis
        '    miParametroCargaEst.DestinoNavegacion = My.Settings.DestinoNavegacion

        '    miPaquete.Add("ParametroCargaEstructura", miParametroCargaEst)

        '    miPaquete.Add("MultiSelect", False)
        '    miPaquete.Add("TipoNavegacion", MotorIU.Motor.TipoNavegacion.Modal)
        '    miPaquete.Add("Agregable", True)
        '    miPaquete.Add("EnviarDatatableAlNavegar", False)
        '    miPaquete.Add("Navegable", True)

        '    miPaquete.Add("AlternatingBackcolorFiltro", My.Settings.ColorAlterFiltro)
        '    miPaquete.Add("AlternatingBackcolorResultados", My.Settings.ColorAlterResultados)

        '    miPaquete.Add("Titulo", My.Settings.TituloDestinoNavegacion)


        '    Me.Marco.Navegar(My.Settings.FormularioNavegacion, Me.ParentForm, Nothing, MotorIU.Motor.TipoNavegacion.Modal, Me.mControlador.ControladorForm.FormularioContenedor.GenerarDatosCarga(), miPaquete)
        'End If

        'If miPaquete IsNot Nothing AndAlso miPaquete.Contains("HuellaEntidadUser") AndAlso miPaquete("HuellaEntidadUser") IsNot Nothing Then
        '    If TypeOf miPaquete("HuellaEntidadUser") Is Framework.DatosNegocio.IHuellaEntidadDN Then
        '        Me.mHuellaUser = miPaquete("HuellaEntidadUser")
        '        txtUsuario.Text = Me.mHuellaUser.ToStringEntidadReferida()
        '    Else
        '        Throw New ApplicationException("El tipo de la entidad asignada no es válido")
        '    End If
        'End If

    End Sub

    Private Sub BorrarUsuario()
        'Solo se permite si el principal es nothing
        If Me.mPrincipal IsNot Nothing Then
            MessageBox.Show("No puede cambiar la entidad asignada a este usuario", "Administración de usuario", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        Else
            Me.mHuellaUser = Nothing
            Me.txtUsuario.Text = ""
        End If
    End Sub

    Private Sub ActualizarEstadoRoles()

        For Each miRol As RolDN In Me.mColRoles
            For i As Integer = 0 To Me.clbRoles.Items.Count - 1
                If miRol.EsIgualRapido(Me.clbRoles.Items(i)) Then
                    Me.clbRoles.SetItemCheckState(i, CheckState.Checked)
                End If
            Next
        Next

    End Sub

    Private Sub EstablecerModoEdicion()
        If mModoConsulta Then
            txtLogin.ReadOnly = True
            txtPassword.ReadOnly = True
            txtConfirmaPassword.Enabled = False
            btnRolesLimpiar.Enabled = False
            btnRolesTodos.Enabled = False
            clbRoles.Enabled = False
            btnAsignarEditarUsuario.Text = "Asignar"
            btnAsignarEditarUsuario.Enabled = False
            btnModificarPassword.Enabled = False
        End If
    End Sub

    Private Sub ActualizarBotonesHuellaUser()
        If mPrincipal Is Nothing Then
            btnAsignarEditarUsuario.Enabled = True
            btnBorrarUsuario.Enabled = True
        Else
            If Not mModoConsulta AndAlso mPrincipal.UsuarioDN.HuellaEntidadUserDN IsNot Nothing Then
                btnAsignarEditarUsuario.Text = "Editar"
                btnAsignarEditarUsuario.Enabled = True
            Else
                btnAsignarEditarUsuario.Text = "Asignar"
                btnAsignarEditarUsuario.Enabled = False
            End If

            btnBorrarUsuario.Enabled = False
        End If
    End Sub

    Private Sub ModificarEstadoPassword(ByVal activado As Boolean)
        If activado Then
            btnModificarPassword.Enabled = False
            mModificarDI = True
            txtLogin.ReadOnly = False
            txtPassword.Text = ""
            txtPassword.ReadOnly = False
            txtConfirmaPassword.Text = ""
            txtConfirmaPassword.ReadOnly = False
        Else
            btnModificarPassword.Enabled = True
            mModificarDI = False
            txtLogin.ReadOnly = True
            txtPassword.Text = "          "
            txtPassword.ReadOnly = True
            txtConfirmaPassword.Text = "          "
            txtConfirmaPassword.ReadOnly = True
        End If
    End Sub

#End Region


End Class

Public Enum ModoAdmin
    Nuevo
    Consultar
    Modificar
End Enum