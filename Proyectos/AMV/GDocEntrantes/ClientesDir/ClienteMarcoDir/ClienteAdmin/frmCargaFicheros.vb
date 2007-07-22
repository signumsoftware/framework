Public Class frmCargaPagos

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        Dim tipoImportacion As String

        Try
            Me.txtFicheroCarga.Text = ""
            Me.cboOperacion.SelectedItem = Nothing
            Me.cboTipoOrigen.SelectedItem = Nothing
            Me.cboOperacion.Items.Clear()
            Me.cboTipoOrigen.Items.Clear()
            Me.DataGridView1.DataSource = Nothing

            If Me.Paquete.Contains("tipoImportacion") AndAlso Not String.IsNullOrEmpty(Me.Paquete("tipoImportacion").ToString()) Then
                tipoImportacion = Me.Paquete("tipoImportacion").ToString()
                Me.Text = "Importación " & tipoImportacion
            Else
                Throw New ApplicationException("Importación no válida")
            End If

            If tipoImportacion = "Pagos" Then

                'Entorno del formulario
                Me.ToolStripButton1.Visible = False

                Dim lista As IList
                Dim objAS As New Framework.AS.DatosBasicosAS()
                lista = objAS.RecuperarListaTipos(GetType(FN.GestionPagos.DN.TipoEntidadOrigenDN))

                cboTipoOrigen.DisplayMember = "Nombre"
                cboOperacion.DisplayMember = "Nombre"

                For Each tipoEO As FN.GestionPagos.DN.TipoEntidadOrigenDN In lista
                    cboTipoOrigen.Items.Add(tipoEO)
                Next

                Dim colTRI As Framework.Procesos.ProcesosDN.ColTransicionDN
                Dim opAS As New Framework.Procesos.ProcesosAS.OperacionesAS()

                colTRI = opAS.RecuperarTransicionesDeInicio(GetType(FN.GestionPagos.DN.PagoDN))

                For Each tr As Framework.Procesos.ProcesosDN.TransicionDN In colTRI
                    cboOperacion.Items.Add(tr.OperacionDestino)
                Next

            ElseIf tipoImportacion = "Proveedores" Then
                'Entorno del formulario
                Me.ToolStripButton2.Visible = False
                Me.Label1.Visible = False
                Me.Label2.Visible = False
                Me.cboOperacion.Visible = False
                Me.cboTipoOrigen.Visible = False

            Else
                Throw New ApplicationException("Importación no válida")
            End If



        Catch ex As Exception
            MostrarError(ex)
            Close()
        End Try

    End Sub

    Private Sub ToolStripButton1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton1.Click
        Try
            If String.IsNullOrEmpty(txtFicheroCarga.Text) Then
                MessageBox.Show("Debe seleccionar un fichero para la importación", "Importar proveedores", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If

            Dim dts As DataSet

            Dim ad As New AdatadorCargaProveedores()
            dts = ad.ProcesarFichero(Nothing, Me.txtFicheroCarga.Text)

            Me.DataGridView1.DataSource = ""
            Me.DataGridView1.DataSource = dts.Tables(0)

        Catch ex As Exception
            Me.MostrarError(ex)
        End Try
    End Sub

    Private Sub ToolStripButton2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripButton2.Click
        Try
            If cboTipoOrigen.SelectedItem Is Nothing Then
                MessageBox.Show("Debe seleccionar un tipo de origen", "Importar pagos", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If

            If cboOperacion.SelectedItem Is Nothing Then
                MessageBox.Show("Debe seleccionar una operación para los pagos", "Importar pagos", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If

            If String.IsNullOrEmpty(txtFicheroCarga.Text) Then
                MessageBox.Show("Debe seleccionar un fichero para la importación", "Importar pagos", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If

            Dim dts As DataSet

            Dim ad As New AdatadorCargaProveedores()
            dts = ad.CargarPagos(Nothing, Me.txtFicheroCarga.Text, cboTipoOrigen.SelectedItem, cboOperacion.SelectedItem)

            Me.DataGridView1.DataSource = ""
            Me.DataGridView1.DataSource = dts.Tables(0)

        Catch ex As Exception
            Me.MostrarError(ex)
        End Try
    End Sub

    Private Sub cmdExaminarFichero_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdExaminarFichero.Click
        Try
            If Me.OpenFileDialog1.ShowDialog() = Windows.Forms.DialogResult.OK Then
                Me.txtFicheroCarga.Text = Me.OpenFileDialog1.FileName
            End If
        Catch ex As Exception
            Me.MostrarError(ex)
        End Try
    End Sub

    Private Sub ToolStripRefrescar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripRefrescar.Click
        Try
            Me.Inicializar()
        Catch ex As Exception
            Me.MostrarError(ex)
        End Try
    End Sub
End Class


Public Class AdatadorCargaProveedores

    Public Function CargarPagos(ByVal GestorModificacionOAlta As Object, ByVal pRuta As String, ByVal tipoOrigen As FN.GestionPagos.DN.TipoEntidadOrigenDN, ByVal operacion As Framework.Procesos.ProcesosDN.OperacionDN) As DataSet

        Dim dts As New FN.GestionPagos.DN.dtsGestionPagos

        Dim str As New IO.StreamReader(pRuta, System.Text.Encoding.Default)

        ' avanza hasta encontrar la linea de separacion
        Dim a As Int16
        For a = 0 To 7
            If Not str.EndOfStream Then
                Dim linea As String = str.ReadLine()
            End If
        Next

        ' la siguiente linea si es la de proveedores la pasamos sino procedemos a la carga

        Do Until str.EndOfStream
            Dim linea1, lineanula As String
            linea1 = str.ReadLine()
            lineanula = str.ReadLine()
            Dim CodSiniestro, importe, cifBeneficiario, NombreBeneficiario As String

            Dim valores() As String = linea1.Split(ControlChars.Tab)

            CodSiniestro = valores(2)
            importe = valores(3)
            cifBeneficiario = valores(6)
            NombreBeneficiario = valores(9)
            ' añadir la fila al dataset

            dts.PagosConCheque.AddPagosConChequeRow(CodSiniestro, importe, cifBeneficiario, NombreBeneficiario)

        Loop

        Dim dtsresultados As Data.DataSet
        Dim lnc As New FN.GestionPagos.LNC.PagosLNC

        dtsresultados = lnc.CargarPagos(dts, tipoOrigen, operacion)

        ' hacer un vinculo del dts con el de resultados

        Return dtsresultados


    End Function


    Public Function ProcesarFichero(ByVal GestorModificacionOAlta As Object, ByVal pRuta As String) As DataSet




        Dim dts As New FN.GestionPagos.DN.dtsGestionPagos
        Dim str As New IO.StreamReader(pRuta, System.Text.Encoding.Default)



        ' avanza hasta encontrar la linea de separacion
        Do Until str.EndOfStream
            Dim linea As String = str.ReadLine()
            If linea.Contains(" ------------ ") Then
                Exit Do
            End If

        Loop



        ' la siguiente linea si es la de proveedores la pasamos sino procedemos a la carga





        Do Until str.EndOfStream
            Dim linea1, linea2 As String
            linea1 = str.ReadLine()
            linea2 = str.ReadLine()

            Dim cuentaContable, NombreEmpresa, Domicilio, idFiscarl, codp, localidad, provincia, telefono, churro As String

            cuentaContable = linea1.Substring(5, 12).Trim


            Dim posicionesl1, nposicionesl2 As Integer


            NombreEmpresa = linea1.Substring(5 + 12 + 1, 30)

            If Not String.IsNullOrEmpty(linea2) Then
                NombreEmpresa += linea2.Substring(5 + 12 + 1, 30)
                codp = linea2.Substring(5 + 12 + 1 + 30, 6)
                localidad = linea2.Substring(5 + 12 + 1 + 30 + 6 + 1, 25).Trim
                provincia = linea2.Substring(5 + 12 + 1 + 30 + 6 + 1 + 25 + 1, 21).Trim
                telefono = linea2.Substring(5 + 12 + 1 + 30 + 6 + 1 + 25 + 1 + 21)

            End If


            NombreEmpresa = NombreEmpresa.Trim


            churro = linea1.Substring(5 + 12 + 1 + 30)

            If churro.Split("-").LongLength > 1 Then
                Domicilio = churro.Split("-")(0).Trim
                idFiscarl = churro.Split("-")(1).Trim
                dts.EntidadesFiscales.AddEntidadesFiscalesRow(cuentaContable, NombreEmpresa, Domicilio, idFiscarl, codp, localidad, provincia, telefono)
            End If


        Loop



        '  Dim dts As New DataSet
        'Dim ttb As New DataTable("Resultados")

        'dts.Tables.Add(ttb)

        'ttb.Columns.Add(New DataColumn("cuentaContable"))
        'ttb.Columns.Add(New DataColumn("idFiscal"))
        'ttb.Columns.Add(New DataColumn("idSistema"))
        'ttb.Columns.Add(New DataColumn("Resultado"))
        'ttb.Columns.Add(New DataColumn("Mensaje"))




        Dim dtsresultados As Data.DataSet
        Dim lnc As New FN.GestionPagos.LNC.PagosLNC

        dtsresultados = lnc.AltaModificacionProveedores(dts)


        ' hacer un vinculo del dts con el de resultados





        Return dtsresultados


    End Function

End Class
