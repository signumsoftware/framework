Imports Framework.iu.iucomun
Public Class ctrlResumenGD

    Implements IctrlDinamico





#Region "eventos"
    Public Event ControlSeleccionado(ByVal sender As Object, ByVal e As ControlSeleccioandoEventArgs) Implements IctrlDinamico.ControlSeleccionado

    Public Event ComandoEjecutado(ByVal sender As Object, ByVal e As System.EventArgs) Implements IctrlDinamico.ComandoEjecutado

    Public Event ComandoSolicitado(ByVal sender As Object, ByRef autorizado As Boolean) Implements IctrlDinamico.ComandoSolicitado

#End Region


#Region "atributos"

#Region "controles genrados dinámicamente"
    Private WithEvents mCheckBox As CheckBox
    Private WithEvents mComboBox As ComboBox
    Private WithEvents mTextBoxValidable As ControlesPBase.textboxXT
    Private WithEvents mctrlBarraBotonesGD As MV2ControlesBasico.ctrlBarraBotonesGD
    Private WithEvents mDatetimePicker As DateTimePicker
    Private WithEvents mIctrlDinamico As MV2Controles.IctrlDinamico
    Private mReferecniaControlEditable As Control
#End Region

#Region "tipo al que se fija este control"
    Private Enum TipoTrabajado As Integer
        T_Indefinido = 0
        T_Entero = 1
        T_Decimal = 2
        T_String = 3
        T_Enum = 4
        T_Boolean = 5
        T_Fecha = 6
    End Enum

    Private mTipoTrabajado As TipoTrabajado
#End Region

    Protected mPropVinc As MV2DN.PropVinc
    Protected mIGestorPersistencia As MV2DN.IGestorPersistencia
    Protected mComandoInstancia As MV2DN.ComandoInstancia



    ' atributos para controlar lso eventos de los controles
    Private mcomoIUaDNgdPermitido As Boolean

#End Region

#Region "constructores"
    Public Sub New()

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub


    Public Sub New(ByVal pPropVinc As MV2DN.PropVinc)

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        mPropVinc = pPropVinc
        '   Me.Poblar()
    End Sub
#End Region

