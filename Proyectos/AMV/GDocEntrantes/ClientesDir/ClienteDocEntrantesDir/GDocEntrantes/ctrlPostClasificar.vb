Imports Framework.Usuarios.DN
Imports Framework.IU.IUComun
Imports AuxIU

Public Class ctrlPostClasificar

#Region "atributos"
    Private mControlador As Controladores.ctrlClasificarctrl

    'la operacion que estamos tratando
    Private mOperacionEnCurso As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
    'la tabla de entidades de negocio
    Private mTablaEntidades As New DataTable
    'generador del arbol
    Private mGeneradorArbol As PresentacionArbol

#End Region

#Region "inicializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        Me.mControlador = New Controladores.ctrlClasificarctrl(Me.Marco, Me)

        'cargamos los tipos de entidades en el árbol
        Dim operador As AmvDocumentosDN.OperadorDN
        Dim principal As PrincipalDN
        principal = Me.Marco.DatosMarco("Principal")
        operador = principal.UsuarioDN.HuellaEntidadUserDN.EntidadReferida

        Dim cabecera As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        cabecera = Me.mControlador.RecuperarArbolEntidades
        cabecera.NodoTipoEntNegoio.PodarNodosHijosNoContenedoresHojas(operador.ColTipoEntNegoio, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
        Me.ArbolNododeT1.NodoPrincipal = cabecera.NodoTipoEntNegoio


        'deshabilitamos los botones
        LimpiarEstadoBotones()

        'rellenamos el combo
        'Me.cboTipoDocs.Items.Add("nuevos documentos")
        Me.cboTipoDocs.Items.Add("ya clasificados")

        Me.cboTipoDocs.SelectedIndex = 0

        CargarColTipoFichero()
        'asignamos la presentación del arbol con numeros
        Me.mGeneradorArbol = New PresentacionArbol
        Me.ArbolNododeT1.GestorPresentacion = Me.mGeneradorArbol

        Me.CrearTablaEntidades()

    End Sub

#End Region

#Region "propiedades"
    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property OperacionEnRelacionENFichero() As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Get
            Return Me.mOperacionEnCurso
        End Get
        Set(ByVal value As AmvDocumentosDN.OperacionEnRelacionENFicheroDN)

            mOperacionEnCurso = value
            'mostramos u ocultamos los botones según tengamos algo o no
            LimpiarEstadoBotones()

            If Not mOperacionEnCurso Is Nothing Then

                RaiseEvent OperacionRecuperada()

                'If (Not mOperacionEnCurso.RelacionENFichero Is Nothing) AndAlso (Not mOperacionEnCurso.RelacionENFichero.EntidadNegocio Is Nothing) Then
                '    Me.ArbolNododeT1.ElementoSeleccionado = mOperacionEnCurso.RelacionENFichero.EntidadNegocio.TipoEntNegocioReferidora

                '    'apuntamos en la tabla de entidades
                '    Me.AgregarTabla(Me.mOperacionEnCurso.RelacionENFichero.EntidadNegocio)

                '    'ponemos la ruta en el label de documento cargado
                '    Me.lblDocumentoCargado.Text = Me.mOperacionEnCurso.RelacionENFichero.HuellaFichero.NombreOriginalFichero

                '    'ponemos el comentario
                '    Me.txtvComentarioOperacion.Text = mOperacionEnCurso.ComentarioOperacion

                '    'ponemos el foco en el dgridEntidades
                '    Me.dgvEntidades.Focus()
                'Else
                '    Me.ArbolNododeT1.Focus()
                'End If

            Else
                MessageBox.Show("No hay ninguna operación a procesar", "Recuperar", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        End Set
    End Property

#End Region

#Region "métodos"


#Region "hilo"
    ''' <summary>
    ''' HILO - recibe el dataset y dibuja los números en los controles
    ''' </summary>
    Public Sub RefrescarNumerosCanalesYArbol(ByVal pDS As DataSet)
        Me.RefrescarNumArbol(pDS.Tables("vwNumDocPendientesPostClasificacionXTipoEntidadNegocio"))
    End Sub

    ''' <summary>
    ''' Método al que llama el hilo para refrescar el número de docs pendientes en cada elemento del árbol
    ''' </summary>
    ''' <param name="pTable"></param>
    ''' <remarks></remarks>
    Private Sub RefrescarNumArbol(ByVal pTable As DataTable)
        'columnas del datatable:
        'ID()
        'Nombre()
        'Num() 
        Me.mGeneradorArbol.Tabla = pTable
        Me.ArbolNododeT1.NodoPrincipal = Me.ArbolNododeT1.NodoPrincipal
        Me.ArbolNododeT1.ExpandirArbol()
    End Sub
#End Region

    Public Event ActualizarHistorial(ByVal Imagen As Image, ByVal Ruta As String, ByVal Accion As String, ByVal Hora As String)


    ''' <summary>
    ''' Agregamos una fila en la lista del historial con los datos de la última operación
    ''' </summary>
    ''' <param name="pOperacion">La operación que queremos agregar</param>
    Private Sub ActualizarTablaHistorial(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN)
        Using New CursorScope(Cursors.WaitCursor)
            Dim accion As String = String.Empty
            Dim imagen As Image = Nothing
            Select Case pOperacion.TipoOperacionREnF.Valor
                Case AmvDocumentosDN.TipoOperacionREnF.Anular
                    imagen = My.Resources.eliminar_documento16
                    accion = "anulado"
                Case AmvDocumentosDN.TipoOperacionREnF.Clasificar
                    imagen = My.Resources.documento_out_16
                    accion = "modificado"
                Case AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar
                    imagen = My.Resources.documento_ok_16
                    accion = "modificado y cerrado"
                Case AmvDocumentosDN.TipoOperacionREnF.Incidentar
                    imagen = My.Resources.documento_warning_16
                    accion = "incidentado"
                Case AmvDocumentosDN.TipoOperacionREnF.Rechazar
                    imagen = My.Resources.documento_stop_16
                    accion = "rechazado"
            End Select

            RaiseEvent ActualizarHistorial(imagen, pOperacion.RelacionENFichero.HuellaFichero.RutaAbsoluta, accion, Now.ToShortTimeString)

        End Using
    End Sub

    Private Sub LimpiarEstadoBotones()
        Me.mTablaEntidades.Clear()
        Me.ArbolNododeT1.ElementoSeleccionado = Nothing
        Me.txtvComentarioOperacion.Text = String.Empty
        Dim tengooperacion As Boolean = Not Me.mOperacionEnCurso Is Nothing
        Me.cmdAbrir.Enabled = tengooperacion
        Me.cmdAceptarYCerrarOperacion.Enabled = tengooperacion
        Me.cmdAnularOperacion.Enabled = tengooperacion
        Me.cmdIncidentarOperacion.Enabled = tengooperacion
        Me.cmdRechazarOperacion.Enabled = tengooperacion
        Me.cmdCopiarID.Enabled = tengooperacion
        Me.cmdCopiarRuta.Enabled = tengooperacion
        Me.cmdAceptarOperacion.Enabled = tengooperacion
        Me.cmdRecuperarSiguteOperacion.Enabled = (Not tengooperacion)
        Me.Panel1.Enabled = (Not tengooperacion)
        '  Me.ArbolNododeT1.Enabled = (tengooperacion)
    End Sub

    ''' <summary>
    ''' Indica que este control tiene una operación con la que va a trabajar
    ''' </summary>
    Public Event OperacionRecuperada()
    ''' <summary>
    ''' Indica que este control ya ha terminado con la operación que tenía
    ''' </summary>
    Public Event OperacionLiberada()



    Private Sub RecuperarSiguienteOperacion()
        Using New CursorScope(Cursors.WaitCursor)
            Dim miTipoEntNegocio As AmvDocumentosDN.TipoEntNegoioDN = Nothing
            If Not Me.lblBusquedaArbol.Tag Is Nothing Then
                miTipoEntNegocio = CType(Me.lblBusquedaArbol.Tag, AmvDocumentosDN.TipoEntNegoioDN)
            End If
            OperacionEnRelacionENFichero = Me.mControlador.RecuperarSiguienteOperacionAProcesarPostClasificados(miTipoEntNegocio)

        End Using
    End Sub

    Private Sub cmdRecuperarSiguteOperacion_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdRecuperarSiguteOperacion.Click
        Try
            RecuperarSiguienteOperacion()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub lblDocumentoCargado_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles lblDocumentoCargado.TextChanged
        Try
            '     Me.ToolTip.SetToolTip(Me.lblDocumentoCargado, Me.lblDocumentoCargado.Text)
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub


#Region "Métodos Aceptar"

    Private Sub ClasificarYCerraOperacion()
        Dim tipoOperacionActual As AmvDocumentosDN.TipoOperacionREnFDN = Me.mOperacionEnCurso.TipoOperacionREnF

        Try
            If MessageBox.Show("Va a cerrar una operación ¿Está seguro?", "Cerrar operación", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) = DialogResult.OK Then
                AsignarValores()

                Me.mOperacionEnCurso.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar)

                If Me.mTablaEntidades.Rows.Count <> 0 Then
                    Dim mensaje As String = String.Empty
                    Dim micolent As AmvDocumentosDN.ColEntNegocioDN = Me.EntidadesNegocio(mensaje)
                    If micolent Is Nothing OrElse micolent.Count = 0 Then
                        Advertencia("Aceptar", mensaje)
                        Me.mOperacionEnCurso.TipoOperacionREnF = tipoOperacionActual
                        Exit Sub
                    End If
                    '  Me.mOperacionEnCurso.RelacionENFichero.EntidadNegocio = micolent(0)
                Else
                    Advertencia("Aceptar", "Debe establecer la categoría a la que pertenece")
                    Me.mOperacionEnCurso.TipoOperacionREnF = tipoOperacionActual
                    Exit Sub
                End If

                Me.mControlador.ClasificarYCerraOperacion(Me.mOperacionEnCurso)
                LimpiarOperacion()

            End If

        Catch ex As Exception
            Me.mOperacionEnCurso.TipoOperacionREnF = tipoOperacionActual
            MessageBox.Show("Se ha producido un error: " & ex.Message, "Clasificar y cerrar", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub

    Private Sub cmdAceptarYCerrarOperacion_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAceptarYCerrarOperacion.Click
        Try
            ClasificarYCerraOperacion()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdIncidentarOperacion_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdIncidentarOperacion.Click
        Try
            If Me.txtvComentarioOperacion.Text.Trim() = String.Empty Then
                Advertencia(sender, "Debe explicar el motivo de la incidencia en el Comentario")
                Exit Sub
            End If
            If MessageBox.Show("Va a Incidentar una operación" & Chr(13) & Chr(13) & "¿Está seguro?", "Incidentar", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) = DialogResult.OK Then
                AsignarValores()
                Me.mOperacionEnCurso.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.Incidentar)
                Me.mControlador.IncidentarOperacion(Me.mOperacionEnCurso)
                LimpiarOperacion()
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdAnularOperacion_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAnularOperacion.Click
        Try
            If Me.txtvComentarioOperacion.Text.Trim() = String.Empty Then
                Me.Advertencia(sender, "Debe explicar el motivo de la anulación en el Comentario")
                Exit Sub
            End If
            If MessageBox.Show("Va a Anular una operación" & Chr(13) & Chr(13) & "¿Está seguro?", "Incidentar", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) = DialogResult.OK Then
                AsignarValores()
                Me.mOperacionEnCurso.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.Anular)
                Me.mControlador.AnularOperacion(Me.mOperacionEnCurso)
                Me.LimpiarOperacion()
            End If
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdRechazarOperacion_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdRechazarOperacion.Click
        Try
            If Me.txtvComentarioOperacion.Text.Trim() = String.Empty Then
                Advertencia(sender, "Debe explicar el motivo del rechazo en el Comentario")
                Exit Sub
            End If
            If MessageBox.Show("Va a Rechazar una operación" & Chr(13) & Chr(13) & "¿Está seguro?", "Incidentar", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) = DialogResult.OK Then
                AsignarValores()
                Me.mOperacionEnCurso.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.Rechazar)
                Me.mControlador.RechazarOperacion(Me.mOperacionEnCurso)
                LimpiarOperacion()
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    ''' <summary>
    ''' Lanza una advertencia en un messagebox
    ''' </summary>
    ''' <param name="mensaje">El cuerpo del mensaje</param>
    ''' <param name="sender">El control sobre el que se genera la advertencia</param>
    Public Overloads Sub Advertencia(ByVal sender As Control, ByVal mensaje As String)
        MessageBox.Show(mensaje, sender.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
    End Sub

    Public Overloads Sub Advertencia(ByVal titulo As String, ByVal mensaje As String)
        MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
    End Sub

    Private Sub LimpiarOperacion()
        Using New CursorScope(Cursors.WaitCursor)
            'actualizamos el historial
            Me.ActualizarTablaHistorial(Me.mOperacionEnCurso)
            'limpiamos los datos y lo dejamos todo vacío
            Me.lblDocumentoCargado.Text = "Ninguno"
            Me.mOperacionEnCurso = Nothing
            Me.LimpiarEstadoBotones()
            RaiseEvent OperacionLiberada()
        End Using
    End Sub

    ''' <summary>
    ''' Asigna los valores a la operación: sólo el comentario
    ''' </summary>
    ''' <returns>True si la operación está modificada al asignar los valores, False si no se ha
    ''' modificado nada</returns>
    Private Function AsignarValores() As Boolean

        Me.mOperacionEnCurso.ComentarioOperacion = Me.txtvComentarioOperacion.Text.Trim()

        Return (Me.mOperacionEnCurso.Estado = Framework.DatosNegocio.EstadoDatosDN.Modificado)
    End Function

    Private Function EntidadesNegocio(ByRef Mensaje As String) As AmvDocumentosDN.ColEntNegocioDN
        Dim micolentnegocios As AmvDocumentosDN.ColEntNegocioDN = Nothing
        Dim huboerror As Integer = 0

        If Me.mTablaEntidades.Rows.Count <> 0 Then
            For Each mir As DataRow In Me.mTablaEntidades.Rows
                micolentnegocios = New AmvDocumentosDN.ColEntNegocioDN()
                Dim miid As String = String.Empty
                Dim micomentario As String = String.Empty

                If mir("IDEntidad") Is DBNull.Value OrElse CType(mir("IDEntidad"), String).Trim() = String.Empty Then
                    Mensaje = "Debe rellenar el ID de todas las Entidades"
                    huboerror += 1
                    Exit For
                Else
                    miid = CType(mir("IDEntidad"), String).Trim()
                End If

                If CType(String.Empty & mir("Comentario"), String).Trim() <> String.Empty Then
                    micomentario = CType(mir("Comentario"), String).Trim()
                End If

                Dim mient As New AmvDocumentosDN.EntNegocioDN()
                mient.TipoEntNegocioReferidora = CType(mir("TipoEntidad"), AmvDocumentosDN.TipoEntNegoioDN)
                mient.IdEntNeg = miid
                mient.Comentario = micomentario

                micolentnegocios.Add(mient)

            Next
        Else
            Mensaje = "Debe asignar al menos una Categoría al Documento"
            huboerror += 1
        End If

        If huboerror = 0 Then
            Return micolentnegocios
        Else
            Return Nothing
        End If

    End Function



