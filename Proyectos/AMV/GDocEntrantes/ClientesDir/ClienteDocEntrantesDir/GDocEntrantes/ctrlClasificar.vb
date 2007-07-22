Imports Framework.Usuarios.DN
Imports AuxIU

Public Class ctrlClasificar

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


        Dim miprincipal As Framework.Usuarios.DN.PrincipalDN = Me.Marco.DatosMarco("Principal")
        Dim operador As AmvDocumentosDN.OperadorDN
        operador = miprincipal.UsuarioDN.HuellaEntidadUserDN.EntidadReferida
        ''cargamos los tipos de entidades en el árbol
        'Me.ArbolNododeT1.NodoPrincipal = Me.mControlador.RecuperarArbolEntidades.NodoTipoEntNegoio



        'If miprincipal.IsInRole("Operador Entrada") Then
        '    Me.ArbolNododeT1.NodoPrincipal = Me.mControlador.RecuperarArbolEntidades.NodoTipoEntNegoio
        'Else
        '    Dim cabecera As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        '    cabecera = Me.mControlador.RecuperarArbolEntidades
        '    cabecera.NodoTipoEntNegoio.PodarNodosHijosNoContenedoresHojas(operador.ColTipoEntNegoio, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
        '    Me.ArbolNododeT1.NodoPrincipal = cabecera.NodoTipoEntNegoio
        'End If


        'deshabilitamos los botones
        LimpiarEstadoBotones()

        'rellenamos el combo
        ' Me.cboTipoDocs.Items.Add("nuevos documentos")
        'Me.cboTipoDocs.Items.Add("ya clasificados")

        ' Me.cboTipoDocs.SelectedIndex = 0




        Dim tbcanales, tpTipoEntNeg As System.Windows.Forms.TabPage
        tbcanales = Me.SelectorCargaTab.TabPages(0)
        tpTipoEntNeg = Me.SelectorCargaTab.TabPages(1)

        If Not miprincipal.IsInRole("Operador Cierre") Then
            Me.SelectorCargaTab.TabPages.Remove(tpTipoEntNeg)
        End If

        If Not miprincipal.IsInRole("Operador Entrada") Then
            Me.SelectorCargaTab.TabPages.Remove(tbcanales)
        End If

        If Me.SelectorCargaTab.TabPages.Count > 0 Then
            Me.SelectorCargaTab.TabPages(0).Select()
        End If

        'rellenamos la tabla de canales
        Me.lstCanales.DisplayMember = "ToString"
        Me.CargarCanales()
        CargarColTipoFichero()

        'asignamos la presentación del arbol con numeros
        Me.mGeneradorArbol = New PresentacionArbol
        Me.ArbolNododeT1.GestorPresentacion = Me.mGeneradorArbol

        'creamos la tabla entidad
        Me.CrearTablaEntidades()

        'deshabilitamos el listbox
        Me.lstCanales.Enabled = False

        ' restricciones de uso por perfil
        AutorizacionCerrar()

    End Sub


    Private Function AutorizacionCerrar() As Boolean
        Dim actor As PrincipalDN

        actor = Me.mControlador.Marco.DatosMarco.Item("Principal")

        If actor.IsInRole("Operador Cierre") Then
            Return True
        Else
            Return False
        End If

    End Function


    Private Sub CargarCanales()
        Dim pColtipoCanales As AmvDocumentosDN.ColTipoCanalDN = Me.mControlador.RecuperarColTipoCanales()
        If Not pColtipoCanales Is Nothing Then
            For Each mitipocanal As AmvDocumentosDN.TipoCanalDN In pColtipoCanales
                Dim micanaliu As New CanalIU
                micanaliu.TipoCanal = mitipocanal
                Me.lstCanales.Items.Add(micanaliu)
            Next
        End If

        Me.lstCanales.Refresh()
    End Sub




    Private Sub CargarColTipoFichero()
        Dim miColTipoFicheroDN As Framework.Ficheros.FicherosDN.ColTipoFicheroDN = Me.mControlador.RecuperarColTipoFichero
        If Not miColTipoFicheroDN Is Nothing Then
            Me.ComboBox1.DataSource = Nothing
            Me.ComboBox1.DisplayMember = "Nombre"
            Me.ComboBox1.DataSource = miColTipoFicheroDN

        End If

        Me.ComboBox1.Refresh()
    End Sub

