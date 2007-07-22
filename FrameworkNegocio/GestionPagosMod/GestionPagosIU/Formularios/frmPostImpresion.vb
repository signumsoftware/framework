Public Class frmPostImpresion

#Region "atributos"
    Private mDatatatable As DataTable
    Private mHTTalonesImpresos As Hashtable
    Private mControlador As frmPostImpresionctrl
#End Region

#Region "inicializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        Me.mControlador = Me.Controlador

        Dim mipaquete As PaquetePostImpresion = Me.Paquete("Paquete")

        mHTTalonesImpresos = mipaquete.TalonesImpresos
        Dim miDatatable As DataTable = mipaquete.Datatable

        CargarDatos(miDatatable)
    End Sub

    Private Sub CargarDatos(ByVal pDatatable As DataTable)
        'metemos en una tabla los registros de los talones que sí se han impreso
        'y en otra los registros de los queno se han impreso por algún motivo
        'y luego los asignamos a sus datagridviews correspondientes

        Dim dtno As New DataTable
        Dim dtsi As New DataTable
        dtno.Merge(pDatatable)
        dtsi.Merge(pDatatable)

        'nos cargamos las filas que sí han sido impresas de la tabla del NO
        Dim listaeliminar As New List(Of DataRow)
        For Each mir As DataRow In dtno.Rows
            If Me.mHTTalonesImpresos.Contains(mir(0)) Then
                'ha sido impreso
                listaeliminar.Add(mir)
            End If
        Next
        For Each mir As DataRow In listaeliminar
            dtno.Rows.Remove(mir)
        Next

        'agregamos la columna de Resultado Impresión a la tabla del SI
        dtsi.Columns.Add(New DataColumn("Impresión Correcta", GetType(Boolean)))

        'nos cargamos las filar que no han sido impresas de la tabla del SI
        listaeliminar.Clear()
        For Each mir As DataRow In dtsi.Rows
            If Not Me.mHTTalonesImpresos.Contains(mir(0)) Then
                'no ha sido impreso
                listaeliminar.Add(mir)
            Else
                'ponemos false en la columna del estado impresión
                'para evitar que luego sea DBNULL
                mir("Impresión Correcta") = False
            End If
        Next
        For Each mir As DataRow In listaeliminar
            dtsi.Rows.Remove(mir)
        Next


        Me.mDatatatable = dtsi

        'asignamos los orígenes de datos a los datagrids
        Me.DataGridView1.DataSource = Me.mDatatatable

        'hacemos editable sólo la última columna (impresión correcta)
        For Each dc As DataGridViewColumn In Me.DataGridView1.Columns
            If dc.Name <> "Impresión Correcta" Then
                dc.ReadOnly = True
            End If
            'hacemos que todas las columnas sean wordwrap
            dc.DefaultCellStyle.WrapMode = DataGridViewTriState.True
        Next

        Me.DataGridView2.DataSource = dtno
        'hacemos el datagrid no editable
        Me.DataGridView2.ReadOnly = True
        'hacmeos todas las columnas wordwrap
        For Each dc As DataGridViewColumn In Me.DataGridView2.Columns
            dc.DefaultCellStyle.WrapMode = DataGridViewTriState.True
        Next

    End Sub
#End Region

