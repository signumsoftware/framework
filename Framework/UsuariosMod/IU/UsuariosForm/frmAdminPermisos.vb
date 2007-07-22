Imports Framework.Usuarios.DN

Public Class frmAdminPermisos

#Region "Atributos"

    Private mControlador As Framework.Usuarios.IUWin.Controladores.ctrlAdminPermisosForm

    Private mColRoles As ColRolDN
    Private mColCasosUso As ColCasosUsoDN
    Private mColMetodosSistema As ColMetodosSistemaDN

    Private mActualizacionCU As Boolean
    Private mActualizacionMS As Boolean

#End Region

#Region "Inicializar"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        mControlador = Me.Controlador

        RecuperarColecciones()
        ActualizarListados()
    End Sub

#End Region

#Region "Delegados de Eventos"

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAceptar.Click
        Try
            Guardar()
            Close()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Try
            Close()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub lsbRoles_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles lsbRoles.SelectedIndexChanged
        Try
            ActualizarSelRol()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub clbCasosUso_ItemCheck(ByVal sender As Object, ByVal e As System.Windows.Forms.ItemCheckEventArgs) Handles clbCasosUso.ItemCheck
        Try
            If Not mActualizacionCU Then
                ModificarRol()
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub clbCasosUso_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles clbCasosUso.SelectedIndexChanged
        Try
            ActualizarSelCasoUso()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub clbMetodosSistema_ItemCheck(ByVal sender As Object, ByVal e As System.Windows.Forms.ItemCheckEventArgs) Handles clbMetodosSistema.ItemCheck
        Try
            If Not mActualizacionMS Then
                ModificarCasoUso()
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub btnNuevoRol_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNuevoRol.Click
        Try
            CrearRol()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub btnNuevoCasoUso_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNuevoCasoUso.Click
        Try
            CrearCasoUso()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Clipboard.Clear()
        Clipboard.SetText(mColRoles.ToXml)
    End Sub

#End Region

