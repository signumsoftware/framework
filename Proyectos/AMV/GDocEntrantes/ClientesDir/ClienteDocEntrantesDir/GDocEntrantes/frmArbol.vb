Public Class frmArbol

    Private mControlador As Controladores.frmArbolctrl
    Private mGeneradorArbol As PresentacionArbol

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = Me.Controlador

        Me.mGeneradorArbol = New PresentacionArbol
        Me.ArbolNododeT1.GestorPresentacion = Me.mGeneradorArbol

        Me.ArbolNododeT1.NodoPrincipal = Me.mControlador.RecuperarArbolEntidades.NodoTipoEntNegoio

        Dim mids As DataSet = Me.mControlador.BalizaNumCanalesTipoEntNeg
        Dim miTable As DataTable = Nothing

        If Not mids Is Nothing AndAlso mids.Tables.Count <> 0 Then
            miTable = mids.Tables("vwNumDocPendientesPostClasificacionXTipoEntidadNegocio")
        End If

        Me.mGeneradorArbol.Tabla = miTable
        Me.ArbolNododeT1.NodoPrincipal = Me.ArbolNododeT1.NodoPrincipal
        Me.ArbolNododeT1.ExpandirArbol()

    End Sub

    Private Sub ArbolNododeT1_BeforeSelect(ByRef ElementoSeleccionado As Object, ByVal e As System.Windows.Forms.TreeViewCancelEventArgs) Handles ArbolNododeT1.BeforeSelect
        Try
            If Not TypeOf ElementoSeleccionado Is AmvDocumentosDN.TipoEntNegoioDN Then
                e.Cancel = True
            End If
        Catch ex As Exception
            MostrarError(ex, "Error")
        End Try
    End Sub

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Aceptar.Click
        If Me.ArbolNododeT1.ElementoSeleccionado Is Nothing Then
            MessageBox.Show("Debe seleccionar un elemento del árbol", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Exit Sub
        End If

        Me.Paquete.Add("TipoEntidadNegocio", Me.ArbolNododeT1.ElementoSeleccionado)
        Me.Close()
    End Sub

    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Me.Paquete.Clear()
        Me.Close()
    End Sub

    Private Sub cmdTodos_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdTodos.Click
        Me.Paquete.Add("Todos", True)
        Me.Close()
    End Sub
End Class