#Region "Copiar y Abrir"

    Private Sub CopiarID()
        If Me.dgvEntidades.Rows.Count <> 0 Then
            If Me.dgvEntidades.SelectedRows.Count <> 0 Then
                Clipboard.Clear()
                Dim mir As DataRow = Me.mTablaEntidades.Rows(Me.dgvEntidades.SelectedRows(0).Index)
                Clipboard.SetText(mir("IDEntidad"))
            ElseIf Me.dgvEntidades.SelectedCells.Count <> 0 Then
                Clipboard.Clear()
                Dim mir As DataRow = Me.mTablaEntidades.Rows(Me.dgvEntidades.SelectedCells(0).RowIndex)
                Clipboard.SetText(mir("IDEntidad"))
            Else
                Advertencia("Copiar ID", "Debe seleccionar una de las entidades")
            End If
        End If
    End Sub

    Private Sub cmdCopiarID_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCopiarID.Click
        Try
            CopiarID()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub CopiarRuta()
        Clipboard.Clear()
        Clipboard.SetText(Me.mOperacionEnCurso.RelacionENFichero.HuellaFichero.RutaAbsoluta)

    End Sub

    Private Sub cmdCopiarRuta_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCopiarRuta.Click
        Try
            CopiarRuta()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub AbrirArchivo()
        Using New CursorScope(Cursors.WaitCursor)
            Dim pr As Process = System.Diagnostics.Process.Start(Me.mOperacionEnCurso.RelacionENFichero.HuellaFichero.RutaAbsoluta)
        End Using

    End Sub

    Private Sub cmdAbrir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAbrir.Click
        Try
            AbrirArchivo()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

