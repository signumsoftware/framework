Public Class frmEjecutarOperacion

#Region "events"
    Public Event OperacionTerminada(ByVal sender As Form, ByVal resultado As ResultadoEjecucion)
#End Region

#Region "atributos"
    'el estado por defecto es sin comenzar
    Private mEstadoejecucion As EstadoEjecucion = ProcesosIU.EstadoEjecucion.sincomenzar
    Private mTransicion As Framework.Procesos.ProcesosDN.TransicionDN
    Private mDatataTableDatos As DataTable
    Private mResultadoejecucion As ResultadoEjecucion
    Private mTipoObjeto As Type
#End Region

#Region "inicializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        If Not Me.Paquete Is Nothing AndAlso Me.Paquete.Contains("Paquete") Then
            Dim mipaquete As PaqueteFormularioEjecutarOperacion = Me.Paquete("Paquete")
            Me.mTransicion = mipaquete.TransicionAEjecutar
            Me.mTipoObjeto = mipaquete.TipoObjeto
            Me.CargarDatos(mipaquete.DataTableDatos)

            Me.lblOperacion.Text = Me.mTransicion.OperacionDestino.VerboOperacion.Nombre
        Else
            Throw New ApplicationException("El Paquete de Configuración de está vacío")
        End If

    End Sub
#End Region

