Imports System.Windows.Forms
Imports AuxIU
Imports MotorBusquedaDN
Imports MotorBusquedabasicasDN
Imports Framework.IU.IUComun

Public Class ctrlBuscadorGenerico2

#Region "atributos"
    Private mParametroCargaEstructura As ParametroCargaEstructuraDN
    Private mControlador As ctrlBuscadorGenerico2ctrl
    Private mTipoNavegacion As TipoNavegacion
    Private mEnviarDatatableAlNavegar As Boolean
    Private mEjecutarOperacion As Boolean = False
    '  Private mFiltroVisible As Boolean = True
    'Private mFiltrable As Boolean

#End Region

#Region "inicializador"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        Me.mControlador = New ctrlBuscadorGenerico2ctrl(Me.Marco, Me)


    End Sub
#End Region

#Region "propiedades"

    'Public ReadOnly Property DataGridViewXT() As ControlesPGenericos.DataGridViewXT
    '    Get

    '    End Get
    'End Property

    <System.ComponentModel.DefaultValue("listado")> _
    Public Property TituloLsitado() As String
        Get

            Return Me.DataGridViewXT1.TituloListado
        End Get
        Set(ByVal value As String)
            ' mFiltrable = value
            Me.DataGridViewXT1.TituloListado = value
        End Set
    End Property

    <System.ComponentModel.DefaultValue(True)> _
    Public Property Filtrable() As Boolean
        Get
            '  Return mFiltrable
            Return Me.DataGridViewXT1.Filtrable
        End Get
        Set(ByVal value As Boolean)
            ' mFiltrable = value
            Me.DataGridViewXT1.Filtrable = value
        End Set
    End Property


    <System.ComponentModel.DefaultValue(True)> _
    Public Property FiltroVisible() As Boolean
        Get
            Return Me.SplitContainer1.Panel1Collapsed
        End Get
        Set(ByVal value As Boolean)
            'Me.mFiltroVisible = value
            Me.SplitContainer1.Panel1Collapsed = Not value

        End Set
    End Property

    ''' <summary>
    ''' Determina si estamos en un buscador para ejecutar operaciones
    ''' </summary>
    ''' <remarks></remarks>
    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property EjecutarOperacion() As Boolean
        Get
            Return Me.mEjecutarOperacion
        End Get
        Set(ByVal value As Boolean)
            Me.mEjecutarOperacion = value
            'si hay que ejecutar operaciones debe ser navegable
            If Me.mEjecutarOperacion AndAlso (Not Navegable) Then
                Navegable = True
            End If
        End Set
    End Property


    ''' <summary>
    ''' Una lista de Condición-Valor para generar las condiciones del filtro
    ''' </summary>
    ''' <value>Lista de Valores de Campo</value>
    ''' <returns>Lista de Valores de Campo</returns>
    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property ListaValores() As List(Of ValorCampo)
        Get
            Return Me.ctrlFiltro.ListaValores
        End Get
        Set(ByVal value As List(Of ValorCampo))
            Me.ctrlFiltro.ListaValores = value
        End Set
    End Property


    ''' <summary>
    ''' Establece u Obtiene si se va apoder navegar al elemento/s
    ''' seleccionado/s en la lista de resultados
    ''' </summary>
    Public Property Navegable() As Boolean
        Get
            Return Me.DataGridViewXT1.Navegable
        End Get
        Set(ByVal value As Boolean)
            Me.DataGridViewXT1.Navegable = value
        End Set
    End Property

    ''' <summary>
    ''' Establece u Obtiene el backcolor del pijama para el dataset del filtro
    ''' </summary>
    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property AlternatingBackcolorFiltro() As System.Drawing.Color
        Get
            Return Me.ctrlFiltro.AlternatingBackcolor
        End Get
        Set(ByVal value As System.Drawing.Color)
            Me.ctrlFiltro.AlternatingBackcolor = value
        End Set
    End Property

    ''' <summary>
    ''' Establece u Obtiene el backcolor del pijama para el dataset de resultados
    ''' </summary>
    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property AlternatingBackcolorResultados() As System.Drawing.Color
        Get
            Return Me.DataGridViewXT1.DatagridView.AlternatingRowsDefaultCellStyle.BackColor
        End Get
        Set(ByVal value As System.Drawing.Color)
            Me.DataGridViewXT1.DatagridView.AlternatingRowsDefaultCellStyle.BackColor = value
        End Set
    End Property

    ''' <summary>
    ''' Determina u Obtiene si se puede/n eliminar elemento/s de la lista
    ''' </summary>
    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property Eliminable() As Boolean
        Get
            Return Me.DataGridViewXT1.Eliminable
        End Get
        Set(ByVal value As Boolean)
            Me.DataGridViewXT1.Eliminable = value
        End Set
    End Property

    ''' <summary>
    ''' Determina u Obtiene si se puede/n agregar elemento/s a la lista
    ''' </summary>
    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property Agregable() As Boolean
        Get
            Return Me.DataGridViewXT1.Agregable
        End Get
        Set(ByVal value As Boolean)
            Me.DataGridViewXT1.Agregable = value
        End Set
    End Property

    ''' <summary>
    ''' Propiedad que devuelve transparentemente el filtro del ctrlFiltro
    ''' </summary>
    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property Filtro() As MotorBusquedaDN.FiltroDN
        Get
            Return Me.ctrlFiltro.Filtro
        End Get
        Set(ByVal value As MotorBusquedaDN.FiltroDN)
            Me.ctrlFiltro.Filtro = value
        End Set
    End Property

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property ParametroCargaEstructura() As ParametroCargaEstructuraDN
        Get
            Return mParametroCargaEstructura
        End Get
        Set(ByVal value As ParametroCargaEstructuraDN)
            mParametroCargaEstructura = value
            Me.ctrlFiltro.ParametroCargaEstructura = mParametroCargaEstructura
        End Set
    End Property

    <System.ComponentModel.DefaultValue(TipoNavegacion.Modal)> _
    Public Property TipoNavegacion() As TipoNavegacion
        Get
            Return Me.mTipoNavegacion
        End Get
        Set(ByVal value As TipoNavegacion)
            Me.mTipoNavegacion = value
        End Set
    End Property

    ''' <summary>
    ''' Determina la propiedad de selección del listado
    ''' </summary>
    <System.ComponentModel.DefaultValue(False)> _
    Public Property MultiSelect() As Boolean
        Get
            Return Me.DataGridViewXT1.DatagridView.MultiSelect
        End Get
        Set(ByVal value As Boolean)
            Me.DataGridViewXT1.DatagridView.MultiSelect = value
        End Set
    End Property

    ''' <summary>
    ''' Determina si cuando se navege se envía el Dataset fuente de datos o no dentro del paquete
    ''' </summary>
    <System.ComponentModel.DefaultValue(False)> _
    Public Property EnviarDatatableAlNavegar() As Boolean
        Get
            Return Me.mEnviarDatatableAlNavegar
        End Get
        Set(ByVal value As Boolean)
            mEnviarDatatableAlNavegar = value
        End Set
    End Property


    Public ReadOnly Property DataGridViewXT() As ControlesPGenericos.DataGridViewXT
        Get
            Return Me.DataGridViewXT1
        End Get
    End Property
    Public ReadOnly Property DataGridView() As DataGridView
        Get
            Return Me.DataGridViewXT1.DatagridView
        End Get
    End Property