#End Region

#End Region

#Region "selección del árbol"
    Private Sub ArbolNododeT1_BeforeSelect(ByRef ElementoSeleccionado As Object, ByVal e As System.Windows.Forms.TreeViewCancelEventArgs) Handles ArbolNododeT1.BeforeSelect
        Try
            'sólo dejamos que seleccione una hoja
            If Not TypeOf ElementoSeleccionado Is AmvDocumentosDN.TipoEntNegoioDN Then
                e.Cancel = True
                Me.ArbolNododeT1.ElementoSeleccionado = Nothing
            End If
        Catch ex As Exception
            MostrarError(ex, "Error al seleccionar")
        End Try
    End Sub
#End Region

#Region "reconocedor eventos teclado"
    Public Sub RecogerTeclaAbreviada(ByVal e As KeyEventArgs)
        Select Case e.KeyCode
            Case Keys.F1
                'Recuperar sigute.
                If Me.mOperacionEnCurso Is Nothing Then
                    RecuperarSiguienteOperacion()
                End If
            Case Keys.F2
                'Copiar ID
                If Not Me.mOperacionEnCurso Is Nothing Then
                    Me.dgvEntidades.EndEdit()
                    CopiarID()
                End If
            Case Keys.F3
                'Copiar Ruta
                If Not Me.mOperacionEnCurso Is Nothing Then
                    Me.dgvEntidades.EndEdit()
                    CopiarRuta()
                End If
            Case Keys.F4
                'Abrir archivo
                If Not Me.mOperacionEnCurso Is Nothing Then
                    AbrirArchivo()
                End If
            Case Keys.F5
                'Aceptar
                If Not Me.mOperacionEnCurso Is Nothing Then
                    Me.dgvEntidades.EndEdit()
                    ClasificarOperacion()
                End If

            Case Keys.F6
                'Cerrar

                If Not Me.mOperacionEnCurso Is Nothing AndAlso Me.cmdAceptarYCerrarOperacion.Visible Then
                    Me.dgvEntidades.EndEdit()
                    ClasificarYCerraOperacion()
                End If



        End Select
    End Sub
