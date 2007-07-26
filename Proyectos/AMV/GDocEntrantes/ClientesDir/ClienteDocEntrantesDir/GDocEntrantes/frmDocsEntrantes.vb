Imports AuxIU
Imports Framework.IU.IUComun
Imports MotorBusquedaBasicasDN

Public Class frmDocsEntrantes

#Region "atributos"
    Private mControlador As Controladores.frmDocsEntrantesctrl
    'la operacion que estamos tratando
    Private mOperacionEnCurso As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
    'la tabla que se usa para el historial
    Private mTablaHistorial As DataTable
    'la tabla de entidades de negocio
    Private mTablaEntidades As DataTable
    'generador del arbol
    Private mGeneradorArbol As PresentacionArbol

    'Intervalo del timer
    Private mIntervalo As Integer

    'Nos dice si hay algo bloqueado
    Private mBloqueo As Bloqueo

    Private Enum Bloqueo As Integer
        ninguno = 0
        procesar = 1
        postprocesar = 2
    End Enum
#End Region

#Region "inicializadores"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        'cargamos el controlador
        Me.mControlador = Me.Controlador


        'construimos la datatable para mostrar el historial
        RellenarTablaHistorial()

        Me.RefrescarHilo()


        'nos cargamos el tabPostProcesar si no está autorizado para ello
        Dim miprincipal As Framework.Usuarios.DN.PrincipalDN = Me.cMarco.DatosMarco("Principal")

        Dim estoyautorizado As Boolean

        'If Not miprincipal.IsInRole("Operador Cierre") Then
        '    ' Me.TabControl1.Controls.Remove(Me.tbpPostProcesar)
        'Else
        '    Me.TabControl1.SelectTab(tbpPostProcesar)
        '    'Me.tbpPostProcesar.Show()
        '    estoyautorizado = True
        'End If

        ''nos cargamos el tabProcesar si no está autorizado para ello
        'If Not miprincipal.IsInRole("Operador Entrada") Then
        '    ' Me.TabControl1.Controls.Remove(Me.tbpProcesar)
        'Else
        '    Me.TabControl1.SelectTab(Me.tbpProcesar)
        '    'Me.tbpProcesar.Show()
        '    estoyautorizado = True
        'End If



        If miprincipal.IsInRole("Operador Cierre") Or miprincipal.IsInRole("Operador Entrada") Then
            Me.TabControl1.SelectTab(tbpProcesar)
            'Me.tbpPostProcesar.Show()
            estoyautorizado = True
            Me.TabControl1.Controls.Remove(Me.tbpPostProcesar)
        Else
            Me.TabControl1.Controls.Remove(Me.tbpProcesar)

        End If




        If Not estoyautorizado Then
            MessageBox.Show("No tiene permisos para ustilizar esta aplicación", "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Application.Exit()
        End If




        ' verificar si el operador dispone de operaciones pendientes de cierre
        VerificarOperacionPendiente()


    End Sub

    Private Sub VerificarOperacionPendiente()

        Dim oper As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim ctrl As Controladores.frmDocsEntrantesctrl
        ctrl = mControlador
        oper = ctrl.RecuperarOperacionEnCursoPara()
        If Not oper Is Nothing Then

            ' el operador tine una operacion a medias
            ' hay que verificar si se trata de una operacion de clasificacion o de post clasificacion
            Select Case oper.RelacionENFichero.EstadosRelacionENFichero.Valor
                Case AmvDocumentosDN.EstadosRelacionENFichero.Creada
                    ' operacion para clasificacion
                    Me.ctrlClasificar.OperacionEnRelacionENFichero = oper
                    'Me.TabControl1.SelectedTab = Me.tbpProcesar
                    Me.TabControl1.SelectTab(Me.tbpProcesar)
                    'Me.tbpProcesar.Show()
                    MessageBox.Show("Tiene una operación pendiente de clasificar", "Operación pendiente", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Case AmvDocumentosDN.EstadosRelacionENFichero.Clasificando
                    'operacion para post clasificacion
                    'Me.ctrlPostClasificar.OperacionEnRelacionENFichero = oper
                    'Me.TabControl1.SelectedTab = Me.tbpPostProcesar

                    Me.ctrlClasificar.OperacionEnRelacionENFichero = oper
                    Me.TabControl1.SelectTab(Me.tbpProcesar)



                    ' Me.TabControl1.SelectTab(Me.tbpPostProcesar)
                    'Me.tbpPostProcesar.Show()
                    MessageBox.Show("Tiene una operación pendiente de clasificar", "Operación pendiente", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End Select




        End If

    End Sub

#Region "Historial"
    Private Sub RellenarTablaHistorial()
        Me.mTablaHistorial = New DataTable

        Dim colnumero As New DataColumn("Numero", GetType(Integer))
        colnumero.Caption = "Nº"
        Dim colImagen As New DataColumn("Imagen", GetType(Image))
        colImagen.Caption = "-"
        Dim colruta As New DataColumn("Ruta", GetType(String))
        colruta.Caption = "Ruta"
        Dim colaccion As New DataColumn("Accion", GetType(String))
        colaccion.Caption = "Acción"
        Dim colHora As New DataColumn("Hora", GetType(DateTime))
        colHora.Caption = "Hora"

        mTablaHistorial.Columns.Add(colnumero)
        mTablaHistorial.Columns.Add(colImagen)
        mTablaHistorial.Columns.Add(colruta)
        mTablaHistorial.Columns.Add(colaccion)
        mTablaHistorial.Columns.Add(colHora)


        Me.DataGridView1.DataSource = Me.mTablaHistorial
        Me.DataGridView1.Refresh()
    End Sub
#End Region

#End Region

#Region "Métodos públicos"
    Public Sub CargarOperacionPostClasificar(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN)
        Me.ctrlPostClasificar.OperacionEnRelacionENFichero = pOperacion
        Me.TabControl1.SelectedTab = Me.tbpPostProcesar
    End Sub
#End Region

#Region "Historial"

    Private Sub ctrlPostClasificar_ActualizarHistorial(ByVal Imagen As System.Drawing.Image, ByVal Ruta As String, ByVal Accion As String, ByVal Hora As String) Handles ctrlPostClasificar.ActualizarHistorial
        ActualizarHistorial(Imagen, Ruta, Accion, Hora)
        RefrescarHilo()
    End Sub

    Private Sub ctrlClasificar_ActualizarHistorial(ByVal Imagen As System.Drawing.Image, ByVal Ruta As String, ByVal Accion As String, ByVal Hora As String) Handles ctrlClasificar.ActualizarHistorial
        ActualizarHistorial(Imagen, Ruta, Accion, Hora)
        RefrescarHilo()
    End Sub

    Private Sub ActualizarHistorial(ByVal Imagen As Image, ByVal Ruta As String, ByVal Accion As String, ByVal Hora As String)
        Dim mir As DataRow = Me.mTablaHistorial.NewRow

        mir("Numero") = Me.mTablaHistorial.Rows.Count + 1
        mir("Imagen") = Imagen
        mir("Ruta") = Ruta
        mir("Accion") = Accion
        mir("Hora") = Hora

        Me.mTablaHistorial.Rows.Add(mir)
        Me.DataGridView1.Refresh()
    End Sub

    Private Sub cmdAbrir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAbrir.Click


        Try
            AbrirArchivo()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try


    End Sub

    Private Sub AbrirArchivo()
        If Me.DataGridView1.SelectedRows.Count > 0 Then
            Using New CursorScope(Cursors.WaitCursor)
                Dim pr As Process = System.Diagnostics.Process.Start(Me.DataGridView1.SelectedRows(0).Cells("Ruta").Value.ToString)
            End Using
        Else
            MessageBox.Show("Para abrir un fichero debe seleccionar una fila.", "Aviso:", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If


    End Sub

#End Region

#Region "hilo"
    ''' <summary>
    ''' HILO
    ''' </summary>
    Private Sub RefrescarNumerosCanalesYArbol()
        Dim mids As DataSet = Me.mControlador.BalizaNumCanalesTipoEntNeg
        'llamamos al control Procesar
        Me.ctrlClasificar.RefrescarNumerosCanalesYArbol(mids)
        'llamamos al control postprocesar
        Me.ctrlPostClasificar.RefrescarNumerosCanalesYArbol(mids)
    End Sub

    Private Sub RefrescarHilo()
        Try
            Using New CursorScope(Cursors.WaitCursor)
                Me.Timer1.Enabled = False
                RefrescarNumerosCanalesYArbol()
                '  Me.Timer1.Enabled = True
            End Using
        Catch ex As Exception
            MostrarError(ex, "Error")
        End Try

    End Sub
#End Region

#Region "Activar/Desactivar Tabs"

    Private Sub ctrlClasificar_OperacionLiberada() Handles ctrlClasificar.OperacionLiberada
        Me.mBloqueo = Bloqueo.ninguno
        Me.cmdBuscarArchivo.Enabled = True
        Me.cmdRefrescar.Enabled = True
        'refrescamos los datos de los hilos
        RefrescarHilo()
    End Sub

    Private Sub ctrlClasificar_OperacionRecuperada() Handles ctrlClasificar.OperacionRecuperada
        Me.mBloqueo = Bloqueo.procesar
        Me.cmdBuscarArchivo.Enabled = False
        Me.cmdRefrescar.Enabled = False
    End Sub

    Private Sub ctrlPostClasificar_OperacionLiberada() Handles ctrlPostClasificar.OperacionLiberada
        Me.mBloqueo = Bloqueo.ninguno
        Me.cmdBuscarArchivo.Enabled = True
        Me.cmdRefrescar.Enabled = True
        RefrescarHilo()
    End Sub

    Private Sub ctrlPostClasificar_OperacionRecuperada() Handles ctrlPostClasificar.OperacionRecuperada
        Me.mBloqueo = Bloqueo.postprocesar
        Me.cmdBuscarArchivo.Enabled = False
        Me.cmdRefrescar.Enabled = False
    End Sub

    Private Sub TabControl1_Selected(ByVal sender As Object, ByVal e As System.Windows.Forms.TabControlEventArgs) Handles TabControl1.Selected
        Try
            Me.cmdRefrescar.Visible = (Not e.TabPage Is Me.tbpHistorial)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub TabControl1_Selecting(ByVal sender As Object, ByVal e As System.Windows.Forms.TabControlCancelEventArgs) Handles TabControl1.Selecting
        If Me.mBloqueo <> Bloqueo.ninguno Then
            If Me.mBloqueo = Bloqueo.procesar AndAlso Not e.TabPage Is Me.tbpProcesar Then
                MessageBox.Show("Debe terminar de procesar el Documento cargado", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                e.Cancel = True
            ElseIf Me.mBloqueo = Bloqueo.postprocesar AndAlso Not e.TabPage Is Me.tbpPostProcesar Then
                MessageBox.Show("Debe terminar de procesar el Documento cargado", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                e.Cancel = True
            End If
        End If
    End Sub

#End Region


#Region "Opacidad formulario"
    Private Sub Form1_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated
        Me.Opacity = 1
    End Sub

    Private Sub Form1_Deactivate(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Deactivate
        If Me.CheckBox1.Checked Then
            Me.Opacity = 0.3
        End If
    End Sub
#End Region

#Region "Siempre visible"
    Private Sub CheckBox1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox1.CheckedChanged
        Try
            Me.TopMost = Me.CheckBox1.Checked
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub
#End Region


#Region "TIMER"
    Private Sub Timer1_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        Try
            Me.mIntervalo -= 1
            If Me.mIntervalo <= 0 Then
                Me.mIntervalo = 5 * 60 'va a 1 seg

                Me.RefrescarHilo()

            End If
        Catch ex As Exception
            MostrarError(ex, "Timer")
        End Try
    End Sub
#End Region



    Private Sub cmdRefrescar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdRefrescar.Click
        Try
            RefrescarHilo()
        Catch ex As Exception
            MostrarError(ex, "Refrescar")
        End Try
    End Sub

    ''' <summary>
    ''' Capturamos el evento del teclado y lo lanzamos al control correspondiente sólo si 
    ''' están activos alguno de ellos
    ''' </summary>
    Private Sub frmDocsEntrantes_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyUp
        Try
            Dim tabseleccionado As TabPage = Me.TabControl1.SelectedTab
            If tabseleccionado Is Me.tbpProcesar Then
                Me.ctrlClasificar.RecogerTeclaAbreviada(e)
            ElseIf tabseleccionado Is Me.tbpPostProcesar Then
                Me.ctrlPostClasificar.RecogerTeclaAbreviada(e)
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub


    Private Sub cmdBuscarArchivo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdBuscarArchivo.Click
        Try
            Dim mipaquete As New Hashtable
            Dim miParametroCargaEst As New ParametroCargaEstructuraDN

            miParametroCargaEst.NombreVistaSel = "vwBuscarFicherosClienteVis"
            miParametroCargaEst.NombreVistaVis = "vwBuscarFicherosClienteVis"

            miParametroCargaEst.CamposaCargarDatos = New List(Of String)
            miParametroCargaEst.CamposaCargarDatos.Add("Tipo_Entidad")

            miParametroCargaEst.DestinoNavegacion = "Archivo"

            mipaquete.Add("ParametroCargaEstructura", miParametroCargaEst)

            mipaquete.Add("MultiSelect", False)
            mipaquete.Add("TipoNavegacion", TipoNavegacion.Modal)
            mipaquete.Add("Agregable", False)
            mipaquete.Add("EnviarDatatableAlNavegar", False)
            mipaquete.Add("Navegable", True)
            mipaquete.Add("AlternatingBackcolorFiltro", System.Drawing.Color.LightBlue)
            mipaquete.Add("AlternatingBackcolorResultados", System.Drawing.Color.LightBlue)

            mipaquete.Add("Titulo", "Búsqueda de Documentos")


            Me.cMarco.Navegar("Filtro", Me, Nothing, TipoNavegacion.Modal, Me.GenerarDatosCarga(), mipaquete)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub ctrlClasificar_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ctrlClasificar.Load

    End Sub
End Class

Public Class CanalIU
    Private mTipoCanal As AmvDocumentosDN.TipoCanalDN
    Private mNumero As Integer

    Public Property TipoCanal() As AmvDocumentosDN.TipoCanalDN
        Get
            Return mTipoCanal
        End Get
        Set(ByVal value As AmvDocumentosDN.TipoCanalDN)
            Me.mTipoCanal = value
        End Set
    End Property

    Public Property Numero() As Integer
        Get
            Return mNumero
        End Get
        Set(ByVal value As Integer)
            mNumero = value
        End Set
    End Property

    Public Overrides Function ToString() As String
        Return Me.mTipoCanal.Nombre & " (" & String.Format("{0:D}", Me.mNumero) & ")"
    End Function
End Class

Public Class PresentacionArbol
    Inherits ControlesPGenericos.GestorPresentacionArbolesDefecto

    Private mTabla As DataTable

    Public Property Tabla() As DataTable
        Get
            Return mTabla
        End Get
        Set(ByVal value As DataTable)
            mTabla = value
        End Set
    End Property

    Public Overrides Sub GenerarElementoParaImageList(ByVal pObjeto As Framework.DatosNegocio.IEntidadBaseDN, ByRef TextoSalida As String, ByRef KeyImagenSalida As String, ByRef KeyImagenSeleccionada As String)
        MyBase.GenerarElementoParaImageList(pObjeto, TextoSalida, KeyImagenSalida, KeyImagenSeleccionada)


        'ahora ponemos en el nombre también el Numero
        'ID()
        'Nombre()
        'Num()  

        If Not mTabla Is Nothing Then
            ' Dim mirows As DataRow() = Me.mTabla.Select("ID=" & pObjeto.ID & " AND Nombre='" & pObjeto.Nombre & "'")
            Dim mirows As DataRow() = Me.mTabla.Select("ID=" & pObjeto.ID & " AND TipoEntidadNegocio='" & pObjeto.Nombre & "'")
            If mirows.GetLength(0) <> 0 Then
                TextoSalida += " (" & String.Format("{0:D}", CType(mirows(0)("Num"), String)) & ")"
            End If
        End If

    End Sub
End Class