Imports System.IO
Imports System.Threading

Imports Framework.Ficheros

Imports Framework.Usuarios.DN

Public Class frmClienteSonda
    Protected mCanalSelecionado As AmvDocumentosDN.CanalEntradaDocsDN
    Protected mColCanales As AmvDocumentosDN.ColCanalEntradaDocsDN
    Protected mDirectorioTemporal As String
    'Protected mDirectorioIncidentados As String
    Protected mht As New Hashtable
    Protected mBloqueado As Boolean
    Protected semaforo As AutoResetEvent = New System.Threading.AutoResetEvent(False)
    Protected hilo As Thread
    Protected mDatosIdentidad As DatosIdentidadDN

    Private Sub btnBloquear_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Disposed
        TerminarThred()
    End Sub


    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        ' cargar los datos de configuracion de la aplicacion
        'Throw New ApplicationException

        CargarConfiguracion()
        Me.mDirectorioTemporal = ""

        ' cargar el estado
        Me.RecuperarEstado()


        Dim miSolicitarBloqueo As Boolean = True

        If Me.mDatosIdentidad Is Nothing Then
            miSolicitarBloqueo = False
            Do
                Me.SolicitarAutorizacion()
            Loop Until Not Me.mDatosIdentidad Is Nothing
        End If

        Try
            SolicitarArbolTiposEntidadesNegocio()
            CargarTiposCanales()
        Catch ex As Exception
            MessageBox.Show("Se requiere visivilidad del servidor para arrancar el proceso", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End
        End Try



        Me.ArrancarThred()
        ReasignarSondas()

        ' configurar el timer

        Me.Timer1.Enabled = False
        If Framework.Configuracion.AppConfiguracion.DatosConfig.ContainsKey("IntervaloRefresco") Then
            Me.Timer1.Interval = Framework.Configuracion.AppConfiguracion.DatosConfig("IntervaloRefresco")
        Else
            Me.Timer1.Interval = 1000
        End If

        If Framework.Configuracion.AppConfiguracion.DatosConfig.ContainsKey("BusquedaPediodicaActiva") Then
            Me.Timer1.Enabled = Framework.Configuracion.AppConfiguracion.DatosConfig("BusquedaPediodicaActiva")
        Else
            Me.Timer1.Enabled = True
        End If

        If miSolicitarBloqueo Then
            mBloqueado = False
            SolicitarBloquear()
        Else
            mBloqueado = True
        End If

    End Sub

    Private Sub CargarTiposCanales()
        Dim ags As ClienteSondaAS.GDocEntrantesAS
        ags = New ClienteSondaAS.GDocEntrantesAS
        Me.ctrlCanalEntrada1.AsignarCanales(ags.RecuperarTiposCanal)
    End Sub

    Private Sub SolicitarArbolTiposEntidadesNegocio()
        Dim ags As ClienteSondaAS.GDocEntrantesAS
        ags = New ClienteSondaAS.GDocEntrantesAS
        Dim cabeceraArbol As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        cabeceraArbol = ags.RecuperarArbolTiposEntNegocio()
        If cabeceraArbol Is Nothing Then
            Me.ctrlCanalEntrada1.Asignar(Nothing)

        Else
            Me.ctrlCanalEntrada1.Asignar(cabeceraArbol.NodoTipoEntNegoio)

        End If
    End Sub

    Private Sub CargarConfiguracion()
        Dim prop As System.Configuration.SettingsPropertyValue



        Dim a As Int64
        a = My.Settings.IntervaloRefresco
        For Each prop In My.Settings.PropertyValues
            Framework.Configuracion.AppConfiguracion.DatosConfig.Add(prop.Name, prop.PropertyValue)
        Next

    End Sub


#Region "Metodos Controladores"
    Private Sub OnFicheroCambia(ByVal source As Object, ByVal e As FileSystemEventArgs)

        Dim canal As AmvDocumentosDN.CanalEntradaDocsDN

        canal = Me.mColCanales.RecuperarXRuta(IO.Path.GetDirectoryName(e.FullPath))
        If canal Is Nothing Then
            Throw New ApplicationException("recivido evento de sonda sin canal asignado ")
        Else

            ' Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine("File: " & e.FullPath & " " & e.ChangeType)
            CapturarFicheroParaEnvioEnDir(canal)
            Me.despertarThread()
        End If




    End Sub


    Private Sub btnAñadir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAñadir.Click
        mColCanales.Add(Me.ctrlCanalEntrada1.NuevoCanal())
        Refrescar()
    End Sub

    Private Sub btnEliminar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEliminar.Click
        If Not Me.ctrlCanalEntrada1.CanalEntradaDocs Is Nothing Then
            CanalEntradaDocsDNBindingSource.DataSource = Nothing
            Me.mColCanales.EliminarEntidadDNxGUID(Me.ctrlCanalEntrada1.CanalEntradaDocs.GUID)
            Me.ctrlCanalEntrada1.CanalEntradaDocs = Nothing
            Me.Refrescar()
        End If


    End Sub

    Private Sub btnTodoOn_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnTodoOn.Click
        EncenderApagarTodo(True)
    End Sub

    Private Sub btnTodoOff_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnTodoOff.Click
        EncenderApagarTodo(False)

    End Sub

    Private Sub btnOn_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Me.EncenderApagar(mCanalSelecionado, True)
    End Sub

    Private Sub btnOff_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Me.EncenderApagar(mCanalSelecionado, False)

    End Sub

    Private Sub btnGuardar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGuardar.Click
        Me.SerializarEstado()
    End Sub

    Private Sub DataGridView1_CellDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles DataGridView1.CellDoubleClick
        If e.RowIndex <> -1 Then
            Me.mCanalSelecionado = DataGridView1.Rows(e.RowIndex).DataBoundItem
            Me.ctrlCanalEntrada1.CanalEntradaDocs = Me.mCanalSelecionado
        End If
    End Sub

    Private Sub btnRecuperar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Me.RecuperarEstado()
    End Sub

    Private Sub btnBloquear1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBloquear1.Click
        SolicitarBloquear()

    End Sub


    Private Sub SolicitarBloquear()
        Try
            Bloquear()
        Catch ex As Exception
            MessageBox.Show("No está autorizado", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            SolicitarBloquear()
        End Try
    End Sub


    Private Sub DataGridView1_CellEndEdit(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles DataGridView1.CellEndEdit
        Dim canal As AmvDocumentosDN.CanalEntradaDocsDN
        canal = DataGridView1.Rows(e.RowIndex).DataBoundItem
        Me.EncenderApagar(canal, canal.Actividad)
    End Sub

#End Region

#Region "Metodos"

    Private Sub CapturarFicheroParaEnvioTodosCanalesActivosyEnviar()
        CapturarFicheroParaEnvioTodosCanalesActivos()
        Me.despertarThread()
    End Sub


    Private Sub CapturarFicheroParaEnvioTodosCanalesActivos()
        Dim cn As AmvDocumentosDN.CanalEntradaDocsDN

        For Each cn In Me.mColCanales

            If cn.Actividad AndAlso Not cn.Incidentado Then
                CapturarFicheroParaEnvioEnDir(cn)
            End If

        Next



    End Sub

    Private Sub CapturarFicheroParaEnvioEnDir(ByVal pcanal As AmvDocumentosDN.CanalEntradaDocsDN)
        ' Specify what is done when a file is changed, created, or deleted.
        Console.WriteLine("File: " & pcanal.Ruta)



        Dim di As System.IO.DirectoryInfo
        di = System.IO.Directory.CreateDirectory(pcanal.Ruta)

        Dim fi As IO.FileInfo
        For Each fi In di.GetFiles
            Try
                CapturarFicheroParaEnvio(fi.FullName, pcanal)
            Catch ex As Exception
                'Beep()
                Debug.WriteLine("el fichero " & fi.FullName & " no pudo ser colocado en cola error:" & ex.Message)
            End Try

        Next

        '   Me.despertarThread()

    End Sub
    Public Sub Refrescar()
        CanalEntradaDocsDNBindingSource.DataSource = Nothing
        If Not CanalEntradaDocsDNBindingSource.DataSource Is Me.mColCanales Then
            CanalEntradaDocsDNBindingSource.DataSource = Me.mColCanales
        End If

        'Me.DataGridView1.DataSource
        Me.DataGridView1.Refresh()
    End Sub

    Public Sub EncenderApagarTodo(ByVal pActividad As Boolean)
        Dim canal As AmvDocumentosDN.CanalEntradaDocsDN
        For Each canal In Me.mColCanales

            EncenderApagar(canal, pActividad)

        Next
        Me.DataGridView1.Refresh()
        ' Refrescar()

    End Sub


    Public Sub ReasignarSondas()
        Dim canal As AmvDocumentosDN.CanalEntradaDocsDN
        For Each canal In Me.mColCanales

            EncenderApagar(canal, canal.Actividad)

        Next
        Me.DataGridView1.Refresh()


    End Sub

    Private Sub EncenderApagar(ByVal pCanal As AmvDocumentosDN.CanalEntradaDocsDN, ByVal encendido As Boolean)

        pCanal.Actividad = encendido

        ' encender o parar la sonda

        Dim sonda As System.IO.FileSystemWatcher

        sonda = mht.Item(pCanal)

        If sonda Is Nothing AndAlso Not pCanal.Incidentado Then
            Try
                sonda = New System.IO.FileSystemWatcher(pCanal.Ruta)
                '  sonda.NotifyFilter = IO.NotifyFilters.CreationTime

                If mht.ContainsKey(pCanal) Then
                    mht.Item(pCanal) = sonda
                Else
                    mht.Add(pCanal, sonda)
                End If



                RemoveHandler sonda.Created, AddressOf OnFicheroCambia
                AddHandler sonda.Created, AddressOf OnFicheroCambia


            Catch ex As Exception
                pCanal.Incidentado = True
            End Try


        End If

        If Not sonda Is Nothing Then
            sonda.EnableRaisingEvents = pCanal.Actividad
        End If

    End Sub
    Private ReadOnly Property RutaDatosIncidentados() As String
        Get
            Return RutaColaEnvio & "\Incidentados"
        End Get

    End Property
    Private ReadOnly Property RutaDatos() As String
        Get
            Return Application.StartupPath & Me.mDirectorioTemporal
        End Get

    End Property
    Private ReadOnly Property RutaLogEnvio() As String
        Get
            Return Application.StartupPath & Me.mDirectorioTemporal & "\log"
        End Get

    End Property
    Private ReadOnly Property RutaColaEnvio() As String
        Get
            Return Application.StartupPath & Me.mDirectorioTemporal & "\Cola"
        End Get

    End Property

    Public Sub SerializarEstado()

        Dim datos As New FicheroDatosLocales
        datos.ColCanales = Me.mColCanales
        datos.DatosIdentidad = Me.mDatosIdentidad


        FicherosAD.FicherosAD.SerializaraFichero(True, RutaDatos & "\Datos.dat", datos)



    End Sub

    Public Sub RecuperarEstado()
        Try



            ' RecuperarArbolTipoEntidadesNegocio()

            mColCanales = New AmvDocumentosDN.ColCanalEntradaDocsDN


            ' comprobar la existencia del directorio
            Dim di As DirectoryInfo
            di = Directory.CreateDirectory(RutaDatos)

            Dim colfi As IO.FileInfo()
            colfi = di.GetFiles("Datos.dat")

            If colfi.Length > 0 Then

                Dim sb As System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
                sb = New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter

                Dim fs As System.IO.FileStream
                fs = New System.IO.FileStream(RutaDatos & "\" & "Datos.dat", IO.FileMode.Open, FileAccess.Read, FileShare.Read)

                Try
                    Dim miFicheroDatosLocales As FicheroDatosLocales
                    miFicheroDatosLocales = sb.Deserialize(fs)

                    Me.mColCanales = miFicheroDatosLocales.ColCanales
                    Me.mDatosIdentidad = miFicheroDatosLocales.DatosIdentidad

                    fs.Flush()
                Catch ex As Exception
                    fs.Dispose()
                End Try

            End If


            Me.Refrescar()


        Catch ex As Exception
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try


    End Sub

    Public Function RecuperarArbolTipoEntidadesNegocio() As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        Dim csas As ClienteSondaAS.GDocEntrantesAS

        csas = New ClienteSondaAS.GDocEntrantesAS

        Return csas.RecuperarArbolTiposEntNegocio
    End Function

    Private Sub Bloquear()
        Dim c As Control
        mBloqueado = Not mBloqueado

        If mBloqueado Then
            If Not SolicitarAutorizacion() Then
                ' se puede dar un mensaje
                MessageBox.Show("No está autorizado", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If
        End If


        For Each c In Me.Controls
            c.Enabled = mBloqueado
        Next


        If Not Me.btnBloquear1.Enabled Then
            Me.btnBloquear1.Enabled = True
            Me.btnBloquear1.Text = "Desbloquear"
        Else
            Me.btnBloquear1.Text = "Bloquear"
        End If





    End Sub

    Private Function SolicitarAutorizacion() As Boolean
        ' pedir autorizacion apra desbloquear
        Dim fa As AutorizacionFrm
        Dim respuestaAutorizado As Boolean

        Try
            fa = New AutorizacionFrm
            fa.ShowDialog(Me)
            ' llamar al servicio de autorizacion si devuelve false se sale 

            Dim di As DatosIdentidadDN
            di = New DatosIdentidadDN(fa.txtNick.Text, fa.txtClave.Text)
            mDatosIdentidad = di

            Dim csas As ClienteSondaAS.GDocEntrantesAS
            csas = New ClienteSondaAS.GDocEntrantesAS
            respuestaAutorizado = csas.AutorizadoConfigurarClienteSonda(di)

            If respuestaAutorizado Then
                SerializarEstado()
            End If

            fa.Dispose()

            Return respuestaAutorizado

        Catch ex As Exception
            MessageBox.Show(ex.Message, "Error en la autorización", MessageBoxButtons.OK, MessageBoxIcon.Error)
            SolicitarAutorizacion()
        End Try

    End Function

    Private Sub CapturarFicheroParaEnvio(ByVal pRuta As String, ByVal canal As AmvDocumentosDN.CanalEntradaDocsDN)
        'Dim rutadir As String
        'rutadir = ruta.Replace(ruta, "")
        'rutadir = rutadir.Substring(0, rutadir.Length - 1)
        ' sacar el nonbre fichwero




        ' busca el canal que tiene asignado esa ruta
        Dim canalAsociado As AmvDocumentosDN.CanalEntradaDocsDN
        canalAsociado = Me.mColCanales.RecuperarXRuta(IO.Path.GetDirectoryName(pRuta))


        Dim hf As FicherosDN.HuellaFicheroAlmacenadoIODN
        hf = FicherosAD.FicherosAD.CrearHuellaFichero(IO.Path.GetFullPath(pRuta), True)

        Dim miFicheroParaEnvio As New AmvDocumentosDN.FicheroParaAlta
        miFicheroParaEnvio.TipoEntidad = canalAsociado.TipoEntNegocioReferidora
        miFicheroParaEnvio.HuellaNodoTipoEntNegoio = canalAsociado.HuellaNodoTipoEntNegoio
        miFicheroParaEnvio.HuellaFichero = hf
        miFicheroParaEnvio.clanal = canal

        FicherosAD.FicherosAD.SerializaraFichero(True, RutaColaEnvio & "\f" & Now.Ticks.ToString & "_" & System.Guid.NewGuid.ToString() & ".dat", miFicheroParaEnvio)
        FicherosAD.FicherosAD.EliminarFichero(pRuta)


    End Sub




    Public Sub IniciarEnvioFicheros()

        Try
            While True


                ' debe implementarse un control de reintentos
                ' de manera que si el error de envio es porque no se encuentra el servidor no contabilice , pero que si es por culpa del fichero
                ' solo se permita n=4 intentos de envio 



                Dim di As System.IO.DirectoryInfo
                di = System.IO.Directory.CreateDirectory(RutaColaEnvio)

                Dim fi As IO.FileInfo
                For Each fi In di.GetFiles
                    Try
                        EnviarFichero(fi)
                    Catch ex As ThreadAbortException
                        Throw ex
                    Catch ex As Exception
                        'Beep()
                        Debug.WriteLine("el fichero " & fi.Name & " no pudo ser enviado error:" & ex.Message)
                    End Try

                Next




                semaforo.WaitOne()
            End While
        Catch ex As ThreadAbortException
            'nada
        End Try

    End Sub

    Public Sub ArrancarThred()
        hilo = New Thread(AddressOf IniciarEnvioFicheros)
        ' hilo.Prior()
        hilo.Start()
    End Sub
    Public Sub TerminarThred()
        ' hilo = New Thread(AddressOf IniciarEnvioFicheros)
        If Not hilo Is Nothing Then
            hilo.Abort()
        End If
    End Sub
    Public Sub despertarThread()
        semaforo.Set()
    End Sub


    Public Function TrazarErorEnvioFichero(ByVal fi As IO.FileInfo, ByVal pex As Exception)
        Try
            Dim entrada As New ClienteSondaEntradaLog
            entrada.mActor = Me.mDatosIdentidad.Nick
            entrada.mComentario = "fichero:" & fi.Name & " error:" & pex.Message

            Dim filest As IO.FileStream
            filest = New IO.FileStream(RutaLogEnvio & "\" & fi.Name & System.Guid.NewGuid.ToString() & ".xml", IO.FileMode.Create)

            Dim xmlf As System.Xml.Serialization.XmlSerializer
            xmlf = New System.Xml.Serialization.XmlSerializer(GetType(ClienteSondaEntradaLog))
            xmlf.Serialize(filest, entrada)
            filest.Dispose()

        Catch ex As Exception
            Throw
        End Try

    End Function

    Public Sub EnviarFichero(ByVal fi As IO.FileInfo)



        Try

            Dim miFicheroParaEnvio As AmvDocumentosDN.FicheroParaAlta
            miFicheroParaEnvio = FicherosAD.FicherosAD.DesSerializarDesdeFichero(fi.DirectoryName, fi.Name)

            Dim caas As New ClienteSondaAS.GDocEntrantesAS
            miFicheroParaEnvio.HuellaFichero.Nombre = fi.Name
            caas.AltaDocumento(Me.mDatosIdentidad, miFicheroParaEnvio)
            FicherosAD.FicherosAD.EliminarFichero(fi.FullName)

        Catch ex As ClienteSondaAS.ServidorNoEncontradoASException


            Try
                TrazarErorEnvioFichero(fi, ex)

            Catch ex2 As Exception

            End Try

            Throw



        Catch ex As ClienteSondaAS.TamañoExcedidoASException


            Try
                TrazarErorEnvioFichero(fi, ex)

            Catch ex2 As Exception

            End Try

            ' el fichero es demasiado grande y se copia a la carpeta de incidentados
            FicherosAD.FicherosAD.MoverFicheroFichero(fi.FullName, Me.RutaDatosIncidentados)

            ' publicar el fichero como incidentado
            Dim caas As New ClienteSondaAS.GDocEntrantesAS
            Dim dfe As New AmvDocumentosDN.DatosFicheroIncidentado
            dfe.Comentario = fi.FullName & " Maquina:" & My.Computer.Name & " Error: " & ex.Message
            dfe.Fecha = Now
            caas.FicheroIncidentado(Me.mDatosIdentidad, dfe)

            Throw

        Catch ex As Exception

            Try
                TrazarErorEnvioFichero(fi, ex)

            Catch ex2 As Exception

            End Try

            '' el fichero es demasiado grande y se copia a la carpeta de incidentados
            'FN.Ficheros.FicherosAD.FicherosAD.MoverFicheroFichero(fi.FullName, Me.RutaDatosIncidentados)

            '' publicar el fichero como incidentado
            'Dim caas As New ClienteSondaAS.GDocEntrantesAS
            'Dim dfe As New AmvDocumentosDN.DatosFicheroIncidentado
            'dfe.Comentario = fi.FullName & " Maquina:" & My.Computer.Name & " Error: " & ex.Message
            'dfe.Fecha = Now
            'caas.FicheroIncidentado(Me.mDatosIdentidad, dfe)

            Throw
        End Try




    End Sub



#End Region

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        ' porcada sonda
        CapturarFicheroParaEnvioTodosCanalesActivosyEnviar()


    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        CapturarFicheroParaEnvioTodosCanalesActivosyEnviar()
    End Sub

    Private Sub cmdAbrirRuta_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAbrirRuta.Click
        Dim miCanalSelecionado As AmvDocumentosDN.CanalEntradaDocsDN
        Try
            If DataGridView1.SelectedCells.Count > 0 Then
                miCanalSelecionado = DataGridView1.Rows(DataGridView1.SelectedCells(0).RowIndex).DataBoundItem
                Dim pr As Process = System.Diagnostics.Process.Start(miCanalSelecionado.Ruta)
            End If
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try


    End Sub

    Private Sub DataGridView1_CellContentClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles DataGridView1.CellContentClick

    End Sub
End Class


<Serializable()> _
Public Class FicheroDatosLocales
    Public ColCanales As AmvDocumentosDN.ColCanalEntradaDocsDN
    Public DatosIdentidad As DatosIdentidadDN


End Class

<Serializable()> _
Public Class ClienteSondaEntradaLog
    Public mActor As String
    Public mFecha As Date = Date.Now
    Public mComentario As String

End Class