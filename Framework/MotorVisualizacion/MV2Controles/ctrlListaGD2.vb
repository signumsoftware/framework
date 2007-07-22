Imports Framework.iu.iucomun
Imports MV2ControlesBasico

Public Class ctrlListaGD2


    Implements MV2Controles.IctrlDinamico



    Implements MV2DN.IRecuperadorInstanciaMap


#Region "Atributos"
    Protected mRecuperadorMap As MV2DN.IRecuperadorInstanciaMap
    Private mPropertyVincPrincipal As MV2DN.PropVinc

    Private mDataTable As New DataTable

    Private mColPropVincMoldeColumna As MV2DN.ColPropVinc

    Dim miInstanciaVincMoldeFila As MV2DN.InstanciaVinc
    Private WithEvents mctrlBarraBotonesGD As MV2ControlesBasico.ctrlBarraBotonesGD
    Protected mComandoInstancia As MV2DN.ComandoInstancia
#End Region

#Region "Constructores"
    Public Sub New()

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
    End Sub

    Public Sub New(ByVal pPropVinc As MV2DN.PropVinc)
        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        Me.mPropertyVincPrincipal = pPropVinc
        ' Me.Poblar()
    End Sub
#End Region

#Region "Propiedades"
    Public Property DatosControl() As String Implements IctrlDinamico.DatosControl
        Get

        End Get
        Set(ByVal value As String)

        End Set
    End Property
#End Region