#End Region

#End Region


#Region "Lanzar Elemento"
    Private Sub ArbolNododeT1_OnElementoLanzado(ByRef pElemnto As Object) Handles ArbolNododeT1.OnElementoLanzado
        Try
            If Not Me.mOperacionEnCurso Is Nothing Then
                If Not pElemnto Is Nothing AndAlso (TypeOf pElemnto Is AmvDocumentosDN.TipoEntNegoioDN) Then
                    AgregarTabla(CType(pElemnto, AmvDocumentosDN.TipoEntNegoioDN))
                End If
            End If
        Catch ex As Exception
            MostrarError(ex, "Error")
        End Try
    End Sub

#End Region

#Region "TablaEntidadesNegocio"

    Private Sub CargarColTipoFichero()
        Dim miColTipoFicheroDN As Framework.Ficheros.FicherosDN.ColTipoFicheroDN = Me.mControlador.RecuperarColTipoFichero
        If Not miColTipoFicheroDN Is Nothing Then
            Me.ComboBox1.DataSource = Nothing
            Me.ComboBox1.DisplayMember = "Nombre"
            Me.ComboBox1.DataSource = miColTipoFicheroDN

        End If

        Me.ComboBox1.Refresh()
    End Sub

    Private Sub CrearTablaEntidades()
        Me.mTablaEntidades = New DataTable

        Dim coltipoentidad As New DataColumn("TipoEntidad", GetType(AmvDocumentosDN.TipoEntNegoioDN))
        coltipoentidad.Caption = "Tipo Entidad"
        coltipoentidad.ReadOnly = True
        Me.mTablaEntidades.Columns.Add(coltipoentidad)

        'Dim colid As New DataColumn("IDEntidad", GetType(String))
        'colid.Caption = "ID Entidad Ref."
        'Me.mTablaEntidades.Columns.Add(colid)

        'Dim colcoment As New DataColumn("Comentario", GetType(String))
        'colcoment.Caption = "Comentario"
        'Me.mTablaEntidades.Columns.Add(colcoment)

        Me.dgvEntidades.DataSource = Me.mTablaEntidades
        Me.dgvEntidades.Columns(0).Name = "Categoría"
        'Me.dgvEntidades.Columns(1).Name = "ID Entidad Ref"
        'Me.dgvEntidades.Columns(2).Name = "Comentario"
        Me.dgvEntidades.Columns(0).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        Me.dgvEntidades.Refresh()
    End Sub

    Private Overloads Sub AgregarTabla(ByVal pTipoEntidad As AmvDocumentosDN.TipoEntNegoioDN)
        '1º limpiamos la tabla, pq en este control sólo se puede trabajar con una operación
        'a la vez
        Me.mTablaEntidades.Clear()

        Dim mir As DataRow = Me.mTablaEntidades.NewRow

        mir("TipoEntidad") = pTipoEntidad

        Me.mTablaEntidades.Rows.Add(mir)
        Me.dgvEntidades.Refresh()
    End Sub

    Private Overloads Sub AgregarTabla(ByVal pEntidad As AmvDocumentosDN.EntNegocioDN)
        Me.mTablaEntidades.Clear()

        Dim mir As DataRow = Me.mTablaEntidades.NewRow

        mir("TipoEntidad") = pEntidad.TipoEntNegocioReferidora
        mir("IDEntidad") = pEntidad.IdEntNeg
        mir("Comentario") = pEntidad.Comentario

        Me.mTablaEntidades.Rows.Add(mir)
        Me.dgvEntidades.Refresh()
    End Sub

    Private Sub cmdEliminarEntidad_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdEliminarEntidad.Click
        Try
            If Me.dgvEntidades.SelectedRows.Count <> 0 Then
                Me.mTablaEntidades.Rows.RemoveAt(Me.dgvEntidades.SelectedRows(0).Index)
            ElseIf Me.dgvEntidades.SelectedCells.Count <> 0 Then
                Me.dgvEntidades.Rows.RemoveAt(Me.dgvEntidades.SelectedCells(0).RowIndex)
            Else
                Advertencia(sender, "Debe seleccionar una fila")
            End If
        Catch ex As Exception
            MostrarError(ex, "Borrar")
        End Try
    End Sub