#Region "propiedades"
    ''' <summary>
    ''' Se supone que estos datos sirven para 'configurar' el control
    ''' </summary>
    Public Property DatosControl() As String Implements IctrlDinamico.DatosControl
        Get
            Throw New NotImplementedException
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException
        End Set
    End Property

    Public Property ControlDinamicoSeleccioando() As IctrlDinamico Implements IctrlDinamico.ControlDinamicoSeleccioando
        Get
            Return Me
        End Get
        Set(ByVal value As IctrlDinamico)

        End Set
    End Property

    Public ReadOnly Property ElementoVinc() As MV2DN.IVincElemento Implements IctrlDinamico.ElementoVinc
        Get
            Return Me.mPropVinc
        End Get
    End Property

    Public ReadOnly Property Comando() As MV2DN.ComandoInstancia Implements IctrlDinamico.Comando
        Get
            Return mComandoInstancia
        End Get
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property IRecuperadorInstanciaMap() As MV2DN.IRecuperadorInstanciaMap Implements IctrlDinamico.IRecuperadorInstanciaMap
        Get

            Return BuscarPadreIctrlDinamico.IRecuperadorInstanciaMap
        End Get
        Set(ByVal value As MV2DN.IRecuperadorInstanciaMap)

        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property IGestorPersistencia() As MV2DN.IGestorPersistencia Implements IctrlDinamico.IGestorPersistencia
        Get
            If Me.mIGestorPersistencia Is Nothing Then
                Me.mIGestorPersistencia = BuscarPadreIctrlDinamico.IGestorPersistencia
            End If
            Return mIGestorPersistencia
        End Get
        Set(ByVal value As MV2DN.IGestorPersistencia)
            mIGestorPersistencia = value
        End Set
    End Property
#End Region

#Region "Establecer y Rellenar Datos"

    Public Sub IUaDNgd() Implements IctrlDinamico.IUaDNgd

        If Not mPropVinc Is Nothing AndAlso Me.mPropVinc.Correcta Then
            Select Case Me.mTipoTrabajado
                Case TipoTrabajado.T_Fecha

                    If Me.mDatetimePicker.Checked = True AndAlso Me.mPropVinc.Eseditable Then
                        Me.mPropVinc.Value = Me.mDatetimePicker.Value
                    End If

                Case TipoTrabajado.T_Boolean
                    Me.mPropVinc.Value = Me.mCheckBox.Checked
                Case TipoTrabajado.T_Decimal, TipoTrabajado.T_Entero
                    Me.mPropVinc.Value = "0" & Me.mTextBoxValidable.Text
                Case TipoTrabajado.T_String
                    If Me.mPropVinc.EsPropiedadEncadenada Then
                        If Me.mPropVinc.ValueTipoRepresentado IsNot Nothing Then
                            Me.mPropVinc.Value = Me.mTextBoxValidable.Text.Trim
                        End If
                    Else
                        If Me.mPropVinc.RepresentarSubEntidad Then
                            Debug.WriteLine(Me.mPropVinc.Map.NombreVis)

                        Else
                            Me.mPropVinc.Value = Me.mTextBoxValidable.Text.Trim

                        End If
                    End If
                Case TipoTrabajado.T_Enum
                    If mPropVinc.Vinculada Then

                        If mPropVinc.EsPropiedadEncadenada Then
                            Me.mPropVinc.ValueTipoRepresentado = Me.mComboBox.SelectedItem
                        Else
                            If Not Me.mPropVinc.Value Is Me.mComboBox.SelectedItem Then
                                Me.mPropVinc.Value = Me.mComboBox.SelectedItem

                            End If
                        End If

                    End If

                Case TipoTrabajado.T_Indefinido
                    Throw New NotImplementedException
            End Select
        End If

        poblarBarraHerramientas()


        If Me.mIctrlDinamico IsNot Nothing Then
            mIctrlDinamico.IUaDNgd()
        End If

    End Sub

    Public Sub DNaIUgd() Implements IctrlDinamico.DNaIUgd
        If Not mPropVinc Is Nothing AndAlso Me.mPropVinc.Correcta Then





            If Me.mPropVinc.Map.InvisibleSiNothing Then
                If Me.mPropVinc.ValorObjetivo Is Nothing Then
                    Me.Visible = False
                Else
                    Me.Visible = True

                End If
            End If
            If Me.Visible Then
                ControlEditable()
            End If

            Select Case Me.mTipoTrabajado
                Case TipoTrabajado.T_Decimal, TipoTrabajado.T_Entero, TipoTrabajado.T_String
                    If Me.mPropVinc.Vinculada Then

                        If Me.mPropVinc.EsTipoPorReferencia Then
                            If Me.mPropVinc.Value Is Nothing Then
                                Me.mTextBoxValidable.Text = ""
                            Else
                                Me.mTextBoxValidable.Text = Me.mPropVinc.Value.ToString


                            End If
                        Else
                            Me.mTextBoxValidable.Text = Me.mPropVinc.Value

                        End If

                    Else
                        Me.mTextBoxValidable.Text = ""
                    End If

                Case TipoTrabajado.T_Boolean

                    If Me.mPropVinc.Vinculada Then
                        Me.mCheckBox.Checked = Boolean.Parse(Me.mPropVinc.Value)
                    Else
                        Me.mCheckBox.Checked = False
                    End If



                Case TipoTrabajado.T_Enum


                    If Me.mComboBox.DataSource Is Nothing Then
                        CargarDatos()
                    End If


                    If Me.mPropVinc.TipoPropiedad.IsEnum Then
                        Me.mComboBox.SelectedItem = Me.mPropVinc.Value
                    Else

                        If Me.mPropVinc.Vinculada Then

                            If Not Me.mPropVinc.TipoPropiedad.IsEnum Then





                                If Me.mPropVinc.EsPropiedadEncadenada Then

                                    Dim objetovalor As Framework.DatosNegocio.IEntidadBaseDN = Me.mPropVinc.ValueTipoRepresentado
                                    If objetovalor IsNot Nothing Then
                                        For Each objetoenumerado As Framework.DatosNegocio.IEntidadBaseDN In Me.mComboBox.Items
                                            If objetoenumerado.ID = objetovalor.ID Then
                                                Me.mComboBox.SelectedItem = objetoenumerado
                                            End If
                                        Next
                                    End If
                                Else
                                    If Me.mPropVinc.Value Is Nothing Then
                                        Me.mComboBox.SelectedItem = Nothing
                                    End If
                                End If

                            End If


                        Else
                            Me.mComboBox.DataSource = Nothing
                            Me.mComboBox.Items.Clear()
                        End If

                    End If




                Case TipoTrabajado.T_Fecha
                    If Me.mPropVinc.Vinculada Then
                        Dim fecha As Date = Me.mPropVinc.Value
                        If fecha = DateTime.MinValue OrElse fecha = DateTime.MaxValue Then


                            Me.mDatetimePicker.Value = Now
                            Me.mDatetimePicker.Checked = False
                            Me.mDatetimePicker.ShowCheckBox = True

                        Else
                            Me.mDatetimePicker.Checked = True
                            Me.mDatetimePicker.ShowCheckBox = False
                            Me.mDatetimePicker.Value = DateTime.Parse(Me.mPropVinc.Value)
                        End If

                    End If

                Case TipoTrabajado.T_Indefinido
                    Throw New NotImplementedException
            End Select
        End If


        poblarBarraHerramientas()


        If Me.mIctrlDinamico IsNot Nothing Then
            Debug.Write(mIctrlDinamico.ElementoVinc.ElementoMap.Nombre)
            mIctrlDinamico.DNaIUgd()
        End If

    End Sub




    Private Sub ReemplazarObjetoCombo()



        If Me.mPropVinc.Vinculada AndAlso Me.mPropVinc.EsTipoPorReferencia Then
        Else
            Exit Sub
        End If

        Dim tipo As System.Type = Me.mPropVinc.TipoPropiedad

        Dim ElementoSeleccioando As Object
        If Me.mPropVinc.Value IsNot Nothing Then

            For Each objetoenumerado As Object In Me.mComboBox.Items




                If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsRef(tipo) Then
                    If CType(objetoenumerado, Framework.DatosNegocio.IEntidadDN).GUID = CType(Me.mPropVinc.Value, Framework.DatosNegocio.IEntidadDN).GUID Then

                        ElementoSeleccioando = objetoenumerado
                        Exit For

                    End If
                Else
                    If objetoenumerado = Me.mPropVinc.Value Then
                        Me.mComboBox.SelectedItem = objetoenumerado
                    End If
                End If

            Next
        Else
            ElementoSeleccioando = Nothing

        End If

        If ElementoSeleccioando IsNot Nothing AndAlso Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsRef(tipo) Then
            mcomoIUaDNgdPermitido = False
            Dim lista As IList = Me.mComboBox.DataSource
            'Me.mComboBox.Sorted = True
            Me.mComboBox.DataSource = Nothing
            lista.Remove(ElementoSeleccioando)
            lista.Add(Me.mPropVinc.Value)


            'If TypeOf lista Is Framework.DatosNegocio.IColDn Then
            '    Dim al As Framework.DatosNegocio.IColDn = lista
            '    al.Sort()

            'End If



            Me.mComboBox.DataSource = lista
            Me.mComboBox.SelectedItem = Me.mPropVinc.Value
            mcomoIUaDNgdPermitido = True
        End If


    End Sub

#End Region


#Region "Creación de Controles Dinámicos"


    Private Sub ControlDeDimension(ByVal control As Control, ByVal altoHijosContenidos As Integer)
        'control.Dock = DockStyle.None
        If control.Dock = DockStyle.Fill Then
            'Beep()
        Else

            control.Width = control.Parent.Width ' el ancho de mi padre 
            control.Height = altoHijosContenidos '  debe ser la suma de el alto de los hijos que contengo
            control.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
        End If
    End Sub



    Private Sub ControlDeResumen()

        Dim alto As Integer = Me.Height
        ControlDeDimension(Me, alto)
        ControlDeDimension(Me.TableLayoutPanel2, alto)


        'ponemos el nombre en el label
        Me.lblNombreVis.Text = Me.mPropVinc.Map.NombreVis

        'generamos el control dinámicamente
        Dim micontrol As Control = GenerarControlPorTipo()

        'Este identificador sirve para que el Formateador de ES no
        'interfiera con resultados no deseados sobre los controles GD
        micontrol.Tag = "ControlGD"
        '  Me.Panel1.Width = micontrol.Width + 4


        Me.Panel1.Controls.Clear()
        If Me.mPropVinc.Map.Alto > -1 Then

            Me.Height = Me.mPropVinc.Map.Alto
            Me.TableLayoutPanel2.RowStyles(0).Height = Me.mPropVinc.Map.Alto
            Me.TableLayoutPanel1.RowStyles(0).Height = Me.mPropVinc.Map.Alto
            Me.Panel1.Controls.Add(micontrol)
        Else

            Me.Panel1.Controls.Add(micontrol)
            micontrol.Dock = DockStyle.Fill
            Me.Panel1.Dock = DockStyle.Fill


        End If







        'establecemos el control en estado edición/consulta
        ControlEditable()

        'ponemos la imagen
        Dim miimagen As Image = MV2ControlesBasico.ProveedorImagenes.ObtenerImagen(Me.mPropVinc.Map.Ico)
        Me.PictureBox1.Image = miimagen


        If Me.mPropVinc.RepresentarSubEntidad AndAlso Not Me.mPropVinc.Map.VisibleCabeceraResumen Then
            Me.TableLayoutPanel1.Height = 0
            Me.TableLayoutPanel1.Dock = DockStyle.None
            Me.TableLayoutPanel1.Visible = False

            Me.TableLayoutPanel1.Visible = False
            Dim rs As Windows.Forms.RowStyle

            '  Me.TableLayoutPanel2.RowStyles.Add(rs)
            rs = Me.TableLayoutPanel2.RowStyles(0)
            rs.SizeType = SizeType.Absolute
            rs.Height = 0
        Else
            If Me.mPropVinc.RepresentarSubEntidad Then


                If Me.Marco.DatosMarco.Contains("ColorTituloGD") Then
                    Me.TableLayoutPanel1.BackColor = CType(Me.Marco.DatosMarco("ColorTituloGD"), Color)
                End If

                Me.lblNombreVis.ForeColor = Color.Blue
                'TODO: luis - 999 - recuadrar el control hijo
                'Me.BorderStyle = Windows.Forms.BorderStyle.FixedSingle
            End If
        End If

    End Sub

    Public Sub Poblar() Implements IctrlDinamico.Poblar

        If mPropVinc Is Nothing Then
            Me.lblNombreVis.Text = "ERROR: no PropVinc vinculada"
            Me.lblNombreVis.ForeColor = Color.Red
        Else
            If Not Me.mPropVinc.Correcta Then
                Me.lblNombreVis.Text = "No Se encuentra"
                Me.lblNombreVis.ForeColor = Color.Red
            Else


                Me.Padding = New Padding(1)

                ControlDeDimension(Me, 0)
                ControlDeDimension(Me.TableLayoutPanel2, 0)

                ControlDeResumen()

                CrearElControlEspecificoContenido(Me.Panel3.Width)

                poblarBarraHerramientas()

                ControlDeDimension(Me, Me.TableLayoutPanel2.Height)



            End If
        End If


    End Sub



    Private Function CrearElControlEspecificoContenido(ByRef altoAvumulado As Int64) As Int64


        Dim alto As Integer
        Me.Panel3.AutoSize = False
        'ControlDeDimension(Me.Panel3.Parent, Me.Panel1.Height)
        Me.Panel3.Controls.Clear()
        ControlDeDimension(Me.Panel3, 0)

        If Me.mPropVinc.RepresentarSubEntidad Then


            ' puede tratarse de un control especifico o de un mapeado para un control de generacion dinamica


            If Not String.IsNullOrEmpty(Me.mPropVinc.Map.ControlAsignado) Then

                ' control especifico del tipo Icontroldn
                'mIctrlDinamico
            Else
                ' control de genracion dinamica
                'Me.BorderStyle = Windows.Forms.BorderStyle.FixedSingle


                Me.Panel3.AutoSize = False
                Me.Panel3.Padding = New Padding(0, 1, 1, 1)
                Me.Panel3.Margin = New Padding(0, 1, 1, 1)
                Me.Panel3.BorderStyle = Windows.Forms.BorderStyle.None
                ' Me.Panel3.BackColor = Color.RosyBrown

                'Me.Panel3.AutoSize = True




                Dim mictrlGD As New ctrlGD
                mIctrlDinamico = mictrlGD
                mictrlGD.Padding = New Padding(0, 1, 1, 1)
                mictrlGD.Margin = New Padding(0, 1, 1, 1)
                mictrlGD.RecuperadorMap = Me.mPropVinc.InstanciaVinc.IRecuperadorInstanciaMap
                If Not Me.mPropVinc.Map.Editable Then

                    Me.mPropVinc.InstanciaVincReferida.Map.Editable = False
                End If
                mictrlGD.InstanciaVinc = Me.mPropVinc.InstanciaVincReferida

                ' mictrlGD.BackColor = Color.Yellow


                Me.Panel3.Controls.Add(mictrlGD)
                ControlDeDimension(mictrlGD, 0)

                mictrlGD.Poblar()
                mictrlGD.DN = Me.mPropVinc.Value

                'ControlDeDimension(Me.Panel3.Parent, mictrlGD.Height + Me.Panel1.Height)
                ControlDeDimension(Me.Panel3, mictrlGD.Height)


                mictrlGD.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
                Panel3.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
                Return mictrlGD.Height
            End If
        End If



        Return 0

    End Function


    Private Sub poblarBarraHerramientas()

        Dim miColComandos As MV2DN.ColComandoInstancia = CrearColComandos()
        Me.Panel2.Controls.Clear()
        If miColComandos IsNot Nothing Then
            mctrlBarraBotonesGD = New MV2ControlesBasico.ctrlBarraBotonesGD
            mctrlBarraBotonesGD.Poblar(miColComandos)
            Me.Panel2.Dock = DockStyle.None
            Me.Panel2.AutoSize = False
            Me.Panel2.Height = Me.Height
            Me.Panel2.Width = mctrlBarraBotonesGD.Width
            'Me.Panel2.BackColor = Color.BurlyWood
            Me.Panel2.Controls.Add(mctrlBarraBotonesGD)
        End If



    End Sub


    Private Function CrearColComandos() As MV2DN.ColComandoInstancia
        Dim micolCom As MV2DN.ColComandoInstancia = Nothing


        If Me.mPropVinc.EsTipoPorReferencia OrElse Me.mPropVinc.RepresentaTipoPorReferencia Then
            Return CrearColComandosDeREsumen()
        Else
            Return Nothing
        End If



    End Function


    Private Function CrearColComandosDeREsumen() As MV2DN.ColComandoInstancia
        Dim micolCom As MV2DN.ColComandoInstancia = Nothing
        If Not mPropVinc.Vinculada OrElse mPropVinc.Map.EsReadOnly Then
            Return Nothing
        End If

        If Me.mPropVinc.EsPropiedadEncadenada Then
            If Me.mPropVinc.ValueTipoRepresentado Is Nothing Then
                ' no tine instancia asociada

                If Not mPropVinc.Map.EsReadOnly AndAlso mPropVinc.Eseditable Then

                    micolCom = New MV2DN.ColComandoInstancia
                    If mPropVinc.Map.Instanciable Then
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.Crear))
                    End If
                    If mPropVinc.Map.EsBuscable Then
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.Buscar))
                    End If

                End If

                Return micolCom

            Else
                ' tiene instancia asociada

                micolCom = New MV2DN.ColComandoInstancia

                If Not Me.mPropVinc.Map.EsReadOnly Then

                    If Me.mPropVinc.Map.EsNavegable Then
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.NavegarEditar))
                    End If

                    If mPropVinc.Map.EsEliminable AndAlso mPropVinc.Eseditable Then
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.NoReferir))
                    End If
                End If
                Return micolCom
            End If
        Else


            If Me.mPropVinc.Value Is Nothing Then
                ' no tine instancia asociada

                ' If Not mPropVinc.Map.EsReadOnly Then
                If mPropVinc.Eseditable Then
                    micolCom = New MV2DN.ColComandoInstancia
                    If mPropVinc.Map.Instanciable Then
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.Crear))
                    End If
                    If mPropVinc.Map.EsBuscable Then
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.Buscar))
                    End If

                End If

                Return micolCom

            Else
                ' tiene instancia asociada

                micolCom = New MV2DN.ColComandoInstancia

                If Not Me.mPropVinc.Map.EsReadOnly Then

                    If Me.mPropVinc.Map.EsNavegable Then
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.NavegarEditar))
                    End If

                    If mPropVinc.Map.EsEliminable AndAlso Me.mPropVinc.Eseditable Then
                        micolCom.Add(MV2DN.ComandoInstancia.CrearComandoBasico(MV2DN.ComandosMapBasicos.NoReferir))
                    End If
                End If

            End If




            Return micolCom
        End If
    End Function


    Private Function ControlEditable() As Control


        Dim miEditable As Boolean = Me.mPropVinc.Eseditable

        'If Not Me.mPropVinc.InstanciaVinc.Map.Editable OrElse Not Me.mPropVinc.Vinculada Then
        '    miEditable = False
        'End If


        Select Case Me.mTipoTrabajado
            Case TipoTrabajado.T_Decimal, TipoTrabajado.T_Entero, TipoTrabajado.T_String
                Me.mTextBoxValidable.ReadOnly = Not miEditable
                mReferecniaControlEditable = mTextBoxValidable

            Case TipoTrabajado.T_Boolean
                Me.mCheckBox.Enabled = miEditable
                mReferecniaControlEditable = mCheckBox

            Case TipoTrabajado.T_Enum
                Me.mComboBox.Enabled = miEditable
                mReferecniaControlEditable = mComboBox

            Case TipoTrabajado.T_Fecha
                Me.mDatetimePicker.Enabled = miEditable
                Me.Panel1.Enabled = miEditable
                mReferecniaControlEditable = mDatetimePicker

        End Select

        If Me.mPropVinc.RepresentarSubEntidad Then
            mReferecniaControlEditable.Visible = False
        End If


        If String.IsNullOrEmpty(Me.mPropVinc.Map.MedidasIcoLabText) OrElse Me.mPropVinc.Map.MedidasIcoLabText = "-1*f/-1*f/-1*f/-1*f" Then
            ' dejar al ancho estandar
        Else

            Try


                Dim medidas As String() = Me.mPropVinc.Map.MedidasIcoLabText.Split("/")

                FijarColumStile(medidas(0).Split("*"), Me.TableLayoutPanel1.ColumnStyles(0))
                FijarColumStile(medidas(1).Split("*"), Me.TableLayoutPanel1.ColumnStyles(1))
                FijarColumStile(medidas(2).Split("*"), Me.TableLayoutPanel1.ColumnStyles(2))
                FijarColumStile(medidas(3).Split("*"), Me.TableLayoutPanel1.ColumnStyles(3))
            Catch ex As Exception
                Debug.WriteLine("Error en el formato MedidasIcoLabText")
            End Try

        End If


        Return mReferecniaControlEditable


    End Function



    Private Sub FijarColumStile(ByVal medidaconcreta As String(), ByVal cs As System.Windows.Forms.ColumnStyle)


        If Not medidaconcreta(0) = "-1" Then ' indica no hacer nada

            Select Case medidaconcreta(1)

                Case Is = "f"
                    cs.SizeType = SizeType.Absolute
                    cs.Width = medidaconcreta(0)
                Case Is = "p"
                    cs.SizeType = SizeType.Percent
                    cs.Width = medidaconcreta(0)
                Case Is = "a"
                    cs.SizeType = SizeType.AutoSize

            End Select




        End If


    End Sub

    Private Sub CargarDatos()
        Dim tipo As Type = Me.mPropVinc.TipoPropiedad
        mcomoIUaDNgdPermitido = False
        ' si se solicitaron cargar los datos el tipo sera como una enumeracion
        If mPropVinc.Map.CargarDatos Then
            ' Me.mComboBox = New ComboBox
            Me.mComboBox.DropDownStyle = ComboBoxStyle.DropDownList
            Me.mComboBox.DisplayMember = mPropVinc.Map.NombrePropSinPrimeraEntidad
            'rellenamo losvalores del combo
            If mPropVinc.EsPropiedadEncadenada Then
                Me.mComboBox.DataSource = Me.IGestorPersistencia.RecuperarLista(Me.mPropVinc.TipoRepresentado)

            Else
                Me.mComboBox.DataSource = Me.IGestorPersistencia.RecuperarLista(tipo)
            End If

            Me.ReemplazarObjetoCombo()



            Me.mComboBox.Width = 50
            Me.mTipoTrabajado = TipoTrabajado.T_Enum
            ' Return Me.mComboBox
        End If
        mcomoIUaDNgdPermitido = True

    End Sub
    ''' <summary>
    ''' En función del Tipo contra el que se esté fijando el control, generamos un control determinado
    ''' </summary>
    ''' <returns>el control que se ha generado</returns>
    Private Function GenerarControlPorTipo() As Control
        Dim tipo As Type = Me.mPropVinc.TipoPropiedad

        ' si se solicitaron cargar los datos el tipo sera como una enumeracion
        If mPropVinc.Map.CargarDatos Then
            Me.mComboBox = New ComboBox
            'Me.mComboBox.DropDownStyle = ComboBoxStyle.DropDownList
            'Me.mComboBox.DisplayMember = mPropVinc.Map.NombrePropSinPrimeraEntidad
            ''rellenamo losvalores del combo
            'If mPropVinc.EsPropiedadEncadenada Then
            '    Me.mComboBox.DataSource = Me.IGestorPersistencia.RecuperarLista(Me.mPropVinc.TipoRepresentado)

            'Else
            '    Me.mComboBox.DataSource = Me.IGestorPersistencia.RecuperarLista(tipo)
            'End If

            'Me.ReemplazarObjetoCombo()



            'Me.mComboBox.Width = 50
            Me.mTipoTrabajado = TipoTrabajado.T_Enum
            Return Me.mComboBox
        End If







        'comprobamos si se trata de un entero
        If tipo Is GetType(Integer) OrElse tipo Is GetType(Int16) OrElse tipo Is GetType(Int32) OrElse tipo Is GetType(Int64) Then
            Me.mTextBoxValidable = New ControlesPBase.textboxXT
            Me.mTextBoxValidable.SoloInteger = True
            Me.mTextBoxValidable.Width = 150
            Me.mTipoTrabajado = TipoTrabajado.T_Entero
            Return Me.mTextBoxValidable
        End If

        'comprobamos si se trata de un decimal
        If tipo Is GetType(Double) OrElse tipo Is GetType(Decimal) OrElse tipo Is GetType(Single) Then
            Me.mTextBoxValidable = New ControlesPBase.textboxXT
            Me.mTextBoxValidable.SoloDouble = True
            Me.mTextBoxValidable.Width = 50
            Me.mTipoTrabajado = TipoTrabajado.T_Decimal
            Return Me.mTextBoxValidable
        End If

        'se trata de un string
        If tipo Is GetType(String) Then
            Me.mTextBoxValidable = New ControlesPBase.textboxXT
            Me.mTextBoxValidable.ExtendidoSiExcede = True
            If Me.mPropVinc.Map.Alto > -1 Then
                Me.mTextBoxValidable.Multiline = True
                Me.mTextBoxValidable.Height = Me.mPropVinc.Map.Alto
                'If Me.mPropVinc.Map.Ancho < 0 Then
                '    Me.mTextBoxValidable.Width = Me.Parent.Width - 198
                '    Me.mTextBoxValidable.Anchor = AnchorStyles.Left Or AnchorStyles.Right
                'End If
            Else
                Me.mTextBoxValidable.Anchor = AnchorStyles.Top

            End If
            If Me.mPropVinc.Map.Ancho > -1 Then
                Me.mTextBoxValidable.Multiline = True

                Me.mTextBoxValidable.Width = Me.mPropVinc.Map.Ancho
                Me.mTextBoxValidable.Anchor = Me.mTextBoxValidable.Anchor Or AnchorStyles.Left
            Else
                Me.mTextBoxValidable.Width = Me.Parent.Width - 198
                Me.mTextBoxValidable.Anchor = Me.mTextBoxValidable.Anchor Or AnchorStyles.Left Or AnchorStyles.Right

            End If



            Me.mTipoTrabajado = TipoTrabajado.T_String
            Return Me.mTextBoxValidable
        End If

        'se trata de un Boolean
        If tipo Is GetType(Boolean) Then
            Me.mCheckBox = New CheckBox
            Me.mCheckBox.Text = ""
            Me.mTipoTrabajado = TipoTrabajado.T_Boolean
            Return mCheckBox
        End If

        'se trata de una enumeración
        'If tipo.BaseType.FullName = "System.Enum" Then
        If tipo.BaseType Is GetType([Enum]) Then
            Me.mComboBox = New ComboBox
            Me.mComboBox.DropDownStyle = ComboBoxStyle.DropDownList
            'rellenamo losvalores del combo
            Me.mComboBox.DataSource = [Enum].GetValues(tipo)
            Me.mComboBox.Width = 50
            Me.mTipoTrabajado = TipoTrabajado.T_Enum
            Return Me.mComboBox
        End If

        'se trata de una fecha
        If tipo Is GetType(DateTime) Then
            Me.mDatetimePicker = New DateTimePicker
            Me.mDatetimePicker.Format = DateTimePickerFormat.Short
            Me.mDatetimePicker.Dock = DockStyle.Fill
            Me.mTipoTrabajado = TipoTrabajado.T_Fecha
            Return Me.mDatetimePicker
        End If

        Me.mTextBoxValidable = New ControlesPBase.textboxXT
        Me.mTextBoxValidable.Width = 50
        Me.mTextBoxValidable.Enabled = False
        Me.mTipoTrabajado = TipoTrabajado.T_String
        Return Me.mTextBoxValidable


        'TODO: luis falta cargar Datos Tipo (IentidadBase)
        Throw New NotImplementedException
    End Function

#End Region


#Region "métodos"

    Public Function RecuperarControlDinamico(ByVal pElementoMap As MV2DN.ElementoMapDN) As IctrlDinamico Implements IctrlDinamico.RecuperarControlDinamico


        If mIctrlDinamico IsNot Nothing Then
            If Me.mIctrlDinamico.ElementoVinc.ElementoMap Is pElementoMap Then
                Return mIctrlDinamico
            Else
                Return mIctrlDinamico.RecuperarControlDinamico(pElementoMap)
            End If

        End If
    End Function

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

    Private Sub lblNombreVis_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lblNombreVis.Click
        Dim PadreIctrlDinamico As IctrlDinamico = BuscarPadreIctrlDinamico()
        PadreIctrlDinamico.ControlDinamicoSeleccioando = Me
    End Sub




    Private Sub Navegar()

        If Me.mPropVinc.Map.EsNavegable Then
            Dim autorizado As Boolean = True
            RaiseEvent ComandoSolicitado(Me, autorizado)
            If autorizado Then

                ' crear el paquete
                Dim paquete As New Hashtable

                If Me.mPropVinc.EsTipoPorReferencia Then
                    paquete.Add("DN", mPropVinc.Value)
                Else
                    paquete.Add("DN", mPropVinc.ValueTipoRepresentado)

                End If


                ' resolver el destino de navegacion


                Dim datosNAvegacion As String = Me.mPropVinc.Map.DatosNavegacion


                Dim funcionNavegacion As String = "FG"

                If Not String.IsNullOrEmpty(datosNAvegacion) Then

                    If datosNAvegacion.Contains("(map)") Then ' se trata de un mapeado de navegacion para el formulario general
                        funcionNavegacion = "FG"
                        paquete.Add("NombreInstanciaMapVis", datosNAvegacion.Replace("(map)", ""))

                    ElseIf datosNAvegacion.Contains("(nav)") Then
                        funcionNavegacion = datosNAvegacion.Replace("(nav)", "")
                        paquete.Add("NombreInstanciaMapVis", funcionNavegacion)

                    End If

                End If


                Me.Marco.Navegar(funcionNavegacion, Me.ParentForm, Nothing, MotorIU.Motor.TipoNavegacion.Modal, Me.GenerarDatosCarga, paquete)



                Me.Poblar()
                Me.DNaIUgd()
                RaiseEvent ComandoEjecutado(Me, Nothing)
            End If

        End If


    End Sub

    Public Sub crear()

        If Me.mPropVinc.Map.Instanciable Then
            Dim autorizado As Boolean = True
            RaiseEvent ComandoSolicitado(Me, autorizado)
            Dim entidad As Framework.DatosNegocio.IEntidadDN


            'TODO: Revisar
            If mPropVinc.Map.NombreProp.Contains(".") Then
                If Me.mPropVinc.RepresentaTipoPorReferencia Then
                    ' Me.mPropVinc.ValueTipoRepresentado = Activator.CreateInstance(Me.mPropVinc.TipoRepresentado)

                    ' aqui hay que poder seleccionar la entidad si es multiple
                    Dim tipoSelecionado As System.Type
                    ' tipoSelecionado = MV2Controles.MV2ControlesHelper.RecuperarTipoSeleccioando(Me.mPropVinc)
                    tipoSelecionado = MV2Controles.MV2ControlesHelper.RecuperarTipoSeleccioando(Me)
                    entidad = Activator.CreateInstance(tipoSelecionado)
                    entidad.AsignarEntidad(mPropVinc.InstanciaVinc.DN)
                    Me.mPropVinc.ValueTipoRepresentado = entidad
                    If Me.mPropVinc.Map.EsNavegable Then
                        Navegar()
                    End If


                Else
                    'Me.mPropVinc.Value = Activator.CreateInstance(Me.mPropVinc.TipoPropiedad)

                    ' aqui hay que poder seleccionar la entidad si es multiple
                    'Dim tipoSelecionado As System.Type
                    ''  tipoSelecionado = MV2Controles.MV2ControlesHelper.RecuperarTipoSeleccioando(Me.mPropVinc)
                    'tipoSelecionado = MV2Controles.MV2ControlesHelper.RecuperarTipoSeleccioando(Me)
                    'entidad = Activator.CreateInstance(tipoSelecionado)
                    'entidad.AsignarEntidad(mPropVinc.InstanciaVinc.DN)
                    'Me.mPropVinc.Value = entidad

                    Me.mPropVinc.Value = MV2Controles.MV2ControlesHelper.CrearInstancia(Me)


                End If
            Else
                Me.mPropVinc.Value = MV2Controles.MV2ControlesHelper.CrearInstancia(Me)
            End If



            Me.Poblar()
            Me.DNaIUgd()

            RaiseEvent ComandoEjecutado(Me, Nothing)
        End If



    End Sub


    Private Sub Buscar()
        Dim autorizado As Boolean = True
        RaiseEvent ComandoSolicitado(Me, autorizado)
        If autorizado Then

            Dim entidad As Framework.DatosNegocio.IEntidadBaseDN = MV2ControlesHelper.Buscar(Me)

            ' TODO: navergar de modo modal 
            If Me.mPropVinc.EsPropiedadEncadenada Then
                Me.mPropVinc.ValueTipoRepresentado = entidad
            Else
                Me.mPropVinc.Value = entidad
            End If


            Me.Poblar()
            Me.DNaIUgd()

            RaiseEvent ComandoEjecutado(Me, Nothing)
        End If



    End Sub



    Private Sub noReferir()


        Dim autorizado As Boolean = True
        RaiseEvent ComandoSolicitado(Me, autorizado)
        If autorizado Then

            If Me.mPropVinc.RepresentaTipoPorReferencia Then
                Me.mPropVinc.ValueTipoRepresentado = Nothing
            Else
                Me.mPropVinc.Value = Nothing
            End If

            mIctrlDinamico = Nothing
            Me.Poblar()
            Me.DNaIUgd()
            RaiseEvent ComandoEjecutado(Me, Nothing)
        End If



    End Sub


    Private Sub mctrlBarraBotonesGD_ComandoSolicitado(ByVal sender As Object, ByVal e As System.EventArgs) Handles mctrlBarraBotonesGD.ComandoSolicitado


        Try

            ' solo se puede operar si  tu instancia vin esta vinculada a un objeto y ella su vez a una propiedad
            mComandoInstancia = mctrlBarraBotonesGD.ComandoAccioando

            Dim mictrlBarraBotonesGD As MV2ControlesBasico.ctrlBarraBotonesGD = sender


            If Me.mPropVinc.Vinculada Then

                If mictrlBarraBotonesGD.ComandoAccioando.Map.EsComandoBasico Then


                    Select Case mictrlBarraBotonesGD.ComandoAccioando.Map.ComandoBasico

                        Case MV2DN.ComandosMapBasicos.NavegarEditar

                            Navegar()


                        Case MV2DN.ComandosMapBasicos.Crear


                            Me.crear()

                        Case MV2DN.ComandosMapBasicos.Buscar
                            Me.Buscar()

                        Case MV2DN.ComandosMapBasicos.NoReferir

                            noReferir()


                    End Select

                End If




            End If

            mComandoInstancia = Nothing

        Catch ex As Exception
            Me.MostrarError(ex)
        End Try


    End Sub

#End Region




    Private Sub mComboBox_SelectedValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles mComboBox.SelectedValueChanged

        If mcomoIUaDNgdPermitido Then
            Me.IUaDNgd()
        End If

    End Sub

    Public Property DN() As Object Implements IctrlBasicoDN.DN
        Get

        End Get
        Set(ByVal value As Object)

        End Set
    End Property

    Public Sub SetDN(ByVal entidad As Framework.DatosNegocio.IEntidadDN) Implements Framework.IU.IUComun.IctrlBasicoDN.SetDN
        Me.mPropVinc.Value = entidad
    End Sub
End Class