#Region "Establecer y Rellenar Datos"

    Public Function RecuperarControlDinamico(ByVal pElementoMap As MV2DN.ElementoMapDN) As IctrlDinamico Implements IctrlDinamico.RecuperarControlDinamico


        'For Each cd As IctrlDinamico In Me.TableLayoutPanel1.Controls

        '    If cd.ElementoVinc.ElementoMap Is pElementoMap Then
        '        Return cd
        '    End If

        'Next
        Return Nothing

    End Function

    Public Function FilaSeleccioanda() As Data.DataRow
        If Me.DataGridView1.SelectedRows.Count > 0 Then
            Return CType(Me.DataGridView1.SelectedRows(0).DataBoundItem, System.Data.DataRowView).Row
        Else
            Return Nothing
        End If
    End Function
    Public Function EntidadSeleccioanda() As Framework.DatosNegocio.IEntidadBaseDN

        Dim mir As DataRow = FilaSeleccioanda()
        If mir Is Nothing Then
            Return Nothing
        Else
            Return mir("$Instancia")
        End If

    End Function
    Private Sub mctrlBarraBotonesGD_ComandoSolicitado(ByVal sender As Object, ByVal e As System.EventArgs) Handles mctrlBarraBotonesGD.ComandoSolicitado



        ' solo se puede operar si  tu instancia vin esta vinculada a un objeto y ella su vez a una propiedad
        mComandoInstancia = mctrlBarraBotonesGD.ComandoAccioando

        Dim mictrlBarraBotonesGD As MV2ControlesBasico.ctrlBarraBotonesGD = sender


        If Me.mPropertyVincPrincipal.Vinculada Then



            If mictrlBarraBotonesGD.ComandoAccioando.Map.EsComandoBasico Then


                Select Case mictrlBarraBotonesGD.ComandoAccioando.Map.ComandoBasico

                    Case MV2DN.ComandosMapBasicos.NavegarEditar

                        'Dim autorizado As Boolean = True
                        'RaiseEvent ComandoSolicitado(Me, autorizado)
                        'If autorizado Then

                        '    Throw New NotImplementedException()
                        '    ' TODO: navergar de modo modal 
                        '    If Me.mPropertyVincPrincipal.RepresentaTipoPorReferencia Then
                        '        Me.mPropertyVincPrincipal.ValueTipoRepresentado = Nothing
                        '    Else
                        '        Me.mPropertyVincPrincipal.Value = Nothing
                        '    End If
                        '    RaiseEvent ComandoEjecutado(Me, Nothing)
                        'End If




                        If Me.mPropertyVincPrincipal.Map.EsNavegable Then
                            Dim autorizado As Boolean = True
                            Dim midnSelecioand As Framework.DatosNegocio.IEntidadBaseDN = EntidadSeleccioanda()
                            If midnSelecioand IsNot Nothing Then
                                RaiseEvent ComandoSolicitado(Me, autorizado)
                                If autorizado Then

                                    ' crear el paquete
                                    Dim paquete As New Hashtable


                                    If Not Me.mPropertyVincPrincipal.Eseditable Then
                                        paquete.Add("Editable", Me.mPropertyVincPrincipal.Eseditable)
                                    End If


                                    paquete.Add("NombreInstanciaMapVis", Me.mPropertyVincPrincipal.Map.DatosNavegacion)
                                    paquete.Add("DN", midnSelecioand)

                                    Me.Marco.Navegar("FG", Me.ParentForm, Nothing, MotorIU.Motor.TipoNavegacion.Modal, Me.GenerarDatosCarga, paquete)
                                    'si la entidad contenida se trata de una huella habria que pedir que referecara sus datos
                                    If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(CType(midnSelecioand, Object).GetType) AndAlso paquete IsNot Nothing AndAlso paquete.Contains("DN") AndAlso paquete.Item("DN") IsNot Nothing Then
                                        Dim h As Framework.DatosNegocio.IHuellaEntidadDN = midnSelecioand
                                        h.AsignarEntidadReferida(paquete.Item("DN"))
                                    End If

                                    Me.Poblar()
                                    Me.DNaIUgd()
                                    RaiseEvent ComandoEjecutado(Me, Nothing)
                                End If

                            End If

                        End If



                    Case MV2DN.ComandosMapBasicos.Crear

                        If Me.mPropertyVincPrincipal.Map.Instanciable Then
                            Dim autorizado As Boolean = True
                            RaiseEvent ComandoSolicitado(Me, autorizado)


                            Dim col As IList
                            col = Me.mPropertyVincPrincipal.Value
                            If col Is Nothing Then
                                col = Activator.CreateInstance(Me.mPropertyVincPrincipal.TipoPropiedad)
                                mPropertyVincPrincipal.Value = col
                            Else
                                ' aqui hay que poder seleccionar la entidad si es multiple
                                col.Add(MV2Controles.MV2ControlesHelper.CrearInstancia(Me))

                            End If
                            RaiseEvent ComandoEjecutado(Me, Nothing)


                        End If


                    Case MV2DN.ComandosMapBasicos.Buscar

                        Dim autorizado As Boolean = True
                        RaiseEvent ComandoSolicitado(Me, autorizado)
                        If autorizado Then
                            'Me.Marco.Navegar("", Me.ParentForm, Me.ParentForm, MotorIU.Motor.TipoNavegacion.Modal, CType(Me.ParentForm, MotorIU.FormulariosP.IFormularioP).Datos, Nothing)

                            Dim colEntidad As New Framework.DatosNegocio.ArrayListValidable(Of Framework.DatosNegocio.IEntidadBaseDN)
                            colEntidad.AddRange(MV2ControlesHelper.BuscarCol(Me))

                            ' tomar referencia a la col destino
                            Dim col As IList
                            col = Me.mPropertyVincPrincipal.Value

                            ' crear la coleccion si no estubiera ya instanciada
                            If col Is Nothing Then
                                col = Activator.CreateInstance(Me.mPropertyVincPrincipal.TipoPropiedad)
                                mPropertyVincPrincipal.Value = col
                            End If


                            ' si la col es de huellas como el buscador nos devulve HUELLASS de entidades añadimos directamente en la coleccion

                            Dim tipoFijadocol As System.Type = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(Me.mPropertyVincPrincipal.TipoPropiedad, Nothing)

                            If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(tipoFijadocol) Then

                                ' es una col de huellas y lo que yo tengo es una col de entidades luego he de crear huellas asociarlas y añadirlas

                                Dim huellaConcreta As Framework.DatosNegocio.IHuellaEntidadDN = Activator.CreateInstance(tipoFijadocol)

                                For Each entidad As Framework.DatosNegocio.IEntidadBaseDN In colEntidad
                                    huellaConcreta = Activator.CreateInstance(tipoFijadocol)
                                    huellaConcreta.AsignarEntidadReferida(entidad)

                                    col.Add(huellaConcreta)
                                Next

                            Else
                                ' es una col de entidades como el buscador devuelve huellas obtenemos las entidades a partir de las huellas
                                For Each enidad As Framework.DatosNegocio.IEntidadBaseDN In colEntidad
                                    If TypeOf enidad Is Framework.DatosNegocio.IHuellaEntidadDN Then

                                        Dim huella As Framework.DatosNegocio.IHuellaEntidadDN = enidad
                                        col.Add(huella.EntidadReferida)
                                    Else
                                        col.Add(enidad)
                                    End If
                                Next

                            End If

                            RaiseEvent ComandoEjecutado(Me, Nothing)
                        End If

                    Case MV2DN.ComandosMapBasicos.NoReferir

                        Dim autorizado As Boolean = True
                        Dim mir As Windows.Forms.DataGridViewRow
                        ' Dim dgvr As Windows.Forms.DataGridViewRow

                        Dim coleliminados As New ArrayList

                        For Each mir In Me.DataGridView1.SelectedRows
                            coleliminados.Add(mir.Cells("$Instancia").Value)
                        Next

                        mComandoInstancia.Datos.Add("colElementosAEliminar", coleliminados)
                        RaiseEvent ComandoSolicitado(Me, autorizado)

                        If autorizado Then
                            Dim col As IList
                            col = Me.mPropertyVincPrincipal.Value
                            If col Is Nothing Then
                                col = Activator.CreateInstance(Me.mPropertyVincPrincipal.TipoPropiedad)
                                mPropertyVincPrincipal.Value = col
                            End If
                            For Each objeto As Object In coleliminados
                                col.Remove(objeto)
                            Next

                            RaiseEvent ComandoEjecutado(Me, Nothing)
                        End If

                End Select

            End If

        End If

        mComandoInstancia = Nothing

        Me.Poblar()

        Me.DNaIUgd()
    End Sub


    Public Sub DNaIUgd() Implements IctrlDinamico.DNaIUgd
        Me.mDataTable.Rows.Clear()


        If Not Me.mPropertyVincPrincipal Is Nothing AndAlso Me.mPropertyVincPrincipal.Correcta AndAlso miInstanciaVincMoldeFila IsNot Nothing Then

            If mPropertyVincPrincipal.Vinculada Then
                ' rellenar las filas
                If mPropertyVincPrincipal.Value IsNot Nothing Then
                    For Each miobjeto As Object In mPropertyVincPrincipal.Value
                        Dim mir As DataRow = Me.mDataTable.NewRow
                        miInstanciaVincMoldeFila.DN = miobjeto
                        Dim colpv As MV2DN.ColPropVinc

                        colpv = miInstanciaVincMoldeFila.ColPropVincTotal

                        ' rellenar las columnas
                        mir("$Instancia") = miobjeto
                        For Each mipropvinc As MV2DN.PropVinc In colpv
                            Dim valor As Object
                            valor = mipropvinc.Value
                            If valor Is Nothing Then
                                'mir(mipropvinc.Map.NombreVis) = System.Data.bdnull
                            Else
                                'If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsRef(valor.GetType) Then
                                '    ' mir(mipropvinc.Map.NombreVis) = valor.ToString

                                'Else
                                '    mir(mipropvinc.Map.NombreVis) = valor
                                'End If

                                mir(mipropvinc.Map.NombreVis) = valor
                            End If



                   


                        Next

                        Me.mDataTable.Rows.Add(mir)

                    Next
                End If

            End If




        End If


        poblarBarraHerramientas()

    End Sub




    Public Sub IUaDNgd() Implements IctrlDinamico.IUaDNgd



        If Not Me.mPropertyVincPrincipal Is Nothing AndAlso Me.mPropertyVincPrincipal.Correcta Then



            For Each mir As DataRow In Me.mDataTable.Rows

                ModificarFila(mir)


            Next


        End If





    End Sub

    Private Sub ModificarFila(ByVal mir As DataRow)

        miInstanciaVincMoldeFila.DN = mir("$Instancia")

        Dim colpv As MV2DN.ColPropVinc
        colpv = miInstanciaVincMoldeFila.ColPropVincTotal

        ' rellenar las columnas
        For Each mipropvinc As MV2DN.PropVinc In colpv
            Debug.WriteLine(mipropvinc.Map.NombreVis & "--" & mir(mipropvinc.Map.NombreVis).ToString)
            If mipropvinc.EsTipoPorReferencia Then

            Else
                If Not Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsRef(mipropvinc.TipoPropiedad) Then

                    If mipropvinc.TipoPropiedad.IsEnum Then
                        mipropvinc.Value = [Enum].Parse(mipropvinc.TipoPropiedad, mir(mipropvinc.Map.NombreVis))

                    Else
                        mipropvinc.Value = mir(mipropvinc.Map.NombreVis).ToString

                    End If


                End If


            End If
        Next


    End Sub



    Private Sub ModificarCelda(ByVal mir As DataRow, ByVal colNombreVis As String)

        miInstanciaVincMoldeFila.DN = mir("$Instancia")

        Dim colpv As MV2DN.ColPropVinc
        colpv = miInstanciaVincMoldeFila.ColPropVincTotal

        ' rellenar las columnas
        For Each mipropvinc As MV2DN.PropVinc In colpv
            If mipropvinc.Map.NombreVis = colNombreVis Then
                Try
                    mipropvinc.Value = mir(mipropvinc.Map.NombreVis).ToString
                Catch ex As Exception
                    'TODO:cambio provisional   Me.MostrarError(ex.InnerException)
                End Try
            End If
        Next


    End Sub