#Region "Métodos"

    Private Sub RecuperarColecciones()
        mColRoles = mControlador.RecuperarColRol()

        Dim miListaCU As IList(Of CasosUsoDN) = mControlador.RecuperarListaCasosUso()
        mColCasosUso = New ColCasosUsoDN()
        For Each miCasoUso As CasosUsoDN In miListaCU
            mColCasosUso.Add(miCasoUso)
        Next

        Dim miListaMS As IList(Of MetodoSistemaDN) = mControlador.RecuperarMetodos()
        mColMetodosSistema = New ColMetodosSistemaDN()
        For Each miMetodo As MetodoSistemaDN In miListaMS
            mColMetodosSistema.Add(miMetodo)
        Next

    End Sub

    Private Sub ActualizarListados()
        lsbRoles.Items.Clear()
        For Each miRol As RolDN In mColRoles
            lsbRoles.Items.Add(miRol)
        Next
        'lsbRoles.Items.AddRange(mColRoles.ToArray())

        clbCasosUso.Items.Clear()
        For Each miCasoUso As CasosUsoDN In mColCasosUso
            clbCasosUso.Items.Add(miCasoUso)
        Next
        'clbCasosUso.Items.AddRange(mColCasosUso.ToArray())

        clbMetodosSistema.Items.Clear()
        'clbMetodosSistema.Items.AddRange(mColMetodosSistema.ToArray())
        For Each miMetodo As MetodoSistemaDN In mColMetodosSistema
            clbMetodosSistema.Items.Add(miMetodo)
        Next
        clbMetodosSistema.Sorted = True

    End Sub

    Private Sub Guardar()
        Dim miColCasosUsoG As New ColCasosUsoDN()
        For Each casoUso As CasosUsoDN In clbCasosUso.Items
            miColCasosUsoG.Add(casoUso)
        Next

        'Se guardan los roles
        For Each miRol As RolDN In lsbRoles.Items
            For i As Integer = 0 To miRol.ColCasosUsoDN.Count - 1
                Dim miCasoUso As CasosUsoDN
                miCasoUso = RecuperarCasoUsoxGUID(miRol.ColCasosUsoDN(i).GUID)
                miRol.ColCasosUsoDN(i) = miCasoUso
                miColCasosUsoG.EliminarEntidadDN(miCasoUso, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
            Next
            mControlador.GuardarRol(miRol)
        Next

        'Se guardan los casos de uso que todavía no hayan sido almacenados
        For Each casoUso As CasosUsoDN In miColCasosUsoG
            mControlador.GuardarCasoUso(casoUso)
        Next

    End Sub

    Private Sub DeschequearCasosUso()
        Do While clbCasosUso.CheckedIndices.Count > 0
            clbCasosUso.SetItemCheckState(clbCasosUso.CheckedIndices(0), CheckState.Unchecked)
        Loop
    End Sub

    Private Sub DeschequearMetodos()
        Do While clbMetodosSistema.CheckedIndices.Count > 0
            clbMetodosSistema.SetItemCheckState(clbMetodosSistema.CheckedIndices(0), CheckState.Unchecked)
        Loop
    End Sub

    Private Sub ActualizarSelRol()
        mActualizacionCU = True

        DeschequearCasosUso()
        Dim miRolSeleccionado As RolDN = lsbRoles.SelectedItem

        For i As Integer = 0 To clbCasosUso.Items.Count - 1
            If miRolSeleccionado.ColCasosUsoDN.Contiene(clbCasosUso.Items(i), Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
                clbCasosUso.SetItemCheckState(i, CheckState.Checked)
            End If
        Next

        mActualizacionCU = False
    End Sub

    Private Sub ActualizarSelCasoUso()
        mActualizacionMS = True
        DeschequearMetodos()
        Dim miCasoUsoSel As CasosUsoDN = clbCasosUso.SelectedItem

        For i As Integer = 0 To clbMetodosSistema.Items.Count - 1
            If miCasoUsoSel.ColMetodosSistemaDN.Contiene(clbMetodosSistema.Items(i), Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
                clbMetodosSistema.SetItemCheckState(i, CheckState.Checked)
            End If
        Next

        mActualizacionMS = False
    End Sub

    Private Sub ModificarRol()
        Dim miRol As RolDN = lsbRoles.SelectedItem
        Dim miIndiceCU As Integer = -1

        If clbCasosUso.SelectedIndices.Count = 1 Then
            miIndiceCU = clbCasosUso.SelectedIndices.Item(0)
        End If

        If miRol IsNot Nothing AndAlso miIndiceCU >= 0 Then
            If Not clbCasosUso.CheckedIndices.Contains(miIndiceCU) Then
                miRol.ColCasosUsoDN.AddUnico(clbCasosUso.Items.Item(miIndiceCU))
            Else
                miRol.ColCasosUsoDN.EliminarEntidadDN(clbCasosUso.Items.Item(miIndiceCU), Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
            End If

        End If

    End Sub

    Private Sub ModificarCasoUso()
        Dim miCasoUso As CasosUsoDN = clbCasosUso.SelectedItem
        Dim miIndiceMS As Integer = -1

        If clbMetodosSistema.SelectedIndices.Count = 1 Then
            miIndiceMS = clbMetodosSistema.SelectedIndices.Item(0)
        End If

        If miCasoUso IsNot Nothing AndAlso miIndiceMS >= 0 Then
            If Not clbMetodosSistema.CheckedIndices.Contains(miIndiceMS) Then
                miCasoUso.ColMetodosSistemaDN.AddUnico(clbMetodosSistema.Items.Item(miIndiceMS))
            Else
                miCasoUso.ColMetodosSistemaDN.EliminarEntidadDN(clbMetodosSistema.Items.Item(miIndiceMS), Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
            End If
        End If
    End Sub

    Private Sub CrearRol()
        If String.IsNullOrEmpty(txtNombreRol.Text) Then
            MessageBox.Show("El nombre del rol no puede ser nulo")
        Else
            lsbRoles.Items.Add(New RolDN(txtNombreRol.Text, New ColCasosUsoDN()))
            txtNombreRol.Text = ""
        End If
    End Sub

    Private Sub CrearCasoUso()
        If String.IsNullOrEmpty(txtNombreCasoUso.Text) Then
            MessageBox.Show("El nombre del caso de uso no puede ser nulo")
        Else
            clbCasosUso.Items.Add(New CasosUsoDN(txtNombreCasoUso.Text, New ColMetodosSistemaDN()))
            txtNombreCasoUso.Text = ""
        End If
    End Sub

    Private Function RecuperarCasoUsoxGUID(ByVal pGUID As String) As CasosUsoDN
        For Each casoUso As CasosUsoDN In clbCasosUso.Items
            If casoUso.GUID = pGUID Then
                Return casoUso
            End If
        Next
        Return Nothing
    End Function

#End Region


End Class