Public Class frmAdjuntarPagoFT

#Region "Atributos"

    Private mControlador As frmAdjuntarPagoFTctrl
    Private mColFicherosTransferencias As FN.GestionPagos.DN.ColFicheroTransferenciaDN
    Private mDatatableOrigen As DataTable
    Private mListaIDs As List(Of String)

#End Region

#Region "Inicializar"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        mControlador = Me.Controlador

        If Me.Paquete Is Nothing Then
            Throw New ApplicationException("El paquete está vacío")
        End If

        If Not Me.Paquete.Contains("DataTable") Then
            Throw New ApplicationException("No se ha pasado la tabla de datos en el paquete")
        End If

        If Not Me.Paquete.Contains("IDMultiple") Then
            Throw New ApplicationException("No se han pasado los identificadores de los objetos en el paquete")
        End If

        mListaIDs = Me.Paquete("IDMultiple")

        CargarListaFT()

        CargarDatos(Me.Paquete("DataTable"), mListaIDs)

        If Me.mDatatableOrigen.Rows.Count = 0 Then
            MessageBox.Show("No hay ningún pago para adjuntar", "Adjuntar pago a fichero de transferencias", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Me.Close()
        End If

    End Sub

#End Region

#Region "Métodos botones"

    Private Sub btnNuevoFT_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNuevoFT.Click
        Try
            'Se navega al formulario de nuevo fichero de transferencias
            Dim paquete As New Hashtable()
            paquete.Add("TipoEntidad", GetType(GestionPagos.DN.FicheroTransferenciaDN))
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(CType(Me, MotorIU.FormulariosP.IFormularioP), paquete, MotorIU.Motor.TipoNavegacion.Normal)

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub btnAdjuntarPagosFT_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAdjuntarPagosFT.Click
        Try
            'Se adjuntan los pagos a la coleccón de pagos del fichero seleccionado
            Dim ft As GestionPagos.DN.FicheroTransferenciaDN = Nothing

            If lbFicherosTransferencias.SelectedItem IsNot Nothing Then
                ft = lbFicherosTransferencias.SelectedItem
            Else
                MessageBox.Show("No hay ningún fichero de transferencias seleccionado", "Navegar a fichero de transferencias", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If

            Dim listaPagos As IList
            Dim arrL As New ArrayList()
            For Each elto As String In mListaIDs
                arrL.Add(elto)
            Next
            listaPagos = Me.mControlador.RecuperarListaPagosxIDs(arrL)

            'Se ejecuta la operación de adjuntar pago a fichero transferencia, en todos los pagos seleccionados
            If listaPagos IsNot Nothing Then
                Dim mensajeErr As String
                For Each pagoAFT As FN.GestionPagos.DN.PagoDN In listaPagos
                    mensajeErr = ""
                    pagoAFT.IdFicheroTransferencia = ft.ID
                    mControlador.EjecutarAdjuntarPagoFT(pagoAFT, mensajeErr)
                Next
            End If

            'Se cierra el formulario
            Close()

        Catch ex As Exception
            MostrarError(ex)
        End Try

    End Sub

    Private Sub btnNavegarFT_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNavegarFT.Click
        Try
            'Navegar al fichero seleccionado
            Dim ft As GestionPagos.DN.FicheroTransferenciaDN = Nothing

            If lbFicherosTransferencias.SelectedItem IsNot Nothing Then
                ft = lbFicherosTransferencias.SelectedItem
            Else
                MessageBox.Show("No hay ningún fichero de transferencias seleccionado", "Navegar a fichero de transferencias", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If


            Dim paquete As New Hashtable()
            paquete.Add("DN", ft)
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(CType(Me, MotorIU.FormulariosP.IFormularioP), paquete, MotorIU.Motor.TipoNavegacion.Modal)

            'Después de la navegación se actualiza el list box de ficheros de transferencias activos
            CargarListaFT()

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub btnRefrescarListaFT_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnRefrescarListaFT.Click
        Try
            CargarListaFT()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#End Region

#Region "Métodos"

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

        'establecemos el datasource
        Me.mDatatableOrigen = midt
        Me.dgvPagos.DataSource = Me.mDatatableOrigen
        'Me.DataGridView1.Columns(0).Visible = False
        For a As Int16 = 0 To Me.dgvPagos.Columns.Count - 1
            'hacemos todas las columnas de solo lectura
            Me.dgvPagos.Columns(a).ReadOnly = True
        Next

    End Sub

    Private Sub CargarListaFT()
        Me.lbFicherosTransferencias.Items.Clear()
        Me.lbFicherosTransferencias.DisplayMember = "ToString()"

        mColFicherosTransferencias = mControlador.RecuperarFicherosTransferenciasActivos()
        If mColFicherosTransferencias IsNot Nothing Then
            Me.lbFicherosTransferencias.Items.AddRange(mColFicherosTransferencias.ToArray())
        End If

    End Sub

#End Region


End Class