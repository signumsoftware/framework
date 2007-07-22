Imports System.Windows.Forms
Imports AuxIU
Imports MotorBusquedaDN
Imports MotorBusquedabasicasDN

Public Class ctrlFiltro

#Region "atributos"
    Private mFiltro As MotorBusquedaDN.FiltroDN
    Private mControlador As ctrlFiltroctrl
    'Private mEstructura As MotorBusquedaDN.EstructuraVistaDN
    Private mParametroCargaEstructura As ParametroCargaEstructuraDN

    Private mTablaFiltro As DataTable
    Private mListaValores As New List(Of ValorCampo)

#End Region

#Region "eventos"
    Public Event Buscar(ByVal pFiltro As MotorBusquedaDN.FiltroDN)
#End Region

#Region "inicializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = New ctrlFiltroctrl(Me.Marco, Me)

        Me.cboCampo.DisplayMember = "NombreCampo"
        Me.cboOperador.DataSource = [Enum].GetValues(GetType(OperadoresAritmeticos))

        CrearTabla()

    End Sub
#End Region

#Region "propiedades"
    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property ListaValores() As List(Of ValorCampo)
        Get
            Return Me.mListaValores
        End Get
        Set(ByVal value As List(Of ValorCampo))
            Me.mListaValores = value
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property AlternatingBackcolor() As System.Drawing.Color
        Get
            Return Me.DataGridView1.AlternatingRowsDefaultCellStyle.BackColor
        End Get
        Set(ByVal value As System.Drawing.Color)
            Me.DataGridView1.AlternatingRowsDefaultCellStyle.BackColor = value
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property Filtro() As MotorBusquedaDN.FiltroDN
        Get
            If IUaDN() Then
                Return Me.mFiltro
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As MotorBusquedaDN.FiltroDN)
            Me.mFiltro = value
            DNaIU(mFiltro)
        End Set
    End Property

    ''' <summary>
    ''' Sirve para cargar los combos en función de los parámetros estructura
    ''' </summary>
    ''' <value>El parámetro de Carga a partir del cual se van a cargar los valores del filtro</value>
    ''' 
    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public WriteOnly Property ParametroCargaEstructura() As ParametroCargaEstructuraDN
        Set(ByVal value As ParametroCargaEstructuraDN)
            'establecemos el parámetro de carga
            Me.mParametroCargaEstructura = value
            ''recuperamos la estructura y la asignamos al atributo
            ''Me.mEstructura = Me.mControlador.CargarEstructura(value)
            Dim miestr As MotorBusquedaDN.EstructuraVistaDN = Me.mControlador.CargarEstructura(value)
            'establecemos los campos con los que vamos a trabajar
            If miestr IsNot Nothing Then
                Me.cboCampo.DataSource = miestr.ListaCampos  ''mEstructura.ListaCampos

            End If

            ' cargar los datos de las operaciones por las que puedes filtra
            ActualizarOperacionesACargar()

        End Set
    End Property


    Private Sub ActualizarOperacionesACargar()

        Me.cboOperaciones.DataSource = Nothing
        Me.cboOperaciones.Items.Clear()
        Dim principal As Framework.Usuarios.DN.PrincipalDN = Me.Marco.DatosMarco.Item("Principal")
        Dim coloper As Framework.Procesos.ProcesosDN.ColOperacionDN

        coloper = principal.ColOperaciones.RecuperarColxTipoEntidadDN(Me.mParametroCargaEstructura.TipodeEntidad)

        Me.cboOperaciones.DisplayMember = "Nombre"
        Me.cboOperaciones.DataSource = coloper

    End Sub


#End Region

#Region "establecer y rellenar datos"
    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Using New CursorScope(Cursors.WaitCursor)
            Dim mifiltro As MotorBusquedaDN.FiltroDN = CType(pDN, MotorBusquedaDN.FiltroDN)
            If Not mifiltro Is Nothing Then



                For Each mic As MotorBusquedaDN.CondicionDN In mFiltro.condiciones
                    Me.AgregarATabla(mic)
                Next
            End If

        End Using
    End Sub

    Protected Overrides Function IUaDN() As Boolean
        If Me.mFiltro Is Nothing Then
            Me.MensajeError = "No se ha definido ningún filtro"
            Return False
        End If
        Return True
    End Function
#End Region

#Region "métodos"



    ''' <summary>
    ''' crea el filtro de búsqueda de manera automática si tiene los
    ''' datos necesarios, y le añade las condiciones
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub GenerarFiltro()

        ' crear el filtro si no esiste y si existe añadirle las condiciones


        If Me.mFiltro Is Nothing Then
            InicializarFiltro()
        End If


        If Me.mListaValores IsNot Nothing AndAlso Me.mListaValores.Count > 0 Then

            ' añadir las condiciones simples




            For Each miValorenCampo As ValorCampo In Me.mListaValores

                ' recuperrar el campo de la estructra

                Dim micampo As CampoDN = Me.mFiltro.Estructura.mListaCampos.RecuperarxNombreCampo(miValorenCampo.NombreCampo)

                If micampo IsNot Nothing Then ' los campos nothing son ignorados
                    If miValorenCampo.Valor IsNot Nothing Then
                        Dim micondicion As MotorBusquedaDN.CondicionDN
                        micondicion = New MotorBusquedaDN.CondicionDN(micampo, miValorenCampo.Operador, miValorenCampo.Valor, Nothing)
                        micondicion.Eliminable = miValorenCampo.Eliminable
                        Me.mFiltro.condiciones.Add(micondicion)
                        AgregarATabla(micondicion)
                    End If


                Else
                    ' puede ser un campo aportado por la vista sel
                    micampo = New CampoDN()
                    micampo.mtipoCampo = tipocampo.texto ' se podria obtener la estructira de la vista SEL tambien
                    micampo.NombreCampo = miValorenCampo.NombreCampo

                    Dim micondicion As MotorBusquedaDN.CondicionDN
                    micondicion = New MotorBusquedaDN.CondicionDN(micampo, miValorenCampo.Operador, miValorenCampo.Valor, Nothing)
                    micondicion.Eliminable = miValorenCampo.Eliminable
                    Me.mFiltro.condiciones.Add(micondicion)
                    AgregarATabla(micondicion)


                    'Throw New ApplicationException("No se encontro el campo " & miValorenCampo.NombreCampo & "   Estructura:" & Me.mParametroCargaEstructura.NombreVistaVis & " - " & Me.mParametroCargaEstructura.NombreVistaSel)
                End If

            Next

        End If



        RefescarCondicionesEnOperaciones()


    End Sub






    Private Sub cboCampo_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cboCampo.SelectedIndexChanged
        Try
            Me.CargarCampos(CType(Me.cboCampo.SelectedItem, MotorBusquedaDN.CampoDN))
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    ''' <summary>
    ''' Cambia la disposición de los cbos. de valor en función de lo que se haya seleccionado
    ''' </summary>
    Private Sub CargarCampos(ByVal pCampoSeleccionado As MotorBusquedaDN.CampoDN)
        Me.cboValorInicial.Campo = pCampoSeleccionado
        Me.cboValorFinal.Campo = pCampoSeleccionado

        'If Not pCampoSeleccionado Is Nothing AndAlso pCampoSeleccionado.TieneListaValores Then
        '    'tiene valores
        '    cboValorInicial.DataSource = pCampoSeleccionado.mValores.Tables(0)
        '    cboValorInicial.DropDownStyle = ComboBoxStyle.DropDownList
        '    cboValorInicial.DisplayMember = pCampoSeleccionado.NombreCampo

        '    cboValorFinal.DataSource = pCampoSeleccionado.mValores.Tables(0)
        '    cboValorFinal.DropDownStyle = ComboBoxStyle.DropDownList
        '    cboValorFinal.DisplayMember = pCampoSeleccionado.NombreCampo

        'Else
        '    'es nothing o no tiene valores
        '    cboValorInicial.DataSource = Nothing
        '    cboValorInicial.Items.Clear()
        '    cboValorInicial.DropDownStyle = ComboBoxStyle.Simple
        '    cboValorInicial.SelectedText = ""

        '    cboValorFinal.DataSource = Nothing
        '    cboValorFinal.Items.Clear()
        '    cboValorFinal.DropDownStyle = ComboBoxStyle.Simple
        '    cboValorFinal.SelectedText = ""
        'End If
    End Sub

    Private Sub CrearTabla()
        Me.mTablaFiltro = New DataTable

        Dim micolObjeto As New DataColumn("Objeto", GetType(MotorBusquedaDN.CondicionDN))
        Dim micolCampo As New DataColumn("Campo", GetType(String))
        Dim micolOperador As New DataColumn("Operador", GetType(String))
        Dim micolValorInicial As New DataColumn("Valor Inicial", GetType(String))
        ' Dim micolValorFinal As New DataColumn("Valor Final", GetType(String))

        Me.mTablaFiltro.Columns.Add(micolObjeto)
        Me.mTablaFiltro.Columns.Add(micolCampo)
        Me.mTablaFiltro.Columns.Add(micolOperador)
        Me.mTablaFiltro.Columns.Add(micolValorInicial)
        ' Me.mTablaFiltro.Columns.Add(micolValorFinal)

        Me.DataGridView1.DataSource = Me.mTablaFiltro
        'ocultamos la columna objeto
        Me.DataGridView1.Columns(0).Visible = False
        '  Me.DataGridView1.Columns(4).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        Me.DataGridView1.Refresh()
    End Sub

    Private Sub cboOperador_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cboOperador.SelectedIndexChanged
        'YA NO EXISTE EL OPERADOR ENTRE
        'Try
        '    If Me.cboOperador.SelectedItem = MotorBusquedaDN.OperadoresAritmeticos.entre Then
        '        'debe mostrarse el cboValorFinal
        '        Me.lblValorFinal.Visible = True
        '        Me.lblValorInicial.Text = "Valor Inicial"
        '        Me.cboValorFinal.Visible = True
        '    Else
        '        Me.lblValorFinal.Visible = False
        '        Me.lblValorInicial.Text = "Valor"
        '        Me.cboValorFinal.Visible = False
        '    End If
        'Catch ex As Exception
        '    MostrarError(ex, sender)
        'End Try

        ' Me.lblValorFinal.Visible = False
        Me.lblValorInicial.Text = "Valor"
        Me.cboValorFinal.Visible = False

    End Sub

    Private Sub cmdAgregar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAgregar.Click
        Try
            AgregarCondicion()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub EliminarCondicionOperacion()
        Try
            Dim op As Framework.Procesos.ProcesosDN.OperacionDN = Me.lbxOperacionesPosibles.SelectedItem

            If op IsNot Nothing Then
                If Me.mFiltro Is Nothing Then
                    InicializarFiltro()
                End If

                Me.mFiltro.ColOperacionesPosibles.EliminarEntidadDN(op, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)

                RefescarCondicionesEnOperaciones()
            Else
                Advertencia("Debe seleccionar una Operación de la tabla", "Eliminar Operación")
            End If

        Catch ex As Exception
            MostrarError(ex, "Eliminar condición de Operación")
        End Try
    End Sub

    Private Sub RefescarCondicionesEnOperaciones()
        Me.lbxOperacionesPosibles.DataSource = Nothing
        Me.lbxOperacionesPosibles.DisplayMember = "Nombre"
        If Me.mFiltro IsNot Nothing AndAlso Me.mFiltro.ColOperacionesPosibles IsNot Nothing Then
            Me.lbxOperacionesPosibles.DataSource = Me.mFiltro.ColOperacionesPosibles

        End If



    End Sub


    Private Sub AgregarCondicionOperacion()
        Try


            If Me.mFiltro Is Nothing Then
                InicializarFiltro()
            End If

            Me.mFiltro.ColOperacionesPosibles.Add(cboOperaciones.SelectedValue)

            'Me.lbxOperacionesPosibles.DataSource = Nothing
            'Me.lbxOperacionesPosibles.DisplayMember = "Nombre"
            'Me.lbxOperacionesPosibles.DataSource = Me.mFiltro.ColOperacionesPosibles
            RefescarCondicionesEnOperaciones()

        Catch ex As Exception
            MostrarError(ex, "Agregar Condición de Operación")
        End Try
    End Sub
    Private Sub AgregarCondicion()
        Try
            Dim micondicion As MotorBusquedaDN.CondicionDN = Nothing
            Try
                micondicion = New MotorBusquedaDN.CondicionDN(Me.cboCampo.SelectedItem, Me.cboOperador.SelectedValue, Me.cboValorInicial.Valor, Me.cboValorFinal.Valor)
                If micondicion Is Nothing Then
                    Throw New Exception("No se ha creado la Condición")
                End If
            Catch ex As Exception
                Advertencia(ex.Message, "Agregar Condición")
            End Try

            If Me.mFiltro Is Nothing Then
                InicializarFiltro()
            End If

            Me.mFiltro.condiciones.Add(micondicion)
            AgregarATabla(micondicion)
        Catch ex As Exception
            MostrarError(ex, "Agregar Condición")
        End Try
    End Sub

    Public Sub Advertencia(ByVal pMensaje As String, ByVal pTitulo As String)
        MessageBox.Show(pMensaje, pTitulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
    End Sub

    Private Sub AgregarATabla(ByVal pCondicion As MotorBusquedaDN.CondicionDN)
        If Not pCondicion Is Nothing Then
            Dim mir As DataRow = Me.mTablaFiltro.NewRow()
            mir("Objeto") = pCondicion
            mir("Campo") = pCondicion.Campo.NombreCampo
            mir("Operador") = pCondicion.OperadoresArictmetico.ToString
            mir("Valor Inicial") = pCondicion.ValorInicial
            '  mir("Valor Final") = pCondicion.ValorFinal

            Me.mTablaFiltro.Rows.Add(mir)
            Me.DataGridView1.Refresh()
        End If
    End Sub

    Private Sub cmdEliminar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdEliminar.Click
        Try
            EliminarCondicionSeleccionada()
        Catch ex As Exception
            MostrarError(ex, "Eliminar Condición")
        End Try
    End Sub

    Private Sub EliminarCondicionSeleccionada()
        Using New CursorScope(Cursors.WaitCursor)
            If Me.DataGridView1.SelectedRows.Count <> 0 Then
                '1 encontramos la condición que se quiere eliminar
                For Each mir As DataGridViewRow In Me.DataGridView1.SelectedRows
                    Dim micondicion As MotorBusquedaDN.CondicionDN = CType(mir.Cells("Objeto").Value, MotorBusquedaDN.CondicionDN)

                    ' si no es eliminable nos largamos
                    If Not micondicion.Eliminable Then
                        Exit Sub
                    End If

                    '2 eliminamos la condición del filtro
                    Me.mFiltro.condiciones.Remove(micondicion)
                    '3 eliminamos la condicion de la Tabla
                    Dim dr_del As DataRow = Nothing
                    For Each midr As DataRow In Me.mTablaFiltro.Rows
                        If midr("Objeto") Is micondicion Then
                            dr_del = midr
                            Exit For
                        End If
                    Next
                    Me.mTablaFiltro.Rows.Remove(dr_del)
                Next
                '4 refrescamos el datagridview
                Me.DataGridView1.Refresh()
            Else
                Advertencia("Debe seleccionar una Condición de la tabla", "Eliminar Condición")
            End If
        End Using
    End Sub

    Private Sub DataGridView1_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles DataGridView1.KeyUp
        Try
            If e.KeyCode = Keys.Back OrElse e.KeyCode = Keys.Delete Then
                EliminarCondicionSeleccionada()
            End If
        Catch ex As Exception
            MostrarError(ex, "Eliminar Condición")
        End Try
    End Sub

    Private Sub cmdEliminarTodos_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdEliminarTodos.Click
        EliminarTodasCondiciones()

    End Sub


    Private Sub ElimianrCondicionesEliminables()


        For Each micondicion As MotorBusquedaDN.CondicionDN In mFiltro.condiciones.ToArray

            ' si no es eliminable nos largamos
            If micondicion.Eliminable Then


                '2 eliminamos la condición del filtro
                Me.mFiltro.condiciones.Remove(micondicion)
                '3 eliminamos la condicion de la Tabla
                Dim dr_del As DataRow = Nothing
                For Each midr As DataRow In Me.mTablaFiltro.Rows
                    If midr("Objeto") Is micondicion Then
                        dr_del = midr
                    End If
                Next
                Me.mTablaFiltro.Rows.Remove(dr_del)

            End If


        Next




        '4 refrescamos el datagridview
        Me.DataGridView1.Refresh()
    End Sub


    Public Sub EliminarTodasCondiciones()
        Try
            If Not Me.mFiltro Is Nothing Then
                Me.mFiltro.ColOperacionesPosibles.Clear()
                Me.lbxOperacionesPosibles.DataSource = Nothing

                ElimianrCondicionesEliminables()


            End If
        Catch ex As Exception
            MostrarError(ex, "Eliminar Condición")
        End Try
    End Sub

    Private Sub cmdBuscar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdBuscar.Click
        Try
            If Me.mFiltro Is Nothing Then
                InicializarFiltro()
            End If

            RaiseEvent Buscar(Me.mFiltro)

        Catch ex As Exception
            MostrarError(ex, "Realizar búsqueda")
        End Try
    End Sub

    Private Sub DataGridView1_SelectionChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles DataGridView1.SelectionChanged
        Try
            Me.cmdEliminar.Enabled = (Me.DataGridView1.SelectedRows.Count <> 0)
        Catch ex As Exception
            MostrarError(ex, "Selección")
        End Try
    End Sub

    Private Sub InicializarFiltro()
        mFiltro = New MotorBusquedaDN.FiltroDN()
        mFiltro.NombreVistaSel = mParametroCargaEstructura.NombreVistaSel
        Me.mFiltro.NombreVistaVis = mParametroCargaEstructura.NombreVistaVis
        Me.mFiltro.ConsultaSQL = mParametroCargaEstructura.ConsultaSQL
        Me.mFiltro.ColOperacionesPosibles = mParametroCargaEstructura.ColOperacion
        Me.mFiltro.Estructura = Me.mControlador.CargarEstructura(mParametroCargaEstructura)
        Me.mFiltro.PropiedadDeInstancia = mParametroCargaEstructura.PropiedadDeInstancia
        Me.mFiltro.TipoReferido = mParametroCargaEstructura.TipodeEntidad

        If Me.mListaValores Is Nothing Then
            Me.mListaValores = mParametroCargaEstructura.ListaValores

        Else
            'añadir los valores sin duplicarlos

            If mParametroCargaEstructura IsNot Nothing AndAlso mParametroCargaEstructura.ListaValores IsNot Nothing Then

                For Each vc As ValorCampo In mParametroCargaEstructura.ListaValores
                    Dim encontrado As Boolean
                    For Each vc2 As ValorCampo In mListaValores
                        If vc2.NombreCampo = vc.NombreCampo AndAlso vc2.Operador = vc.Operador AndAlso vc2.Valor = vc.Valor Then
                            encontrado = True
                        End If
                    Next
                    If Not encontrado Then
                        mListaValores.Add(vc)
                    End If
                Next

            End If

        End If
    End Sub

#End Region





    Private Sub cmdAgregarCondOperacion_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAgregarCondOperacion.Click
        Try
            Me.AgregarCondicionOperacion()
        Catch ex As Exception
            MostrarError(ex, "Eliminar Condición")
        End Try
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Try
            EliminarCondicionOperacion()
        Catch ex As Exception
            MostrarError(ex, "Eliminar Condición")
        End Try
    End Sub
End Class
