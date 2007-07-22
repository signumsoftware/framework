
Imports Framework.Ficheros.FicherosDN
Public Class RutaAlmacenamientoFrm

#Region "Atributos"

    Private mControlador As ctrlRutaAlmacenamientoFrm
    Private mColRutaAlmacenamiento As ColRutaAlmacenamientoFicherosDN
    Private mIndiceFilaSeleccionada As Integer

#End Region

#Region "Inicializar"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        mControlador = Me.Controlador

        Try
            RecuperarColRutasAlmacenamiento()
            ActualizarListadoRutas()
            InicializarEstadoControl()
        Catch ex As Exception
            Me.MostrarError(ex, Me)
            Close()
        End Try

    End Sub

#End Region

#Region "Delegados eventos"

    Private Sub cmdAbrir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAbrir.Click
        Try
            SeleccionarRutaListado()
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

    Private Sub cmdGuardar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdGuardar.Click
        Try
            Guardar()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdNuevo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdNuevo.Click
        Try
            CrearRuta()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub dgvRutas_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles dgvRutas.DoubleClick
        Try
            SeleccionarRutaListado()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub btnDisponible_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDisponible.Click
        Try
            EstadoInicialRuta()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub btnAbierta_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAbierta.Click
        Try
            AbrirRuta()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub btnCerrada_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCerrada.Click
        Try
            CerrarRuta()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

#End Region