#End Region

#Region "métodos"
    ''' <summary>
    ''' Le dice al control filtro que, si tiene los datos necesarios,
    ''' genere un filtro de búsqueda automáticamente
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub GenerarFiltro()
        Me.ctrlFiltro.GenerarFiltro()
    End Sub
#End Region



#Region "Manejadores de eventos"

    Public Function buscar() As Boolean
        buscar(Me.ctrlFiltro.Filtro)

    End Function



    Private Sub Buscar(ByVal pFiltro As MotorBusquedaDN.FiltroDN)
        Try
            Using New CursorScope(Cursors.WaitCursor)
                'limpiamos el datasource del listado
                Me.DataGridViewXT1.DatagridView.DataSource = Nothing

                Dim ds As DataSet = Me.mControlador.RealizarBusqueda(pFiltro)

                If ds Is Nothing AndAlso ds.Tables.Count <> 0 Then
                    MessageBox.Show("No se han encontrado resultados para la búsqueda", "Buscar", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Exit Sub
                End If

                Me.DataGridViewXT1.DatagridView.DataSource = ds.Tables(0)
                'hacemos invisible la 1ª columna, que es la que lleva el ID por el que se va a identificar a la unidad
                'Me.DataGridViewXT1.DatagridView.Columns(0).Visible = False

                Me.DataGridViewXT1.Refresh()
            End Using
        Catch ex As Exception
            MostrarError(ex, "Error")
        End Try
    End Sub

    Private Sub ctrlFiltro_Buscar(ByVal pFiltro As MotorBusquedaDN.FiltroDN) Handles ctrlFiltro.Buscar


        buscar(pFiltro)
    End Sub


#Region "artefactos de la ejecución de operaciones"
    Private mFormularioEjecucion As ProcesosIU.frmEjecutarOperacion

    ''' <summary>
    ''' Realiza la navegación o el agregado de datos para la ejecución de la 
    ''' operación seleccionada
    ''' </summary>
    Private Overloads Sub NavegarAEjecutarOperacion(ByVal pSelectedRows As DataGridViewRowCollection)

        'comprobamos si ya se está ejecutando el proceso 
        'o debe cerrar la ventana de resultados
        If Not Me.mFormularioEjecucion Is Nothing Then
            Select Case Me.mFormularioEjecucion.EstadoEjecucion
                Case ProcesosIU.EstadoEjecucion.enproceso
                    MessageBox.Show("La ejecución de la operación solicitada se está realizando. Hasta que termine no se pueden ejecutar nuevas operaciones.", "Operación en proceso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    Exit Sub
                Case ProcesosIU.EstadoEjecucion.terminado
                    MessageBox.Show("Se ha completado el proceso de ejecución anterior. Debe cerrar la ventana de resultados para poder ejecutar nuevas operaciones.", "Operación Anterior", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    Exit Sub
            End Select
        End If


        '1 --> creamos el datatable con la misma estructura que el datatble que forma
        'el datasource del datagridview
        Dim midt As New DataTable
        Dim dtorigen As DataTable = CType(Me.DataGridViewXT1.DatagridView.DataSource, DataTable)

        For Each c As DataColumn In dtorigen.Columns
            Dim mic As New DataColumn(c.ColumnName, c.DataType)
            midt.Columns.Add(mic)
        Next

        'ahora le metemos las columnas que se han seleccionado

        For Each sr As DataGridViewRow In pSelectedRows
            Dim nr As DataRow = midt.NewRow
            For Each mic As DataColumn In midt.Columns
                nr(0) = sr.Cells(0).Value
            Next
            midt.Rows.Add(nr)
        Next

        '--> ya tenemos el datatable con los datos que se van a ejecutar


        '2 --> comprobamos si se trata de navegar o de agregar los datos a ejecutar
        If Me.mFormularioEjecucion Is Nothing Then
            'hay que navegar

            Dim mipaquete As New ProcesosIU.PaqueteFormularioEjecutarOperacion
            mipaquete.DataTableDatos = midt
            mipaquete.TransicionAEjecutar = Nothing 'TODO: luis - 777 falta por poner la Transición
            mipaquete.TipoObjeto = Nothing 'TODO: luis - 777 falta por poner el Tipo

            'TODO: luis - 777 falta por definir el nombre de la función
            Me.Marco.Navegar("", Me.ParentForm, Me.ParentForm.MdiParent, TipoNavegacion.Normal, Me.GenerarDatosCarga, mipaquete.GenerarPaquete, Me.mFormularioEjecucion)

            'asociamos el evento de operacion finalizada a nuestro método
            AddHandler Me.mFormularioEjecucion.OperacionTerminada, AddressOf EscucharResultado

        Else
            'hay que agregar/actualizar los datos
            Me.mFormularioEjecucion.CargarDatos(midt)
        End If

    End Sub

    Private Overloads Sub NavegarAEjecutarOperacion(ByVal pSelectedRow As DataGridViewRow)
        'metemos el selectedrow en una colección y lo pasamos a la sobrecarga
        Dim misr As New DataGridViewRowCollection(Me.DataGridViewXT1.DatagridView)
        misr.Add(pSelectedRow)
        Me.NavegarAEjecutarOperacion(pSelectedRow)
    End Sub

    Private Sub EscucharResultado(ByVal sender As Form, ByVal resultado As ProcesosIU.ResultadoEjecucion)
        'liberamos la referencia al formulario de ejecucion
        Me.mFormularioEjecucion = Nothing

        'mostramos un mensaje con los datos de resultado
        Dim mensaje As String = String.Empty
        Dim icono As System.Windows.Forms.MessageBoxIcon
        Select Case resultado
            Case ProcesosIU.ResultadoEjecucion.cancelada
                mensaje = "La operación fue cancelada por el usuario antes de ralizar ningún proceso."
                icono = MessageBoxIcon.Exclamation
            Case ProcesosIU.ResultadoEjecucion.errores
                mensaje = "La operación fue completada pero se produjo algún error en el proceso."
                icono = MessageBoxIcon.Exclamation
            Case ProcesosIU.ResultadoEjecucion.exito
                mensaje = "La operación fue completada con éxito."
                icono = MessageBoxIcon.Information
        End Select

        MessageBox.Show(mensaje, "Operación completada", MessageBoxButtons.OK, icono)

        'refrescamos la búsqueda
        'TODO: luis - 777 falta volver a ejecutar la búsqueda
    End Sub



#End Region

    ''' <summary>
    ''' Recibe un paquete, y él termina de conformar los datos del paquete y realiza la acción
    ''' de navegación
    ''' </summary>
    ''' <param name="paquete">El paquete ya instanciado para navegar</param>
    Private Sub Navegacion(ByVal paquete As Hashtable, ByVal agregando As Boolean)

        'Si nos han llamado modalmente, lo único que hacemos es cerrarnos
        'para que el frmP que nos llamó reciba los datos en el paquete
        If Me.ParentForm.Modal AndAlso Not agregando Then
            Me.ParentForm.Close()
        Else

            Dim destino As String = Me.mParametroCargaEstructura.DestinoNavegacion

            ' si no se establece ningun destino por defecto el destino es el formulario generico
            If String.IsNullOrEmpty(destino) Then
                destino = "FG"
            End If

            ' es posible que si no se definió un destino de navegacion se haya definido un mapeado de visibilidad para el formulario generico que es
            ' el destino de navegacion por defecto
            If Not String.IsNullOrEmpty(Me.mParametroCargaEstructura.NombreInstanciaMapVis) Then
                paquete.Add("NombreInstanciaMapVis", Me.mParametroCargaEstructura.NombreInstanciaMapVis)
            End If

            'si tenemos que enviar el datatble al navegar, lo metemos en el paquete
            If Me.mEnviarDatatableAlNavegar Then
                paquete.Add("DataTable", CType(Me.DataGridViewXT1.DatagridView.DataSource, DataTable))
            End If

            'metemos el TipoEntidad 
            If (Not Me.mParametroCargaEstructura Is Nothing) AndAlso (Not Me.mParametroCargaEstructura.TipodeEntidad Is Nothing) Then
                If paquete.ContainsKey("TipoEntidad") Then
                    paquete("TipoEntidad") = Me.mParametroCargaEstructura.TipodeEntidad
                Else
                    paquete.Add("TipoEntidad", Me.mParametroCargaEstructura.TipodeEntidad)

                End If
            End If

            ' metemos la entidad referidora
            If (Not Me.mParametroCargaEstructura Is Nothing) AndAlso (Not Me.mParametroCargaEstructura.EntidadReferidora Is Nothing) Then
                If paquete.ContainsKey("EntidadReferidora") Then
                    paquete("EntidadReferidora") = Me.mParametroCargaEstructura.EntidadReferidora
                Else
                    paquete.Add("EntidadReferidora", Me.mParametroCargaEstructura.EntidadReferidora)

                End If
            End If

            ' metemos la poropiedad referifora
            If (Not Me.mParametroCargaEstructura Is Nothing) AndAlso (Not Me.mParametroCargaEstructura.EntidadReferidora Is Nothing) Then
                If paquete.ContainsKey("PropiedadReferidora") Then
                    paquete("PropiedadReferidora") = Me.mParametroCargaEstructura.PropiedadReferidora
                Else
                    paquete.Add("PropiedadReferidora", Me.mParametroCargaEstructura.PropiedadReferidora)
                End If
            End If



            If Not agregando Then
                'navegamos en función del tipo de navegación que nos hayan definido
                Me.Marco.Navegar(destino, Me.ParentForm, Me.ParentForm.MdiParent, Me.mTipoNavegacion, Me.mControlador.ControladorForm.FormularioContenedor.GenerarDatosCarga, paquete)
            Else
                'hacemos la navegación para agregar

                If Me.ParentForm.Modal Then
                    Me.Marco.Navegar(destino, Me.ParentForm, Nothing, TipoNavegacion.Modal, Me.GenerarDatosCarga, paquete)
                    If Not paquete Is Nothing AndAlso paquete.Contains("DN") Then
                        'metemos el resultado en el paquete de nuestro padre
                        Dim mipadre As MotorIU.FormulariosP.FormularioBase = Me.ParentForm
                        mipadre.Paquete.Add("DN", paquete("DN"))

                        'cerramos a nuestro padre
                        mipadre.Close()
                    End If
                Else
                    Me.Marco.Navegar(destino, Me.ParentForm, Me.ParentForm.MdiParent, TipoNavegacion.Normal, Me.GenerarDatosCarga, paquete)
                End If
            End If
        End If


    End Sub


    Public Sub Navegar(ByVal SelectedRow As System.Windows.Forms.DataGridViewRow)
        Try

            '--- si está ejecutando operación
            If Me.mEjecutarOperacion Then
                Me.NavegarAEjecutarOperacion(SelectedRow)
                Exit Sub
            End If
            '---

            Dim identidad As String = CType(String.Empty & SelectedRow.Cells(0).Value, String)

            Dim mipaquete As Hashtable

            If Me.ParentForm.Modal Then
                mipaquete = CType(Me.ParentForm, MotorIU.FormulariosP.IFormularioP).Paquete
            Else
                mipaquete = New Hashtable
            End If

            mipaquete.Add("ID", identidad)
            mipaquete.Add("TipoEntidad", mParametroCargaEstructura.TipodeEntidad)

            Me.Navegacion(mipaquete, False)

        Catch ex As Exception
            MostrarError(ex, "Navegar")
        End Try
    End Sub

    Private Sub DataGridViewXT1_MostrarFiltro() Handles DataGridViewXT1.MostrarFiltro

        'If Me.mFiltrable Then
        '    Me.ctrlFiltro.Visible = Not Me.ctrlFiltro.Visible
        'Else
        '    Me.ctrlFiltro.Visible = False
        '    Me.DataGridViewXT1.Filtrable = mFiltrable
        'End If
        Dim visible As Boolean = Not Me.ctrlFiltro.Visible
        Me.ctrlFiltro.Visible = visible
        Me.SplitContainer1.Panel1Collapsed = Not visible

    End Sub

    Private Sub DataGridViewXT1_Navegar(ByVal SelectedRow As System.Windows.Forms.DataGridViewRow) Handles DataGridViewXT1.Navegar
        Navegar(SelectedRow)
    End Sub

    Private Sub DataGridViewXT1_NavegarMultiple(ByVal SelectedRows As System.Windows.Forms.DataGridViewSelectedRowCollection) Handles DataGridViewXT1.NavegarMultiple
        Try

            '--- si está ejecutando operación
            If Me.mEjecutarOperacion Then
                Dim rc As New DataGridViewRowCollection(Me.DataGridViewXT1.DatagridView)
                For Each dr As DataGridViewRow In SelectedRows
                    rc.Add(dr)
                Next
                Me.NavegarAEjecutarOperacion(rc)
                Exit Sub
            End If
            '---

            Dim mipaquete As Hashtable

            If Me.ParentForm.Modal Then
                mipaquete = CType(Me.ParentForm, MotorIU.FormulariosP.IFormularioP).Paquete
            Else
                mipaquete = New Hashtable
            End If

            Dim identidad As New List(Of String)

            For Each mir As DataGridViewRow In SelectedRows
                identidad.Add(CType(String.Empty & mir.Cells(0).Value, String))
            Next

            mipaquete.Add("IDMultiple", identidad)

            Me.Navegacion(mipaquete, False)
        Catch ex As Exception
            MostrarError(ex, "Navegar")
        End Try
    End Sub

    Private Sub DataGridViewXT1_Agregar() Handles DataGridViewXT1.Agregar
        Try
            Dim mipaquete As New Hashtable

            Me.Navegacion(mipaquete, True)
        Catch ex As Exception
            MostrarError(ex, "Agregar")
        End Try
    End Sub

#End Region


   
 
 


    Private Sub DataGridViewXT1_Refrescar() Handles DataGridViewXT1.Refrescar
        Me.buscar()
    End Sub
End Class