#End Region




#Region "Métodos de Generación Dinámica"



    Public Sub Poblar() Implements IctrlDinamico.Poblar
        'si nos han pasado una cosa que no es una colección lanzamos una excepción
        If Not Me.mPropertyVincPrincipal.EsColeccion Then
            Throw New ApplicationException("El Tipo en PropVinc no es una colección")
        End If

        Try

            Me.SuspendLayout()



            'comprobamos si nos pasan un mapeado específico para la colección


            Me.lblNombreVis.Text = Me.mPropertyVincPrincipal.Map.NombreVis

            '1º recuperar el map

            Dim InstanciaMap As MV2DN.InstanciaMapDN
            If String.IsNullOrEmpty(Me.mPropertyVincPrincipal.Map.DatosControlAsignado) Then
                InstanciaMap = Me.RecuperarInstanciaMap(Me.mPropertyVincPrincipal.TipoPropiedad) ' mapeado por defecto para el tipo cuando formaparte de col
                If InstanciaMap Is Nothing Then
                    InstanciaMap = Me.RecuperarInstanciaMap(Me.mPropertyVincPrincipal.TipoFijadoColPropiedad) ' mapeado por defecto para el tipo
                End If
            Else
                InstanciaMap = Me.RecuperarInstanciaMap(Me.mPropertyVincPrincipal.Map.DatosControlAsignado) ' el mapeado especifico solicitado
            End If

            If InstanciaMap Is Nothing Then
                Dim milabel As Label = Me.lblNombreVis
                milabel.ForeColor = Color.Red
                milabel.Text += " No se ha Recuperado el Mapeado para la propiedad para los tipos: " & Me.mPropertyVincPrincipal.TipoFijadoColPropiedad.FullName & " -- " & Me.mPropertyVincPrincipal.TipoPropiedad.FullName
                'milabel.Location = Me.DataGridView1.Location
                'Me.Controls.Add(milabel)
                'Me.DataGridView1.VirtualMode = False
                Exit Sub
            End If

            '2º recuperar el vinc

            miInstanciaVincMoldeFila = New MV2DN.InstanciaVinc(Me.mPropertyVincPrincipal.TipoFijadoColPropiedad, InstanciaMap, mRecuperadorMap, Nothing)

            '3º creamos las columnas a partir de los PropVinc


            Me.mDataTable.Columns.Clear()
            'creamos la 1ª columna, que nos va a identificar el objeto de cada columna
            Me.mDataTable.Columns.Add(New DataColumn("$Instancia", miInstanciaVincMoldeFila.Tipo))

            mColPropVincMoldeColumna = miInstanciaVincMoldeFila.ColPropVincTotal

            For Each propertyvinc As MV2DN.PropVinc In mColPropVincMoldeColumna
                If propertyvinc.Correcta Then
                    Dim micolumna As New DataColumn(propertyvinc.Map.NombreVis, propertyvinc.TipoPropiedad)

                    Me.mDataTable.Columns.Add(micolumna)
                Else
                    Dim micolumna As New DataColumn(propertyvinc.Map.NombreVis, GetType(String))
                    micolumna.DefaultValue = "Incorrecta"
                    Me.mDataTable.Columns.Add(micolumna)
                End If
            Next


            Me.DataGridView1.DataSource = Me.mDataTable
            'ocultamos la 1ª columna (el objeto)
            Me.DataGridView1.Columns(0).Visible = False
            ' Me.DataGridView1.Columns(0).ReadOnly = True

            ' poner datagrid en modo lectura si no es editable
            'If Me.miInstanciaVincMoldeFila.Eseditable Then
            '    Me.DataGridView1.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
            'Else
            '    Me.DataGridView1.EditMode = DataGridViewEditMode.EditProgrammatically
            'End If


            Dim pueteroCol As Integer = 1
            For Each propertyvinc As MV2DN.PropVinc In mColPropVincMoldeColumna
                If propertyvinc.EsColeccion Then
                    'Beep()
                Else



                    If pueteroCol < Me.DataGridView1.Columns.Count Then
                        'If Not mPropertyVincPrincipal.ElementoMap.Editable Then
                        'If Not mPropertyVincPrincipal.Eseditable Then
                        '    Me.DataGridView1.Columns(pueteroCol).ReadOnly = True
                        'Else
                        If propertyvinc.Map.EsReadOnly Then
                            Me.DataGridView1.Columns(pueteroCol).ReadOnly = True
                        Else
                            Me.DataGridView1.Columns(pueteroCol).ReadOnly = (Not propertyvinc.Map.Editable)
                        End If
                        'End If


                    Else
                        'Beep()
                    End If
                    pueteroCol += 1
                End If


            Next


            poblarBarraHerramientas()

            ControlTamaño(0, 0)
        Finally
            Me.ResumeLayout()
        End Try


    End Sub

    Public Sub ControlTamaño(ByRef alto As Integer, ByRef ancho As Integer)



        If Me.mPropertyVincPrincipal.Map.Ancho > -1 OrElse Me.mPropertyVincPrincipal.Map.Alto > -1 Then

            Me.Dock = DockStyle.None
            If Me.mPropertyVincPrincipal.Map.Ancho > -1 Then
                Me.Width = Me.mPropertyVincPrincipal.Map.Ancho
            End If
            If Me.mPropertyVincPrincipal.Map.Alto > -1 Then
                Me.Height = Me.mPropertyVincPrincipal.Map.Alto

            End If


        Else

        End If
        If Me.mPropertyVincPrincipal.Map.Ancho <= -1 AndAlso Me.mPropertyVincPrincipal.Map.Alto <= -1 Then
            Me.Dock = DockStyle.Fill
        Else


            If Me.mPropertyVincPrincipal.Map.Ancho > -1 AndAlso Me.mPropertyVincPrincipal.Map.Alto > -1 Then

                Me.Anchor = AnchorStyles.None


            Else
                If Me.mPropertyVincPrincipal.Map.Ancho > -1 Then
                    Me.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom
                Else
                    Me.Anchor = AnchorStyles.Left Or AnchorStyles.Right
                End If


            End If


        End If

        ancho = Me.Width
        alto = Me.Height

    End Sub


    Public Sub poblarBarraHerramientas()


        ' crear los controles asociados
        'Me.Panel1.Controls.Clear()
        If Me.Panel1.Controls.Contains(Me.mctrlBarraBotonesGD) Then
            Me.Panel1.Controls.Remove(Me.mctrlBarraBotonesGD)
        End If

        If Me.mPropertyVincPrincipal.Vinculada Then
            mctrlBarraBotonesGD = New MV2ControlesBasico.ctrlBarraBotonesGD
            mctrlBarraBotonesGD.FlowLayoutPanel.FlowDirection = FlowDirection.TopDown
            ' mctrlBarraBotonesGD.FlowLayoutPanel1.BackColor = Color.Yellow
            mctrlBarraBotonesGD.Poblar(Me.CrearColComandosDeColeccion)
            ' mctrlBarraBotonesGD.BackColor = Color.Red


            Me.Panel1.Dock = DockStyle.Fill
            Me.Panel1.Controls.Add(mctrlBarraBotonesGD)
            '  Me.Panel1.BackColor = Color.Black


            ' mctrlBarraBotonesGD.Dock = DockStyle.Fill
            ' mctrlBarraBotonesGD.FlowLayoutPanel1.Dock = DockStyle.Fill
            ' mctrlBarraBotonesGD.Height = 300
            'mctrlBarraBotonesGD.Left = Me.Panel1.Width - mctrlBarraBotonesGD.Width
            '  mctrlBarraBotonesGD.FlowLayoutPanel1.Height = 300

            'mctrlBarraBotonesGD.Anchor = AnchorStyles.Right Or AnchorStyles.Top

        End If





    End Sub



    Private Function CrearColComandosDeColeccion() As MV2DN.ColComandoInstancia


        Dim micolCom As MV2DN.ColComandoInstancia = Nothing

        micolCom = New MV2DN.ColComandoInstancia

        If mPropertyVincPrincipal.InstanciaVinc.DN Is Nothing OrElse mPropertyVincPrincipal.Value Is Nothing Then
            If Not mPropertyVincPrincipal.Map.EsReadOnly Then
                If mPropertyVincPrincipal.Map.Instanciable Then
                    micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.Crear))
                End If
            Else
                Debug.WriteLine(mPropertyVincPrincipal.Map.EsReadOnly)
            End If
            Return micolCom

        Else
            micolCom = New MV2DN.ColComandoInstancia


            If mPropertyVincPrincipal.Eseditable Then

                If Not mPropertyVincPrincipal.Map.EsReadOnly Then

                    If mPropertyVincPrincipal.Map.EsNavegable Then
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.NavegarEditar))

                    End If

                    If mPropertyVincPrincipal.Map.Instanciable Then
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.Crear))
                    End If
                    If mPropertyVincPrincipal.Map.EsBuscable Then
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.Buscar))

                    End If

                    If mPropertyVincPrincipal.Map.EsEliminable Then
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.NoReferir))
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.NoReferirTodos))
                    End If


                End If
            Else

                If Not mPropertyVincPrincipal.Map.EsReadOnly AndAlso mPropertyVincPrincipal.Map.EsNavegable Then


                    micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.NavegarEditar))

                End If

            End If







            Return micolCom


        End If

    End Function


    Private Function BuscarRecuperadorMapeado(ByVal pContenedor As Control) As MV2DN.IRecuperadorInstanciaMap

        If mRecuperadorMap Is Nothing Then
            If pContenedor Is Nothing Then
                Return Nothing
            End If

            If TypeOf pContenedor Is MV2DN.IRecuperadorInstanciaMap Then
                mRecuperadorMap = pContenedor
            Else
                Return BuscarRecuperadorMapeado(pContenedor.Parent)
            End If
        End If

        Return mRecuperadorMap



    End Function
