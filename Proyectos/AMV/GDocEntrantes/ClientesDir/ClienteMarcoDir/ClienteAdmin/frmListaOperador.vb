Public Class frmListaOperador

#Region "Atributos"

    Private mControlador As frmOperadorControlador
    Private mTablaOperadores As DataTable
    Private mColOperadores As AmvDocumentosDN.ColOperadorDN

#End Region

#Region "Inicializar"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        mControlador = Me.Controlador


        mTablaOperadores = dgvOperadores.DataSource()

        Me.dgvOperadores.DataSource = mTablaOperadores

        ActualizarListado()

    End Sub

#End Region

#Region "Delegados eventos"

    Private Sub cmdConsultar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdConsultar.Click
        Try
            Consultar()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdModificar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdModificar.Click
        Try
            Modificar()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdNuevo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdNuevo.Click
        Try
            Nuevo()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAceptar.Click
        Try
            Aceptar()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdCerrar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCerrar.Click
        Try
            Close()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

#End Region

#Region "Métodos"

    Private Sub ActualizarListado()
        'TODO: Cambiar métodos RecuperarListaOperadores de IList a colOperadores
        mColOperadores = New AmvDocumentosDN.ColOperadorDN()
        mColOperadores.AddRange(Me.mControlador.RecuperarListaOperador())

        dgvOperadores.Rows.Clear()

        For Each miOperador As AmvDocumentosDN.OperadorDN In mColOperadores
            dgvOperadores.Rows.Add()
            Dim indiceColumna As Integer
            indiceColumna = dgvOperadores.Rows.GetLastRow(DataGridViewElementStates.Visible)
            dgvOperadores.Rows.Item(indiceColumna).Cells("ID").Value = miOperador.ID
            dgvOperadores.Rows.Item(indiceColumna).Cells("Nombre").Value = miOperador.Nombre
            dgvOperadores.Rows.Item(indiceColumna).Cells("FechaAlta").Value = miOperador.FI.ToShortDateString()
            dgvOperadores.Rows.Item(indiceColumna).Cells("FechaBaja").Value = miOperador.FF.ToShortDateString()
            dgvOperadores.Rows.Item(indiceColumna).Cells("Baja").Value = miOperador.Baja

        Next

        dgvOperadores.Refresh()
    End Sub

    Private Sub Consultar()
        Me.NavegarAdmin(ModoAdmin.Consultar)
    End Sub

    Private Sub Modificar()
        Me.NavegarAdmin(ModoAdmin.Modificar)

        'Se actualiza el listado de usuarios
        Me.ActualizarListado()
    End Sub

    Private Sub Nuevo()
        Me.NavegarAdmin(ModoAdmin.Nuevo)

        'Se actualiza el listado de usuarios
        Me.ActualizarListado()
    End Sub

    Private Sub NavegarAdmin(ByVal modo As ModoAdmin)
        Dim miPaquete As New Hashtable()

        If modo = ModoAdmin.Nuevo Then
            miPaquete.Add("ID", "")
        Else
            Dim idUsuario As String = Me.RecuperarIdSeleccionado()
            If String.IsNullOrEmpty(idUsuario) Then
                MessageBox.Show("No hay ningún usuario seleccionado", "Usuarios", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Return
            End If
            miPaquete.Add("ID", idUsuario)
        End If
        miPaquete.Add("Modo", modo)
        Me.cMarco.Navegar("EntidadUsuario", Me, Me, MotorIU.Motor.TipoNavegacion.Modal, Me.GenerarDatosCarga(), miPaquete)
    End Sub

    Private Function RecuperarIdSeleccionado() As String
        If Me.dgvOperadores.SelectedRows.Count > 0 Then
            Return Me.dgvOperadores.SelectedRows.Item(0).Cells.Item(0).Value.ToString()
        End If
        Return Nothing
    End Function

    Private Sub Aceptar()
        Dim miHuellaOp As AmvDocumentosDN.HuellaOperadorDN
        Dim idOp As String

        idOp = RecuperarIdSeleccionado()

        If Not String.IsNullOrEmpty(idOp) Then
            miHuellaOp = New AmvDocumentosDN.HuellaOperadorDN(mColOperadores.RecuperarxID(idOp))

            If Me.Paquete.Contains("HuellaEntidadUser") Then
                Me.Paquete.Item("HuellaEntidadUser") = miHuellaOp
            Else
                Me.Paquete.Add("HuellaEntidadUser", miHuellaOp)
            End If

        End If

        Close()
    End Sub

#End Region

End Class

Public Enum ModoAdmin
    Nuevo
    Consultar
    Modificar
End Enum