#Region "propiedades"

    ''' <summary>
    ''' Devuelve el estado acual del proceso de ejecución
    ''' </summary>
    Public ReadOnly Property EstadoEjecucion() As EstadoEjecucion
        Get
            Return Me.mEstadoejecucion
        End Get
    End Property

#End Region

#Region "métodos"

#Region "CARGAR DATOS"
    ''' <summary>
    ''' Carga los datos del datatable en la tabla que se va
    ''' a enviar a la ejecución de la operación
    ''' </summary>
    ''' <param name="pDataTable">El datatable origen de los datos que se van a 
    ''' añadir a la ejecución de la operación</param>
    Public Sub CargarDatos(ByVal pDataTable As DataTable)
        If pDataTable Is Nothing Then
            Throw New ApplicationException("La tabla de datos a ejecutar está vacía")
        End If

        'si no estamos sin comenzar no permitimos que haga nada
        If Me.mEstadoejecucion <> ProcesosIU.EstadoEjecucion.sincomenzar Then
            MessageBox.Show("No se pueden cargar datos en el estado actual", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Exit Sub
        End If

        'cortamos el display
        Me.DataGridView1.SuspendLayout()
        Try
            'si el datatable origen está vacío, lo creamos
            If Me.DataGridView1.DataSource Is Nothing Then
                Me.mDatataTableDatos = New DataTable()

                'creamos las mismas columnas que haya en el datatable origen
                For Each mic As DataColumn In pDataTable.Columns
                    Me.mDatataTableDatos.Columns.Add(New DataColumn(mic.ColumnName, mic.DataType))
                Next
            End If


            Me.mDatataTableDatos.Merge(pDataTable, False, MissingSchemaAction.Error)

            'ojo: comprobar que no existiera de antes ese registro
            ' ''ahora agregamos los registros nuevos al datatable origen
            ''For Each mir As DataRow In pDataTable.Rows
            ''    Dim r As DataRow = Me.mDatataTableDatos.NewRow()
            ''    For a As Integer = 0 To Me.mDatataTableDatos.Columns.Count - 1
            ''        r(a) = mir(a)
            ''    Next
            ''    Me.mDatataTableDatos.Rows.Add(r)
            ''Next

            'establecemos el datasource del datagridview
            Me.DataGridView1.DataSource = Me.mDatataTableDatos
            'hacemos invisible la 1ª columna (ID)
            Me.DataGridView1.Columns(0).Visible = False

        Catch ex As Exception
            Throw
        Finally
            'restablecemos el display
            Me.DataGridView1.ResumeLayout()
        End Try

    End Sub

#End Region

    Private t As New Threading.Thread(AddressOf Ejecucion)
    Private mCancelarOperacion As Boolean = False

#Region "EJECUCION"
    Private Sub cmdEjecutar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdEjecutar.Click
        Try
            'habilitamos la visualizacion de ejecución
            Me.mEstadoejecucion = ProcesosIU.EstadoEjecucion.enproceso
            Me.DataGridView1.Height = Me.DataGridView1.Height - (Me.Panel1.Height + 20)
            Me.Panel1.Visible = True
            Me.pnlBotones.Enabled = False

            'lanzamos el hilo que se encarga de hacerlo de todo de manera
            'independiente
            t.Priority = Threading.ThreadPriority.Normal
            t.Name = "Ejecucion"
            t.Start()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    ''' <summary>
    ''' El método que ejecuta las operaciones y va mostrando los resultados 
    ''' según los da el servidor
    ''' </summary>
    Private Sub Ejecucion()
        Try
            Dim mias As New Framework.Procesos.ProcesosAS.OperacionesAS

            Dim miproceso As New Framework.Procesos.ProcesosDN.ProcesoDeEjecucionDN(Me.cMarco.Principal, Me.mDatataTableDatos, Me.mTransicion, Me.mTipoObjeto)

            Me.ProgressBar1.Maximum = 100

            Dim miticket As String = mias.EjecutarOperacionEnBloque(miproceso)

            'iteracion
            Do While Not miproceso.Completado
                'esperamos un rato
                Threading.Thread.Sleep(5000)

                'obtenemos el nuevo proceso
                miproceso = mias.RecuperarProceso(miticket)

                'establecemos como datasource la tabla del proceso
                Me.DataGridView1.DataSource = miproceso.Datatable
                Me.DataGridView1.Columns(0).Visible = False

                'ponemos en el progressbar el porcentaje actual
                Me.ProgressBar1.Value = Integer.Parse(miproceso.PorcentajeCompletado)
                Me.lblPasoActual.Text = miproceso.PorcentajeCompletado.ToString & " %"
            Loop


            'habilitamos la visualización de salida
            Me.Panel1.Visible = False
            Me.DataGridView1.Height += (Me.Panel1.Height + 20)
            Me.cmdCancelar.Visible = False
            Me.cmdEjecutar.Visible = False
            Me.cmdSalir.Visible = True
            Me.cmdExcel.Visible = True
            Me.pnlBotones.Enabled = True

            'traemos el formulario al frente
            Me.BringToFront()

            'lanzamos un mensaje diciendo que está listo
            MessageBox.Show("El Proceso de Ejecuciones ha sido completado", "Proceso de Ejecución de Operaciones completado", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            Me.BringToFront()
            MostrarError(ex, "Error en la monitorización de la Ejecución del Proceso")
        End Try
    End Sub

    ''' <summary>
    ''' Comprueba si han solicitado el final de la ejecución y en caso afirmativo
    ''' aborta en hilo de ejecución, apuntando el resultado como cancelación
    ''' </summary>
    Private Sub ComprobarAbort()
        If mCancelarOperacion Then
            mResultadoejecucion = ResultadoEjecucion.cancelada
            Threading.Thread.CurrentThread.Abort()
        End If
    End Sub
#End Region

#Region "CANCELAR Y SALIR"
    Private Sub cmdSalir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdSalir.Click
        Try
            Me.Close()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Try
            Me.Close()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    ''' <summary>
    ''' Controla el closing del Form y es el encargado de permititr que se cierre o no, de
    ''' abortar el hilo si es necesario y de lanzar el evento de finalización
    ''' </summary>
    Private Sub CerrandoForm(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        If Me.mEstadoejecucion = ProcesosIU.EstadoEjecucion.enproceso Then
            If MessageBox.Show("El proceso de ejecución está en curso." & Chr(13) & Chr(13) & "¿Seguro que desea cancelar la ejecución y no ejecutar las operaciones que aún no se han iniciado?", "Cancelar", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) = Windows.Forms.DialogResult.Yes Then
                'le indicamos al hilo en ejecucion que debe cancelarlo
                'y salir - (lo debe hacer él) 
                Me.mCancelarOperacion = True
                'ahora esperamos hasta que el hilo de ejecucion haya terminado
                t.Join()
                'decimos cómo ha terminado la operación
                RaiseEvent OperacionTerminada(Me, Me.mResultadoejecucion)
            Else
                'si cancela, cancelamos el proceso de cierre
                e.Cancel = True
            End If
        Else
            'está en otro estado

            If Me.mEstadoejecucion = ProcesosIU.EstadoEjecucion.sincomenzar Then
                'aún no se ha lanzado nada
                If MessageBox.Show("¿Seguro que desea cancelar el proceso y no ejecutar ninguna operación?", "Cancelar ejecución " & Me.mTransicion.OperacionDestino.Nombre, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = Windows.Forms.DialogResult.Yes Then
                    'indicamos que se ha cancelado sin hacer nada
                    RaiseEvent OperacionTerminada(Me, ResultadoEjecucion.cancelada)
                Else
                    'cancelamos el proceso de cierre
                    e.Cancel = True
                End If


                'ya ha terminado la ejecución
            ElseIf Me.mEstadoejecucion = ProcesosIU.EstadoEjecucion.terminado Then
                'lanzamos el evento diciendo que se ha terminado e informa del resultado
                RaiseEvent OperacionTerminada(Me, Me.mResultadoejecucion)
            End If
        End If
    End Sub

#End Region

#Region "Eliminar Filas"
    Private Sub cmdEliminarElemento_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdEliminarElemento.Click
        Try
            If Me.DataGridView1.SelectedRows.Count = 0 Then
                MessageBox.Show("Debe seleccionar alguna fila de la tabla", "Eliminar elemento", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If

            For Each mir As DataGridViewRow In Me.DataGridView1.SelectedRows
                Me.DataGridView1.Rows.Remove(mir)
            Next
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub
#End Region


#End Region







End Class

Public Enum EstadoEjecucion As Integer
    sincomenzar = 0
    enproceso = 1
    terminado = 2
End Enum

Public Enum ResultadoEjecucion As Integer
    cancelada = 0
    errores = 1
    exito = 2
End Enum

Public Class PaqueteFormularioEjecutarOperacion
    Inherits MotorIU.PaqueteIU

    Public DataTableDatos As DataTable
    Public TransicionAEjecutar As Framework.Procesos.ProcesosDN.TransicionDN
    Public TipoObjeto As System.Type

End Class