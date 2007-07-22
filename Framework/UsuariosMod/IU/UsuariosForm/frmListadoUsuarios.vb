Public Class frmListadoUsuarios

#Region "Atributos"

    Private mControlador As Framework.Usuarios.IUWin.Controladores.ctrlAdminUsuariosForm

#End Region

#Region "Inicializar"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        mControlador = Me.Controlador

        Try
            Me.ActualizarListadoUsuarios()
        Catch ex As Exception
            Me.MostrarError(ex, Me)
        End Try

    End Sub

#End Region

#Region "Delegados eventos"

    Private Sub cmdCerrar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCerrar.Click
        Try
            Me.Close()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdConsultar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdConsultar.Click
        Try
            Me.ConsultarUsuario()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdModificar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdModificar.Click
        Try
            Me.ModificarUsuario()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdNuevo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdNuevo.Click
        Try
            Me.NuevoUsuario()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

#End Region

#Region "Métodos"

    Private Sub ActualizarListadoUsuarios()
        Dim dts As DataSet
        Dim dt As New DataTable()
        Dim dr As IDataReader

        dts = Me.mControlador.RecuperarListadoUsuarios()

        dr = dts.CreateDataReader()
        dt.Load(dr, LoadOption.OverwriteChanges)

        Me.dgvUsuarios.DataSource = dt
    End Sub

    Private Sub ConsultarUsuario()
        Me.NavegarAdminUsuarios(ModoAdmin.Consultar)
    End Sub

    Private Sub ModificarUsuario()
        Me.NavegarAdminUsuarios(ModoAdmin.Modificar)

        'Se actualiza el listado de usuarios
        Me.ActualizarListadoUsuarios()
    End Sub

    Private Sub NuevoUsuario()
        Me.NavegarAdminUsuarios(ModoAdmin.Nuevo)

        'Se actualiza el listado de usuarios
        Me.ActualizarListadoUsuarios()
    End Sub

    Private Sub NavegarAdminUsuarios(ByVal modo As ModoAdmin)
        Dim miPaquete As New Hashtable()


        If modo = ModoAdmin.Nuevo Then
            miPaquete.Add("ID", "")
        Else
            Dim idUsuario As String = Me.RecuperarIdUsuarioSeleccionado()
            If String.IsNullOrEmpty(idUsuario) Then
                MessageBox.Show("No hay ningún usuario seleccionado")
                Return
            End If
            miPaquete.Add("ID", idUsuario)
        End If

        miPaquete.Add("Modo", modo)
        Me.cMarco.Navegar("AdminUsuarios", Me, Me, MotorIU.Motor.TipoNavegacion.Modal, Me.GenerarDatosCarga(), miPaquete)
    End Sub

    Private Function RecuperarIdUsuarioSeleccionado() As String
        If Me.dgvUsuarios.SelectedRows.Count > 0 Then
            Return Me.dgvUsuarios.SelectedRows.Item(0).Cells.Item(0).Value.ToString()
        End If
        Return Nothing
    End Function
#End Region

End Class

Public Enum ModoAdmin
    Nuevo
    Consultar
    Modificar
End Enum