#Region "métodos"
    Private Sub cmdIncidentarTodos_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdIncidentarTodos.Click
        Try
            For Each mir As DataGridViewRow In Me.DataGridView1.Rows
                If Not mir.ReadOnly Then
                    mir.Cells("Impresión Correcta").Value = False
                End If
            Next

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub cmdTodosOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdTodosOK.Click
        Try
            For Each mir As DataGridViewRow In Me.DataGridView1.Rows
                If Not mir.ReadOnly Then
                    mir.Cells("Impresión Correcta").Value = True
                End If
            Next
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#Region "ARTEFACTOS GUARDADO ESTADO TALONES"
    Private mEstadoProceso As EstadoProceso = EstadoProceso.sincomenzar

    Private Sub BackgroundWorker1_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        'guardamos ref al bw
        Dim bw As System.ComponentModel.BackgroundWorker = sender

        'decimos que estamos guardando
        Me.mEstadoProceso = EstadoProceso.guardando

        'actualizamos el contenido de los talones y los agregamos a su TalonDN
        'correspondiente
        Dim a As Integer = 0

        For Each dr As DataRow In Me.mDatatatable.Rows
            Try
                'obtenemos el td correspondiente si es que existe
                '(sólo estará en la lista si se Imprimió, si se canceló y 
                'no se llegó a enviar a la impresora se encontrará en la tabla 
                'pero no en el hashtable como objeto)
                Dim td As FN.GestionPagos.DN.TalonDocumentoDN = Me.mHTTalonesImpresos(dr(0))
                td.Anulado = Not (dr("Impresión Correcta"))
                'guardamos el TalonDoc
                If td.Anulado Then
                    Me.mControlador.AnularImpresionTalon(td.Talon.Pago)
                Else
                    Me.mControlador.ValidarImpresionTalon(td.Talon.Pago)
                End If
            Catch ex As Exception
                Dim obj(0) As Object
                obj(0) = ex
                Me.Invoke(New LanzarErrorTalon(AddressOf ErrorValidandoTalon), obj)
            End Try
            a += 1
            bw.ReportProgress(a)
        Next
    End Sub

    Private Delegate Sub LanzarErrorTalon(ByVal ex As Exception)

    Private Sub ErrorValidandoTalon(ByVal ex As Exception)
        MostrarError(ex, "Error al validar el estado del Talon Impreso")
    End Sub

    Private Sub BackgroundWorker1_ProgressChanged(ByVal sender As Object, ByVal e As System.ComponentModel.ProgressChangedEventArgs) Handles BackgroundWorker1.ProgressChanged
        Me.ProgressBar1.Value = e.ProgressPercentage
    End Sub

    Private Sub BackgroundWorker1_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        'volvemos a habilitar el botón de aceptar
        Me.grpGuardar.Visible = False
        Me.cmd_Aceptar.Visible = True
        Me.cmd_Aceptar.Enabled = True

        'decimos que hemos terminado
        Me.mEstadoProceso = EstadoProceso.terminado

        MessageBox.Show("Se ha completado el proceso de impresión", "Proceso completado", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub


    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Aceptar.Click
        Try
            Select Case Me.mEstadoProceso
                Case EstadoProceso.sincomenzar
                    'lanzamos el proceso de guardado y nos deshabilitamos
                    Me.cmd_Aceptar.Visible = False

                    'mostramos el groupbox 
                    Me.grpGuardar.Visible = True
                    Me.DataGridView1.Enabled = False
                    Me.cmdIncidentarTodos.Visible = False
                    Me.cmdTodosOK.Visible = False

                    Me.ProgressBar1.Maximum = Me.mDatatatable.Rows.Count

                    Me.BackgroundWorker1.RunWorkerAsync()
                Case EstadoProceso.guardando
                    'no hacemos nada, porque hay que esperar a que termine el proceso de guardado 
                    '(no debería darse nunca porque el botón se hallará invisible)
                    Exit Sub
                Case EstadoProceso.terminado
                    'salimos
                    Me.Close()
            End Select
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub
#End Region

#Region "control de cerrado"
    Private Sub CerrandoForm(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        Select Case Me.mEstadoProceso
            Case EstadoProceso.sincomenzar
                MessageBox.Show("Debe definir el resultado de la impresión para los talones que se han imprimido y hacer click en Aceptar para guardar el resultado de la impresión", "Salir Interrumpido", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                e.Cancel = True
                Exit Sub
            Case EstadoProceso.guardando
                MessageBox.Show("No se puede cerrar la ventana hasta que no se haya completado el proceso de validación de los estados de los talones", "Salir Interrumpido", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                e.Cancel = True
                Exit Sub
            Case EstadoProceso.terminado
                'ya se puede cerrar sin problema
                Exit Sub
        End Select
    End Sub
#End Region

#End Region


    Private Enum EstadoProceso As Integer
        sincomenzar = 0
        guardando = 1
        terminado = 2
    End Enum





End Class