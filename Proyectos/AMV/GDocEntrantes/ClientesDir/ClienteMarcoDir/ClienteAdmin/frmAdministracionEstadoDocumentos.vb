Public Class frmAdministracionEstadoDocumentos

#Region "atributos"
    Private mControlador As frmAdministracionEstadoDocumentosctrl
    Private mDatataTable As DataTable
#End Region

#Region "inicializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = Me.Controlador

        If Not Me.Paquete Is Nothing Then
            If Me.Paquete.Contains("DataTable") Then
                'hacemos un clon "profundo" para obtener una copia del datatable
                Dim paquete As Byte() = Framework.Utilidades.Serializador.Serializar(Me.Paquete("DataTable"))
                Me.mDatataTable = CType(Framework.Utilidades.Serializador.DesSerializar(paquete), DataTable)

                Dim listaids As List(Of String) = Me.Paquete("IDMultiple")
                Dim rowsaborrar As New List(Of DataRow)

                For Each dtr As DataRow In Me.mDatataTable.Rows
                    Dim estaenlalista As Boolean = False
                    For Each id As String In listaids
                        If dtr(0).ToString = id Then
                            estaenlalista = True
                            Exit For
                        End If
                    Next
                    If Not estaenlalista Then
                        rowsaborrar.Add(dtr)
                    End If
                Next

                For Each dr As DataRow In rowsaborrar
                    Me.mDatataTable.Rows.Remove(dr)
                Next

                Me.DataGridView1.DataSource = mDatataTable
                Me.DataGridView1.Columns(0).Visible = 0


                'ponemos la última columna para que ocupe hasta el final
                If Not Me.DataGridView1.Columns Is Nothing AndAlso Me.DataGridView1.Columns.Count <> 0 Then
                    Me.DataGridView1.Columns(Me.DataGridView1.Columns.Count - 1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                End If
            End If

            'rellenamos el combo
            cboEstado.DataSource = System.Enum.GetValues(GetType(AmvDocumentosDN.EstadosRelacionENFichero))

            'Me.cboEstado.Items.Add([Enum].GetValues(GetType(AmvDocumentosDN.EstadosRelacionENFichero)))

        End If
    End Sub
#End Region

#Region "métodos"
    Private Sub cmdContinuar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdContinuar.Click
        Try
            'If Me.DataGridView1.SelectedRows.Count = 0 Then
            '    MessageBox.Show("Debe seleccionar algún documento")
            '    Exit Sub
            'End If

            If Me.cboEstado.SelectedItem Is Nothing Then
                MessageBox.Show("Debe seleccionar el Estado que quiere asignar a los Documentos seleccionados")
                Exit Sub
            End If


            Dim listaIds As New List(Of String)

            For Each dr As DataGridViewRow In Me.DataGridView1.Rows
                listaIds.Add(dr.Cells(0).Value.ToString)
            Next

            Me.ProcesarVuelta(Me.mControlador.ProcesarCambioEstadoOperacion(listaIds, CType(Me.cboEstado.SelectedItem, AmvDocumentosDN.EstadosRelacionENFichero)))

            Me.cmdAceptar.Visible = True
            Me.cmdContinuar.Visible = False
            Me.cmdCancelar.Enabled = False
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub


    Private Sub ProcesarVuelta(ByVal pColComandos As AmvDocumentosDN.ColComandoOperacionDN)
        If Not pColComandos Is Nothing Then
            Using New AuxIU.CursorScope(Cursors.WaitCursor)
                Dim colimagen As New DataColumn("Resultado", GetType(Image))
                Dim colmensaje As New DataColumn("Comentario", GetType(String))

                Me.DataGridView1.Columns(Me.DataGridView1.Columns.Count - 1).AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader

                Me.mDatataTable.Columns.Add(colimagen)
                Me.mDatataTable.Columns.Add(colmensaje)

                For Each dr As DataRow In Me.mDatataTable.Rows
                    Dim estoy As Boolean = False
                    Dim soyerror As Boolean = False
                    Dim mierror As String = String.Empty
                    For Each colcom As AmvDocumentosDN.ComandoOperacionDN In pColComandos
                        If colcom.IDRelacion = dr(0).ToString Then
                            estoy = True
                            If (Not colcom.Resultado) Then
                                soyerror = True
                                mierror = colcom.Mensaje
                                Exit For
                            End If
                        End If
                    Next
                    If estoy Then
                        If soyerror Then
                            dr("Resultado") = My.Resources.error_16
                            dr("Comentario") = mierror
                        Else
                            dr("Resultado") = My.Resources.ok_16
                        End If
                    End If
                Next

                Me.DataGridView1.Columns(Me.DataGridView1.Columns.Count - 1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                Me.DataGridView1.Refresh()
            End Using
        End If
    End Sub

    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Try
            Me.Paquete.Clear()
            Me.Close()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdAceptar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmdAceptar.Click
        Try
            Me.Close()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub
#End Region

End Class