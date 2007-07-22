Public Class frmPreImpresion

    Private mconfiguracionimpresion As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN
    Private mControlador As frmPreImpresionctrl
    Private mDatatableOrigen As DataTable

#Region "inicializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = Me.Controlador

        If Me.Paquete Is Nothing Then
            Throw New ApplicationException("El paquete está vacío")
        End If
        If Not Me.Paquete.Contains("DataTable") Then
            Throw New ApplicationException("No se ha pasado la tabla de datos en el paquete")
        End If
        If Not Me.Paquete.Contains("IDMultiple") Then
            Throw New ApplicationException("No se han pasado los identificadores de los objetos en el paquete")
        End If

        CargarDatos(Me.Paquete("DataTable"), Me.Paquete("IDMultiple"))

        If Me.mDatatableOrigen.Rows.Count = 0 Then
            MessageBox.Show("No hay ningún elemento a imprimir", "Impresión de Talones", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Me.Close()
        End If

        Me.cboConfiguracionImpresion.DisplayMember = "Nombre"
        Dim milista As List(Of FN.GestionPagos.DN.ConfiguracionImpresionTalonDN) = LNC.RecuperarTodasConfiguracionesImpresion

        For Each ci As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN In milista
            Me.cboConfiguracionImpresion.Items.Add(ci)
        Next

    End Sub

    Private Sub CargarDatos(ByVal pDatataTable As DataTable, ByVal listaIDs As List(Of String))
        'tiene que ser un datatable nuevo, para que no mantenga una
        'referencia a él el buscador
        Dim midt As DataTable = pDatataTable.Clone

        midt.Merge(pDatataTable)

        'quitamos las filas que no están seleccionadas
        Dim rowsaborrar As New List(Of DataRow)

        For Each mir As DataRow In midt.Rows
            If Not listaIDs.Contains(mir(0)) Then
                rowsaborrar.Add(mir)
            End If
        Next

        For Each mir As DataRow In rowsaborrar
            midt.Rows.Remove(mir)
        Next

        'agregamos la columna con el nº de serie
        midt.Columns.Add(New DataColumn("Nº Serie", GetType(String)))

        'establecemos el datasource
        Me.mDatatableOrigen = midt
        Me.DataGridView1.DataSource = Me.mDatatableOrigen
        'Me.DataGridView1.Columns(0).Visible = False
        For a As Int16 = 0 To Me.DataGridView1.Columns.Count - 2
            'hacemos sólo editable la columna de nº de serie (la última)
            Me.DataGridView1.Columns(a).ReadOnly = True
        Next

    End Sub


#End Region

#Region "métodos"

#Region "configuración de impresión"
    Private Sub cboConfiguracionImpresion_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cboConfiguracionImpresion.SelectedIndexChanged
        Try
            Me.mconfiguracionimpresion = CType(Me.cboConfiguracionImpresion.SelectedItem, FN.GestionPagos.DN.ConfiguracionImpresionTalonDN)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#End Region

#Region "números de serie"
    Private Sub cmdNumerosSerie_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdNumerosSerie.Click
        Try
            Dim minumero As String = Microsoft.VisualBasic.InputBox("Escriba el número de serie inicial", "Número de Serie de los Talones", "")
            If Not String.IsNullOrEmpty(minumero.Trim) AndAlso IsNumeric(minumero) Then
                Dim numint As Long = CLng(minumero)
                For Each mir As DataRow In Me.mDatatableOrigen.Rows
                    mir("Nº Serie") = String.Format("{0:D}", numint)
                    numint += 1
                Next
            Else
                MessageBox.Show("El número de serie no es correcto", "Número de sere de Talones", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If

        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub
#End Region

#Region "imprimir talones"

    ''' <summary>
    ''' Guardamos los TalonDoc relacionados con su ID para
    ''' pasárselos luego al formulario de postimpresión
    ''' y que no tenga que volver a recuperar los objs del servidor
    ''' </summary>
    Private mTalonesImpresos As New Hashtable

    Private mTalonEnCurso As String = String.Empty

    Private Sub BackgroundWorker1_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork

        'referencia al bgworker
        Dim bw As System.ComponentModel.BackgroundWorker = sender

        Dim fm As New AuxIU.FormateadorMonedaEurosConSimbolo()
        fm.NumeroDecimales = 2

        'agregamos la columna de impresion al datatable
        Me.mDatatableOrigen.Columns.Add(New DataColumn("Impreso", GetType(Boolean)))

        'agregamos la columna del comentario de la impresión al datatable
        Me.mDatatableOrigen.Columns.Add(New DataColumn("Comentario", GetType(String)))

        Dim numfilas As Integer = 0

        For Each mir As DataRow In Me.mDatatableOrigen.Rows
            'si nos han dicho que hay que cancelar, salimos del bucle
            If Me.mCancelar Then
                Exit For
            End If

            'recuperamos el talonDN que corresponde a ese registro
            Dim talon As FN.GestionPagos.DN.TalonDN = Me.mControlador.RecuperarPagoDN(mir(0)).Talon

            'si tiene una plantilla pero no un texto, hay que generar
            'automáticamente el texto a partir de la plantilla
            LNC.CargarHuella(talon.HuellaRTF)
            If Not talon.PlantillaCarta Is Nothing Then
                If talon.HuellaRTF Is Nothing Then
                    LNC.GenerarTextoCartaConPlantilla(talon)
                End If
            End If

            Dim td As New FN.GestionPagos.DN.TalonDocumentoDN()
            'al pasarle el talonDN él solo rellena todos los datos
            td.Talon = talon

            'generamos el texto de la carta para el talondoc a partir del texto del 
            'talón si es que lo tiene
            If Not td.HuellaRTF Is Nothing Then
                'si ya tiene huella, como está recién creada está cargada
                'lo pasamos por los reemplazos
                LNC.PasarTextoPorReemplazos(td)
            End If

            'le ponemos el número de serie
            td.NumeroSerie = mir("Nº Serie").ToString
            'le ponemos la fecha del talón
            td.FechaTalon = New Date(Now.Year, Now.Month, Now.Day)

            'lo agregamos a la colección de talones impresos
            'si luego no se puede imprimir no se guardará (ejecución de la operación "imprimir", así que
            'sólo se quedará en el lado del cliente)
            talon.ColTalonesImpresos.Add(td)

            'ponemos la fecha de impresion (idem)
            td.FechaImpresion = New Date(Now.Year, Now.Month, Now.Day)

            'apuntamos el talón que estamos imprimiendo
            Me.mTalonEnCurso = td.NumeroSerie & " - " & td.Destinatario & " - " & fm.Formatear(td.Importe.ToString)




            'definimos el paquete de impresion
            Dim mipaqueteimpresion As New PaqueteImpresion
            mipaqueteimpresion.ConfiguracionImpresion = Me.mconfiguracionimpresion
            mipaqueteimpresion.TalonDocumento = td
            mipaqueteimpresion.ImpresionSilenciosa = Me.optAutomatica.Checked
            mipaqueteimpresion.PrinterSettings = mPrinterSettings

            Dim mipaquete As Hashtable = mipaqueteimpresion.GenerarPaquete

            Dim miobj(0) As Object
            miobj(0) = mipaquete

            'llamamos sincrónicamente al método que lanza el formulario de impresión desde el 
            'hilo principal de la aplicación para no hacer un crossingthread con el iu
            mipaquete = Me.Invoke(New LanzamientoImpresionDelegado(AddressOf LanzarFormImpresion), miobj)

            'apuntamos en el datable si se ha impreso o se ha cancelado
            'por el usuario
            mir("Impreso") = mipaqueteimpresion.Impreso

            'si no se ha cancelado 
            If mipaqueteimpresion.Impreso Then
                ''guardamos el talon
                'talon = LNC.GuardarTalonDN(talon)
                'lo metemos en la lista de talones que se han impreso para pasar al post-impresión
                Me.mTalonesImpresos.Add(mir(0), td)
            Else
                Dim comentario As String = String.Empty
                'comprobamos si hay un error o lo ha cancelado el usuario
                If mipaqueteimpresion.MensajeError = String.Empty Then
                    comentario = "Impresión cancelada por el usuario"
                Else
                    comentario = mipaqueteimpresion.MensajeError
                End If
                mir("Comentario") = comentario
            End If

            'aumentamos el valor del progressbar
            numfilas += 1
            bw.ReportProgress(numfilas)
        Next


    End Sub

    'el delegado a la función de sincronización
    Private Delegate Function LanzamientoImpresionDelegado(ByVal paquete As Hashtable) As Hashtable

    ''' <summary>
    ''' lanza el formulario de impresión y es invocado de manera asíncrona por el hilo de impresión
    ''' </summary>
    ''' <remarks></remarks>
    Private Function LanzarFormImpresion(ByVal mipaquete As Hashtable) As Hashtable
        Me.cMarco.Navegar("ImpresionTalon", Me, Me.MdiParent, MotorIU.Motor.TipoNavegacion.Modal, mipaquete)
        Return mipaquete
    End Function

    Private Sub BackgroundWorker1_ProgressChanged(ByVal sender As Object, ByVal e As System.ComponentModel.ProgressChangedEventArgs) Handles BackgroundWorker1.ProgressChanged
        Me.ProgressBar1.Value = e.ProgressPercentage
        Me.lblOperacionEnCurso.Text = Me.mTalonEnCurso
        Me.DataGridView1.DataSource = Me.mDatatableOrigen
        Me.DataGridView1.Refresh()
    End Sub


    Private Sub BackgroundWorker1_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        If Not Me.mCancelar Then
            'una vez terminado el proceso de impresión, avisamos y navegamos al formulario de
            'postimpresión
            MessageBox.Show("Se ha completado el proceso de impresión de talones." & Chr(13) & Chr(13) & "A continuación debe indicar si los talones se han impreso correctamente", "Impresión completada", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Else
            'nos han cancelado, así que hay que los talones que no se llegaron a
            'imprmir quedan como no impresos (deaultvalue de la columna "Impreso"
            MessageBox.Show("El proceso de impresión de talones ha sido cancelado por el usuario" & Chr(13) & Chr(13) & "A continuación debe indicar, si procede, si los talones se han impreso correctamente", "Impresión Cancelada", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If


        Dim ppi As New PaquetePostImpresion
        ppi.Datatable = Me.mDatatableOrigen
        ppi.TalonesImpresos = Me.mTalonesImpresos

        'advertimos que vamos a navegar
        Me.mEstadoImpresion = EstadoImpresion.navegandoapostimpresion

        Me.cMarco.Navegar("PostImpresionTalones", Me, Me.ParentForm, MotorIU.Motor.TipoNavegacion.CerrarLanzador, ppi.GenerarPaquete)
    End Sub

    Private mPrinterSettings As System.Drawing.Printing.PrinterSettings

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Aceptar.Click
        Try
            If Not Me.ComprobarEstadoParaImprimir() Then
                Exit Sub
            End If

            If Me.optAutomatica.Checked Then
                If Me.PrintDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
                    mPrinterSettings = Me.PrintDialog1.PrinterSettings
                Else
                    Exit Sub
                End If
            End If

            Me.cmd_Aceptar.Enabled = False
            Me.DataGridView1.ReadOnly = True

            'mostramos el progressbar
            Me.grpConfiguracion.Visible = False
            Me.grpProgreso.Visible = True

            'ponemos el valor máximo del progressbar en función del nº de talones
            'que hay que imprimir
            Me.ProgressBar1.Maximum = Me.mDatatableOrigen.Rows.Count

            'decimos que estamos en ello
            Me.mEstadoImpresion = EstadoImpresion.enproceso

            'llamamos al bckw asíncronamente
            Me.BackgroundWorker1.RunWorkerAsync()


        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    ''' <summary>
    ''' Valida que todos los datos obligatorios para poder imprimir están correctos.
    ''' Si alguno no lo está, lanza un messagebox diciéndolo y devuelve false
    ''' </summary>
    Private Function ComprobarEstadoParaImprimir() As Boolean
        'comprobamos que tiene una configuracion de impresion
        If Me.mconfiguracionimpresion Is Nothing Then
            MessageBox.Show("No se ha definido una configuración de impresión", "¡Atención!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return False
        End If

        'comprobamos que hay un número de serie para todos los talones
        For Each mir As DataRow In Me.mDatatableOrigen.Rows
            If String.IsNullOrEmpty(mir("Nº Serie").ToString) Then
                MessageBox.Show("No hay un Número de Serie asignado para todos los talones", "¡Atención!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Return False
            End If
        Next

        'si llegamos aquí es que todo está correcto
        Return True
    End Function

#End Region

#Region "control de cancelación"
    Private mEstadoImpresion As EstadoImpresion
    Private mCancelar As Boolean = False

    Public Sub CerrarForm(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        'comprobamos el estado en el que estamos
        Select Case Me.mEstadoImpresion
            Case EstadoImpresion.sincomenzar
                If MessageBox.Show("¿Desea cancelar el proceso de impresión y salir sin imprimir ninguno de los talones?", "Salir", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = Windows.Forms.DialogResult.Yes Then
                    'sale sin cancelar el cierre
                    Exit Sub
                Else
                    e.Cancel = True
                    Exit Sub
                End If
            Case EstadoImpresion.enproceso
                If MessageBox.Show("¿Desea cancelar el proceso de impresión y no imprimir los talones que quedan pendientes?", "Cancelar impresión", MessageBoxButtons.OK, MessageBoxIcon.Question) = Windows.Forms.DialogResult.Yes Then
                    'sólo decimos que queremos cancelar, y el hilo recogerá 
                    'la advertencia y será el que cierre
                    Me.mCancelar = True
                End If
                e.Cancel = True
                Exit Sub
            Case EstadoImpresion.navegandoapostimpresion
                'ya se ha encargado el hilo de terminar o cancelar el proceso
                Exit Sub
        End Select
    End Sub
#End Region

#End Region


    Private Enum EstadoImpresion As Integer
        sincomenzar = 0
        enproceso = 1
        navegandoapostimpresion = 3
    End Enum


    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Try
            Me.Close()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub
End Class