#End Region



#Region "métodos"


#Region "hilo"
    ''' <summary>
    ''' HILO - recibe el dataset y dibuja los números en los controles
    ''' </summary>
    Public Sub RefrescarNumerosCanalesYArbol(ByVal pDS As DataSet)
        Me.RefrescarNumArbol(pDS.Tables("vwNumDocPendientesPostClasificacionXTipoEntidadNegocio"))
        Me.RefrescarNumCanales(pDS.Tables("vwNumDocPendientesClasificacionXTipoCanal"))
    End Sub

    ''' <summary>
    ''' Método al que debe llamar el hilo para refrescar el número de docs pendientes por canal
    ''' </summary>
    ''' <param name="pTable">La tabla del dataset que contiene la relación de tipocanal-nº</param>
    Private Sub RefrescarNumCanales(ByVal pTable As DataTable)
        'columnas del datatable:
        'ID()
        'Nombre()
        'Num() 

        Dim indiceElementoSelecionado As Integer
        indiceElementoSelecionado = lstCanales.SelectedIndex


        'recojemos la colcanales y además losponeoms todos a 0 
        'pq el WS nos devuelve números sólo en los que la cantidad<>0
        Dim micolcanalesiu As New List(Of CanalIU)
        For Each mic As CanalIU In Me.lstCanales.Items
            mic.Numero = 0
            micolcanalesiu.Add(mic)
        Next

        Me.lstCanales.Items.Clear()

        If Not pTable Is Nothing Then
            For Each mir As DataRow In pTable.Rows
                Dim canalcoincidente As CanalIU = Nothing
                For Each miCanalIU As CanalIU In micolcanalesiu

                    If miCanalIU.TipoCanal.ID = CType(mir("ID"), String) Then
                        canalcoincidente = miCanalIU
                        Exit For
                    End If
                    Application.DoEvents()
                Next
                canalcoincidente.Numero = mir("Num")
                Application.DoEvents()
            Next
        End If

        Me.lstCanales.Items.AddRange(micolcanalesiu.ToArray)


        If lstCanales.Items.Count > indiceElementoSelecionado Then
            lstCanales.SelectedIndex = indiceElementoSelecionado
        End If


        Me.lstCanales.Refresh()

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
        Me.cmdAceptarOperacion.Enabled = tengooperacion
        Me.cmdAnularOperacion.Enabled = tengooperacion
        Me.cmdIncidentarOperacion.Enabled = tengooperacion
        Me.cmdRechazarOperacion.Enabled = tengooperacion
        Me.cmdCopiarID.Enabled = tengooperacion
        Me.cmdCopiarRuta.Enabled = tengooperacion
        Me.cmdAceptarYCerrar.Enabled = tengooperacion AndAlso AutorizacionCerrar()
        Me.cmdRecuperarSiguteOperacion.Enabled = (Not tengooperacion)
        Me.SplitContainer1.Panel1.Enabled = (Not tengooperacion)
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


#Region "establecer y rellenar datos"
    Protected Overrides Sub DNaIU(ByVal pDN As Object)
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

            '    Me.dgvEntidades.Focus()
            'Else
            '    Me.ArbolNododeT1.Focus()
            'End If






            If (Not mOperacionEnCurso.RelacionENFichero Is Nothing) Then
                If (Not mOperacionEnCurso.RelacionENFichero.TipoEntNegoio Is Nothing) Then
                    Me.ArbolNododeT1.ElementoSeleccionado = mOperacionEnCurso.RelacionENFichero.TipoEntNegoio
                    'TODO: esto es un error porque dado que la relacion es uno a muchos se debieran recuperar el resto de elementos de la clasificacion
                    '  apuntamos en la tabla de entidades 
                    Me.AgregarTabla(Me.mOperacionEnCurso.RelacionENFichero.TipoEntNegoio)
                End If

                If (Not mOperacionEnCurso.RelacionENFichero.HuellaFichero Is Nothing) AndAlso (Not mOperacionEnCurso.RelacionENFichero.HuellaFichero.Colidentificaciones Is Nothing) AndAlso (mOperacionEnCurso.RelacionENFichero.HuellaFichero.Colidentificaciones.Count > 0) Then
                    ' cargar los elementos de la coleccion de identificaciones del fichero
                    RefrescarGridIdentificaciones()
                End If


                'ponemos la ruta en el label de documento cargado
                If Me.mOperacionEnCurso.TipoCanal Is Nothing Then
                    Me.lblDocumentoCargado.Text = Me.mOperacionEnCurso.RelacionENFichero.HuellaFichero.NombreOriginalFichero & " (Ninguno)"

                Else
                    Me.lblDocumentoCargado.Text = Me.mOperacionEnCurso.RelacionENFichero.HuellaFichero.NombreOriginalFichero & " (" & Me.mOperacionEnCurso.TipoCanal.Nombre & ")"

                End If




                'ponemos el comentario
                Me.txtvComentarioOperacion.Text = mOperacionEnCurso.ComentarioOperacion

                Me.dgvEntidades.Focus()
            Else
                Me.ArbolNododeT1.Focus()
            End If





        Else
            MessageBox.Show("No hay ninguna operación a procesar", "Recuperar", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If

    End Sub
    Private Sub RefrescarGridIdentificaciones()
        Me.DataGridView1.AutoGenerateColumns = False
        Me.DataGridView1.DataSource = Nothing

        If mOperacionEnCurso Is Nothing Then
            Return
        End If

        Me.DataGridView1.DataSource = mOperacionEnCurso.RelacionENFichero.HuellaFichero.Colidentificaciones

        Me.DataGridView1.Columns.Clear()

        Dim dtc As Windows.Forms.DataGridViewColumn


        dtc = New Windows.Forms.DataGridViewTextBoxColumn
        dtc.HeaderText = "Identificador"
        dtc.DataPropertyName = "identificacion"
        Me.DataGridView1.Columns.Add(dtc)


        dtc = New Windows.Forms.DataGridViewTextBoxColumn
        dtc.HeaderText = "Tipo Documento"
        dtc.DataPropertyName = "TipoFichero"
        dtc.ReadOnly = True
        Me.DataGridView1.Columns.Add(dtc)

        dtc = New Windows.Forms.DataGridViewTextBoxColumn
        dtc.HeaderText = "ID"
        dtc.DataPropertyName = "ID"
        dtc.ReadOnly = True
        Me.DataGridView1.Columns.Add(dtc)


        'Me.DataGridView1.Columns(0).Visible = False
        'Me.DataGridView1.Columns(0).ReadOnly = True

    End Sub
#End Region

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public WriteOnly Property OperacionEnRelacionENFichero() As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Set(ByVal value As AmvDocumentosDN.OperacionEnRelacionENFicheroDN)

            mOperacionEnCurso = value
            DNaIU(mOperacionEnCurso)

        End Set
    End Property





    Private Sub RecuperarSiguienteOperacion()
        Using New CursorScope(Cursors.WaitCursor)
            'If Me.lstCanales.SelectedItem Is Nothing Then
            '    Advertencia("Seleccionar", "Debe seleccionar un Canal de Entrada")
            '    Exit Sub
            'End If
            Dim idtipocanal As String
            If Not Me.lstCanales.SelectedItem Is Nothing Then
                idtipocanal = CType(Me.lstCanales.SelectedItem, CanalIU).TipoCanal.ID
            End If
            OperacionEnRelacionENFichero = Me.mControlador.RecuperarSiguienteOperacionAProcesar(Nothing, idtipocanal)


        End Using
    End Sub
    Private Sub RecuperarSiguienteOperacionXTipoEnt()
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



    
            If Me.SelectorCargaTab.SelectedTab.Name = "tpCanales" Then

                ' clasificados
                RecuperarSiguienteOperacion()
            Else

                ' post clasificados
                Me.RecuperarSiguienteOperacionXTipoEnt()
            End If








        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub lblDocumentoCargado_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles lblDocumentoCargado.TextChanged
        Try
            '       Me.ToolTip.SetToolTip(Me.lblDocumentoCargado, Me.lblDocumentoCargado.Text)
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub


#Region "Métodos Aceptar"

    Private Sub ClasificarOperacion()
        Dim mimensaje As String = String.Empty

        Dim micolent As AmvDocumentosDN.ColEntNegocioDN = Me.EntidadesNegocio(mimensaje)
        If micolent Is Nothing Then
            Advertencia("Aceptar", mimensaje)
            Exit Sub
        End If

        'ponemos el comentario
        AsignarValores()

        'llamamos al controlador
        Me.mControlador.ClasificarOperacion(Me.mOperacionEnCurso, micolent)

        LimpiarOperacion()
    End Sub

    Private Sub cmdAceptarOperacion_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAceptarOperacion.Click
        Try
            ClasificarOperacion()
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
                Advertencia(sender, "Debe explicar el motivo de la anulación en el Comentario")
                Exit Sub
            End If
            If MessageBox.Show("Va a Anular una operación" & Chr(13) & Chr(13) & "¿Está seguro?", "Incidentar", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) = DialogResult.OK Then
                AsignarValores()
                Me.mOperacionEnCurso.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.Anular)
                Me.mControlador.AnularOperacion(Me.mOperacionEnCurso)
                LimpiarOperacion()
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
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

            Me.DataGridView1.DataSource = Nothing

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

                'If CType(String.Empty & mir("IDEntidad"), String).Trim() = String.Empty Then
                '    Mensaje = "Debe rellenar el ID de todas las Entidades"
                '    huboerror += 1
                '    Exit For
                'Else
                '    miid = CType(mir("IDEntidad"), String).Trim()
                'End If

                'If CType(String.Empty & mir("Comentario"), String).Trim() <> String.Empty Then
                '    micomentario = CType(mir("Comentario"), String).Trim()
                'End If

                Dim mient As New AmvDocumentosDN.EntNegocioDN()
                mient.TipoEntNegocioReferidora = CType(mir("TipoEntidad"), AmvDocumentosDN.TipoEntNegoioDN)
                mient.IdEntNeg = miid
                mient.Comentario = micomentario

                micolentnegocios.Add(mient)

            Next
        Else
            Mensaje = "Debe asignar al menos una Categoría para el Documento"
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
                Clipboard.SetText(mir("IDEntidad").ToString)
            ElseIf Me.dgvEntidades.SelectedCells.Count <> 0 Then
                Clipboard.Clear()
                Dim mir As DataRow = Me.mTablaEntidades.Rows(Me.dgvEntidades.SelectedCells(0).RowIndex)
                Clipboard.SetText(mir("IDEntidad").ToString)
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
                    'Me.cmdCopiarID.Focus()
                    CopiarID()
                End If
            Case Keys.F3
                'Copiar Ruta
                If Not Me.mOperacionEnCurso Is Nothing Then
                    Me.dgvEntidades.EndEdit()
                    'Me.cmdCopiarRuta.Focus()
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
                    'Me.cmdAceptarOperacion.Focus()
                    ClasificarOperacion()
                End If
            Case Keys.F6
                'Cerrar
                If Not Me.mOperacionEnCurso Is Nothing AndAlso Me.cmdAceptarYCerrar.Visible Then
                    Me.dgvEntidades.EndEdit()
                    Me.ClasificarYCerraOperacion()
                End If

        End Select
    End Sub
#End Region

    Private Sub chkCanales_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkCanales.CheckedChanged
        Try
            Me.lstCanales.Enabled = Me.chkCanales.Checked
            If Me.chkCanales.Checked Then
                Me.lstCanales.SelectedIndex = 0
            Else

                Me.lstCanales.SelectedItem = Nothing
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

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
        Me.dgvEntidades.Columns(0).HeaderText = "Categoría"
        'Me.dgvEntidades.Columns(1).HeaderText = "ID Ent Ref"
        'Me.dgvEntidades.Columns(2).HeaderText = "Comentario"
        Me.dgvEntidades.Columns(0).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        Me.dgvEntidades.Refresh()
    End Sub

    Private Overloads Sub AgregarTabla(ByVal pTipoEntidad As AmvDocumentosDN.TipoEntNegoioDN)
        Dim mir As DataRow =  Me.mTablaEntidades.NewRow

        mir("TipoEntidad") = pTipoEntidad

        'vemos si tiene multiselección
        If Me.chkMultiseleccion.Checked Then
            'comprobamos que no exista ya
            For Each mirow As DataRow In Me.mTablaEntidades.Rows
                If CType(mirow("TipoEntidad"), AmvDocumentosDN.TipoEntNegoioDN).ID = pTipoEntidad.ID Then
                    Exit Sub
                End If
            Next
        Else
            'no tiene multiselección, hay que sustituir la fila 
            'que(tenga) si la hubiese

            If Me.mTablaEntidades.Rows.Count <> 0 Then
                Dim mirexistente As DataRow = Me.mTablaEntidades.Rows(0)
                'copiamos el id y el comentario de la tabla
                'mir("IDEntidad") = mirexistente("IDEntidad")
                'mir("Comentario") = mirexistente("Comentario")
                'borramos la tabla que exista
                Me.mTablaEntidades.Rows.Remove(mirexistente)
            End If

        End If

        Me.mTablaEntidades.Rows.Add(mir)
        Me.dgvEntidades.Refresh()
    End Sub

    Private Overloads Sub AgregarTabla(ByVal pEntidad As AmvDocumentosDN.EntNegocioDN)
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





    Private Sub dgvEntidades_RowsAdded(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewRowsAddedEventArgs) Handles dgvEntidades.RowsAdded
        ' dgvEntidades.Rows(e.RowIndex).Cells(1).Selected = True
        dgvEntidades.Focus()

    End Sub


    Private Sub cmdAceptarYCerrar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAceptarYCerrar.Click

        Try
            ClasificarYCerraOperacion()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub
    Private Sub ClasificarYCerraOperacion()
        Dim tipoOperacionActual As AmvDocumentosDN.TipoOperacionREnFDN = Me.mOperacionEnCurso.TipoOperacionREnF

        Try
            'If MessageBox.Show("Va a cerrar una operación ¿Está seguro?", "Cerrar operación", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) = DialogResult.OK Then
            '    AsignarValores()

            '    Me.mOperacionEnCurso.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar)

            '    If Me.mTablaEntidades.Rows.Count <> 0 Then
            '        Dim mensaje As String = String.Empty
            '        Dim micolent As AmvDocumentosDN.ColEntNegocioDN = Me.EntidadesNegocio(mensaje)
            '        If micolent Is Nothing OrElse micolent.Count = 0 Then
            '            Advertencia("Aceptar", mensaje)
            '            Me.mOperacionEnCurso.TipoOperacionREnF = tipoOperacionActual
            '            Exit Sub
            '        End If
            '        Me.mOperacionEnCurso.RelacionENFichero.EntidadNegocio = micolent(0)
            '    Else
            '        Advertencia("Aceptar", "Debe establecer la categoría a la que pertenece")
            '        Me.mOperacionEnCurso.TipoOperacionREnF = tipoOperacionActual
            '        Exit Sub
            '    End If
            '    Me.mControlador.ClasificarYCerraOperacion(Me.mOperacionEnCurso)
            '    LimpiarOperacion()
            'End If



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
                    Me.mOperacionEnCurso.RelacionENFichero.TipoEntNegoio = micolent(0).TipoEntNegocioReferidora
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

#Region "multiselección"
    Private Sub chkMultiseleccion_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkMultiseleccion.CheckedChanged
        Try
            If Me.chkMultiseleccion.Checked = False Then
                'si tiene más de una fila no puede quitarse el multiselect
                If Me.mTablaEntidades.Rows.Count > 1 Then
                    Advertencia(sender, "No puede quitar la multiselección de la Tabla si tiene varias filas en ella")
                    Me.chkMultiseleccion.Checked = True
                    Exit Sub
                End If
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub
#End Region

    Private Sub ComboBox1_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles ComboBox1.KeyUp
        If e.KeyCode = 13 Then
            AñadirIdentidadFichero()
        End If





    End Sub




    Private Sub AñadirIdentidadFichero()

        Try
            If Not mOperacionEnCurso Is Nothing Then
                Dim iddoc As New Framework.Ficheros.FicherosDN.IdentificacionDocumentoDN
                iddoc.TipoFichero = ComboBox1.SelectedItem
                If iddoc.TipoFichero Is Nothing Then
                    MessageBox.Show("debe seleccionar un tipo de documento ")
                End If


                mOperacionEnCurso.RelacionENFichero.HuellaFichero.Colidentificaciones.Add(iddoc)
                RefrescarGridIdentificaciones()

                DataGridView1.Rows(DataGridView1.Rows.Count - 1).Cells(0).Selected = True
                Me.DataGridView1.Focus()
            End If




        Catch ex As Exception
            MostrarError(ex, Me)
        End Try

    End Sub
    Private Sub eliminarIdentidadFicheroSeleccioando()

        Try
            If Me.DataGridView1.SelectedRows.Count > 0 Then
                Dim iddoc As Framework.Ficheros.FicherosDN.IdentificacionDocumentoDN = Me.DataGridView1.SelectedRows(0).DataBoundItem
                iddoc.TipoFichero = ComboBox1.SelectedItem
                mOperacionEnCurso.RelacionENFichero.HuellaFichero.Colidentificaciones.Remove(iddoc)
            End If
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try



        RefrescarGridIdentificaciones()
    End Sub
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        eliminarIdentidadFicheroSeleccioando()
    End Sub


    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        AñadirIdentidadFichero()
    End Sub


    Private Sub ArbolNododeT1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ArbolNododeT1.Load

    End Sub

    Private Sub cmdSeleccionarArbol_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdSeleccionarArbol.Click
        Try
            Dim mipaquete As New Hashtable
            Me.Marco.Navegar("SeleccionArbol", Me.ParentForm, Nothing, MotorIU.Motor.TipoNavegacion.Modal, Me.mControlador.ControladorForm.FormularioContenedor.GenerarDatosCarga, mipaquete)
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

    Private Sub tpTipoEntNeg_Enter(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tpTipoEntNeg.Enter
        Dim miprincipal As Framework.Usuarios.DN.PrincipalDN = Me.Marco.DatosMarco("Principal")
        Dim operador As AmvDocumentosDN.OperadorDN
        Dim cabecera As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        operador = miprincipal.UsuarioDN.HuellaEntidadUserDN.EntidadReferida
        cabecera = Me.mControlador.RecuperarArbolEntidades
        cabecera.NodoTipoEntNegoio.PodarNodosHijosNoContenedoresHojas(operador.ColTipoEntNegoio, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
        Me.ArbolNododeT1.NodoPrincipal = cabecera.NodoTipoEntNegoio
        ArbolNododeT1.ExpandirArbol()
    End Sub

    Private Sub tpCanales_Enter(ByVal sender As Object, ByVal e As System.EventArgs) Handles tpCanales.Enter
        Me.ArbolNododeT1.NodoPrincipal = Me.mControlador.RecuperarArbolEntidades.NodoTipoEntNegoio
        ArbolNododeT1.ExpandirArbol()
    End Sub

 
   
End Class