#End Region

#Region "Búsqueda X TipoEntNegocio"
    Private Sub lblBusquedaArbol_DragDrop(ByVal sender As Object, ByVal e As System.Windows.Forms.DragEventArgs) Handles lblBusquedaArbol.DragDrop
        Try
            If e.Effect = DragDropEffects.Copy Then
                Dim mitipoentneg As AmvDocumentosDN.TipoEntNegoioDN = CType(CType(e.Data, TreeNode).Tag, AmvDocumentosDN.TipoEntNegoioDN)
                Me.lblBusquedaArbol.Tag = mitipoentneg
                Me.lblBusquedaArbol.Text = mitipoentneg.Nombre
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub lblBusquedaArbol_DragEnter(ByVal sender As Object, ByVal e As System.Windows.Forms.DragEventArgs) Handles lblBusquedaArbol.DragEnter
        Try
            If TypeOf e.Data Is TreeNode AndAlso TypeOf CType(e.Data, TreeNode).Tag Is AmvDocumentosDN.TipoEntNegoioDN Then
                e.Effect = DragDropEffects.Copy
            Else
                e.Effect = DragDropEffects.None
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdSeleccionarArbol_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdSeleccionarArbol.Click
        Try
            Dim mipaquete As New Hashtable
            Me.Marco.Navegar("SeleccionArbol", Me.ParentForm, Nothing, TipoNavegacion.Modal, Me.mControlador.ControladorForm.FormularioContenedor.GenerarDatosCarga, mipaquete)
            If Not mipaquete Is Nothing Then
                If mipaquete.Contains("TipoEntidadNegocio") AndAlso Not mipaquete("TipoEntidadNegocio") Is Nothing Then
                    Dim mientnegocio As AmvDocumentosDN.TipoEntNegoioDN = CType(mipaquete("TipoEntidadNegocio"), AmvDocumentosDN.TipoEntNegoioDN)
                    Me.lblBusquedaArbol.Tag = mientnegocio
                    Me.lblBusquedaArbol.Text = mientnegocio.Nombre
                ElseIf mipaquete.Contains("Todos") AndAlso mipaquete("Todos") = True Then
                    Me.lblBusquedaArbol.Text = "Todos"
                    Me.lblBusquedaArbol.Tag = Nothing
                End If
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub


#End Region


    Private Sub cmdAceptarOperacion_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAceptarOperacion.Click

        Try
            ClasificarOperacion()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub ClasificarOperacion()
        Dim mimensaje As String = String.Empty

        Dim micolent As AmvDocumentosDN.ColEntNegocioDN = Me.EntidadesNegocio(mimensaje)
        If micolent Is Nothing OrElse micolent.Count = 0 Then
            Advertencia("Aceptar", mimensaje)
            Exit Sub
        End If

        'ponemos el comentario
        AsignarValores()

        'llamamos al controlador
        Me.mControlador.ClasificarOperacion(Me.mOperacionEnCurso, micolent)

        LimpiarOperacion()
    End Sub

End Class