#Region "Métodos"

    Private Sub InicializarEstadoControl()
        CtrlRutaAlmacenamiento1.SoloLectura = True
        btnAbierta.Enabled = False
        btnCerrada.Enabled = False
        btnDisponible.Enabled = False
    End Sub

    Private Sub CrearRuta()
        RecuperarRutaControl()

        'Se crea una nueva ruta y se añade
        CtrlRutaAlmacenamiento1.RutaAlmacenamiento = Nothing
        mColRutaAlmacenamiento.Add(CtrlRutaAlmacenamiento1.RutaAlmacenamiento)

        ActualizarListadoRutas()

        'se selecciona en el dgv
        mIndiceFilaSeleccionada = dgvRutas.Rows.GetLastRow(DataGridViewElementStates.Visible)
        dgvRutas.Rows.Item(mIndiceFilaSeleccionada).Selected = True
    End Sub

    Private Sub Guardar()
        RecuperarRutaControl()

        For Each miRuta As RutaAlmacenamientoFicherosDN In mColRutaAlmacenamiento
            If miRuta.EstadoRAF <> RutaAlmacenamientoFicherosEstado.Cerrada Then
                miRuta = mControlador.GuardarRutaAlmacenamientoF(miRuta)
            End If
        Next

        ActualizarListadoRutas()
    End Sub

    Private Sub ActualizarListadoRutas()
        dgvRutas.Rows.Clear()

        For Each miRuta As RutaAlmacenamientoFicherosDN In mColRutaAlmacenamiento
            dgvRutas.Rows.Add()
            RellenarFilaDataGridView(miRuta, dgvRutas.Rows.GetLastRow(DataGridViewElementStates.Visible))
        Next

        dgvRutas.Refresh()

    End Sub

    Private Sub RecuperarColRutasAlmacenamiento()
        mColRutaAlmacenamiento = New ColRutaAlmacenamientoFicherosDN()

        Dim lista As IList(Of RutaAlmacenamientoFicherosDN) = mControlador.RecuperarListadoRutas()
        If lista IsNot Nothing AndAlso lista.Count > 0 Then
            mColRutaAlmacenamiento.AddRange(lista)
        End If
    End Sub

    Private Sub SeleccionarRutaListado()
        Dim miRutaAlmacenamiento As RutaAlmacenamientoFicherosDN

        RecuperarRutaControl()

        If dgvRutas.SelectedRows.Count = 1 Then
            miRutaAlmacenamiento = RecuperarRutaxGUID(dgvRutas.SelectedRows(0).Cells("GUID").Value)
            CtrlRutaAlmacenamiento1.RutaAlmacenamiento = miRutaAlmacenamiento
            mIndiceFilaSeleccionada = dgvRutas.SelectedRows(0).Index
            ActualizarBotonesEstado(miRutaAlmacenamiento.EstadoRAF)
        End If

    End Sub

    Private Function RecuperarRutaxGUID(ByVal rutaGUID As String) As RutaAlmacenamientoFicherosDN
        Return mColRutaAlmacenamiento.RecuperarXGUID(rutaGUID)
    End Function

    Private Sub RecuperarRutaControl()
        If Not CtrlRutaAlmacenamiento1.SoloLectura Then
            Dim miRutaAlmacenamiento As RutaAlmacenamientoFicherosDN
            miRutaAlmacenamiento = CtrlRutaAlmacenamiento1.RutaAlmacenamiento
            RellenarFilaDataGridView(miRutaAlmacenamiento, mIndiceFilaSeleccionada)
        End If

    End Sub

    Private Sub RellenarFilaDataGridView(ByVal rutaAlmacenamiento As RutaAlmacenamientoFicherosDN, ByVal indiceFila As Integer)
        dgvRutas.Rows.Item(indiceFila).Cells("ID").Value = rutaAlmacenamiento.ID
        dgvRutas.Rows.Item(indiceFila).Cells("GUID").Value = rutaAlmacenamiento.GUID
        dgvRutas.Rows.Item(indiceFila).Cells("Nombre").Value = rutaAlmacenamiento.Nombre
        dgvRutas.Rows.Item(indiceFila).Cells("Ruta").Value = rutaAlmacenamiento.RutaCarpeta
        dgvRutas.Rows.Item(indiceFila).Cells("Estado").Value = rutaAlmacenamiento.EstadoRAF.ToString()
        dgvRutas.Rows.Item(indiceFila).Cells("FechaCreacion").Value = rutaAlmacenamiento.FI.ToString()
    End Sub

    Private Sub ActualizarBotonesEstado(ByVal rutaAlmacenamientoEstado As RutaAlmacenamientoFicherosEstado)
        btnAbierta.Enabled = (rutaAlmacenamientoEstado = RutaAlmacenamientoFicherosEstado.Disponible)
        btnCerrada.Enabled = Not (rutaAlmacenamientoEstado = RutaAlmacenamientoFicherosEstado.Cerrada)
        btnDisponible.Enabled = Not (rutaAlmacenamientoEstado = RutaAlmacenamientoFicherosEstado.Disponible)
    End Sub

    Private Sub AbrirRuta()
        If MessageBox.Show("Las rutas en estado abierto pasarán a estado Creada. ¿Desea continuar?", "Cambiar estado de la ruta", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) = Windows.Forms.DialogResult.OK Then
            For Each rutaAlmacenamiento As RutaAlmacenamientoFicherosDN In mColRutaAlmacenamiento
                If rutaAlmacenamiento.EstadoRAF = RutaAlmacenamientoFicherosEstado.Abierta Then
                    rutaAlmacenamiento.EstadoRAF = RutaAlmacenamientoFicherosEstado.Disponible
                End If
            Next
            CtrlRutaAlmacenamiento1.RutaAlmacenamientoEstado = RutaAlmacenamientoFicherosEstado.Abierta
            RecuperarRutaControl()
            ActualizarListadoRutas()
            dgvRutas.Rows.Item(mIndiceFilaSeleccionada).Selected = True
            ActualizarBotonesEstado(RutaAlmacenamientoFicherosEstado.Abierta)
        End If
    End Sub

    Private Sub CerrarRuta()
        Dim miRutaAlmacenamiento As RutaAlmacenamientoFicherosDN
        miRutaAlmacenamiento = CtrlRutaAlmacenamiento1.RutaAlmacenamiento
        miRutaAlmacenamiento = mControlador.CerrarRaf(miRutaAlmacenamiento)
        mColRutaAlmacenamiento.Remove(RecuperarRutaxGUID(miRutaAlmacenamiento.GUID))
        mColRutaAlmacenamiento.Add(miRutaAlmacenamiento)
        CtrlRutaAlmacenamiento1.RutaAlmacenamiento = miRutaAlmacenamiento
        RellenarFilaDataGridView(miRutaAlmacenamiento, mIndiceFilaSeleccionada)
        ActualizarBotonesEstado(RutaAlmacenamientoFicherosEstado.Cerrada)
    End Sub

    Private Sub EstadoInicialRuta()
        CtrlRutaAlmacenamiento1.RutaAlmacenamientoEstado = RutaAlmacenamientoFicherosEstado.Disponible
        RecuperarRutaControl()
        ActualizarBotonesEstado(RutaAlmacenamientoFicherosEstado.Disponible)
    End Sub

#End Region

End Class