#End Region

#Region "MV2Controles.IctrlDinamico"

    Private Function RecuperarInstanciaMap(ByVal pTipo As System.Type) As MV2DN.InstanciaMapDN Implements MV2DN.IRecuperadorInstanciaMap.RecuperarInstanciaMap

        BuscarRecuperadorMapeado(Me.Parent)

        If Not mRecuperadorMap Is Nothing Then
            Return Me.mRecuperadorMap.RecuperarInstanciaMap(pTipo)
        End If

        Return Nothing
    End Function
    Public Function RecuperarInstanciaMap(ByVal pNombreMapInstancia As String) As MV2DN.InstanciaMapDN Implements MV2DN.IRecuperadorInstanciaMap.RecuperarInstanciaMap
        If mRecuperadorMap Is Nothing Then
            Me.BuscarRecuperadorMapeado(Me.Parent)
        End If

        Return Me.mRecuperadorMap.RecuperarInstanciaMap(pNombreMapInstancia)


    End Function

#End Region




    Public Event ControlSeleccionado(ByVal sender As Object, ByVal e As ControlSeleccioandoEventArgs) Implements IctrlDinamico.ControlSeleccionado

    Public Property ControlDinamicoSeleccioando() As IctrlDinamico Implements IctrlDinamico.ControlDinamicoSeleccioando
        Get

        End Get
        Set(ByVal value As IctrlDinamico)

        End Set
    End Property

    Public ReadOnly Property ElementoVinc() As MV2DN.IVincElemento Implements IctrlDinamico.ElementoVinc
        Get
            Return Me.mPropertyVincPrincipal
        End Get
    End Property

    Private Sub lblNombreVis_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lblNombreVis.Click
        Dim PadreIctrlDinamico As IctrlDinamico = BuscarPadreIctrlDinamico()
        PadreIctrlDinamico.ControlDinamicoSeleccioando = Me
    End Sub

    Private Function BuscarPadreIctrlDinamico() As MV2Controles.IctrlDinamico
        Return BuscarPadreIctrlDinamico(Me.Parent)
    End Function

    Private Function BuscarPadreIctrlDinamico(ByVal pContenedor As Control) As MV2Controles.IctrlDinamico


        If pContenedor Is Nothing Then
            Return Nothing
        Else
            If TypeOf pContenedor Is MV2Controles.IctrlDinamico Then
                Return pContenedor
            Else
                Return BuscarPadreIctrlDinamico(pContenedor.Parent)
            End If
        End If

    End Function

    Public ReadOnly Property Comando() As MV2DN.ComandoInstancia Implements IctrlDinamico.Comando
        Get
            Return Me.mComandoInstancia
        End Get
    End Property

    Public Event ComandoEjecutado(ByVal sender As Object, ByVal e As System.EventArgs) Implements IctrlDinamico.ComandoEjecutado

    Public Event ComandoSolicitado(ByVal sender As Object, ByRef autorizado As Boolean) Implements IctrlDinamico.ComandoSolicitado

    Private Sub DataGridView1_CellEndEdit(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles DataGridView1.CellEndEdit

        Dim drv As System.Data.DataRowView = DataGridView1.Rows(e.RowIndex).DataBoundItem
        ' ModificarFila(drv.Row)

        ModificarCelda(drv.Row, DataGridView1.Columns(e.ColumnIndex).Name)


    End Sub

    Private Sub DataGridView1_RowLeave(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles DataGridView1.RowLeave


    End Sub


    Public Property IRecuperadorInstanciaMap() As MV2DN.IRecuperadorInstanciaMap Implements IctrlDinamico.IRecuperadorInstanciaMap
        Get
            Return BuscarPadreIctrlDinamico.IRecuperadorInstanciaMap
        End Get
        Set(ByVal value As MV2DN.IRecuperadorInstanciaMap)

        End Set
    End Property

    Public Property IGestorPersistencia() As MV2DN.IGestorPersistencia Implements IctrlDinamico.IGestorPersistencia
        Get
            Return BuscarPadreIctrlDinamico.IGestorPersistencia
        End Get
        Set(ByVal value As MV2DN.IGestorPersistencia)

        End Set
    End Property


#Region "control del ZOOM"

    Private mPanelZoom As Panel

    Private Sub cmdZoom_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdZoom.Click
        Try
            'ejecutamos iuadn para que los posibles cambios que se hayan
            'hecho sobre el datagrid se reflejen en los objetos en memoria
            Me.IUaDNgd()

            'creamos el control zoom y le pasamos el propertyvinc y lo poblamos
            'para que tenga los mismos datos que tengo yo
            Dim micontrolzoom As New ctrlListaGDZoom(Me.mPropertyVincPrincipal)
            'le decimos que yo soy el que le va a dar el recuperador de mapeado
            micontrolzoom.RecuperadorMapeado = Me
            micontrolzoom.IGestorPersistencia = Me.IGestorPersistencia
            'lo poblamos y ejecutamos DNaIU
            micontrolzoom.Poblar()
            micontrolzoom.DNaIUgd()

            'escuchamos el evento de fin de zoom
            AddHandler micontrolzoom.ZoomAcabado, AddressOf ZoomTerminado

            'creamos el panel, metemos el zoom dentro y lo mostramos sobre todo lo demás
            mPanelZoom = New Panel
            mPanelZoom.SuspendLayout()

            mPanelZoom.Controls.Add(micontrolzoom)
            micontrolzoom.Dock = DockStyle.Fill
            Me.ParentForm.Controls.Add(mPanelZoom)
            mPanelZoom.BringToFront()
            mPanelZoom.Dock = DockStyle.Fill
            mPanelZoom.ResumeLayout()
        Catch ex As Exception
            'TODO:cambio provisional   MostrarError(ex)
        End Try
    End Sub

    Private Sub ZoomTerminado()
        'ejecutamos dnaiu para obtener los cambios que hayan podido
        'darse en el control zoom y que ahora están en memoria
        Me.DNaIUgd()

        'ahora destruimos el panel con el control zoom
        Me.ParentForm.SuspendLayout()

        Me.ParentForm.Controls.Remove(mPanelZoom)
        mPanelZoom.Dispose()

        Me.ParentForm.ResumeLayout()
    End Sub
#End Region

    Public Property DN() As Object Implements IctrlBasicoDN.DN
        Get

        End Get
        Set(ByVal value As Object)

        End Set
    End Property

    Private Sub DataGridView1_CellContentClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles DataGridView1.CellContentClick

    End Sub
End Class
