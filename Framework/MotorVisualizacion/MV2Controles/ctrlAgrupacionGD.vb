
Imports Framework.iu.iucomun
Public Class ctrlAgrupacionGD
    Implements MV2DN.IRecuperadorInstanciaMap
    Implements IctrlDinamico


    Shared clasecolor As Int64

    Dim WithEvents mTableLayoutPanel As TableLayoutPanel
    Protected mControlDinamicoSeleccioando As IctrlDinamico
    Protected mAgrupacionVinc As MV2DN.AgrupacionVinc
    Protected mListaIctrlBasicoDN As New List(Of Framework.IU.IUComun.IctrlBasicoDN)
    Protected mIGestorPersistencia As MV2DN.IGestorPersistencia
    Protected mTamañoControlesContenidos As Integer
    Protected WithEvents cagl As MV2Controles.ctrlAgrupacionGD
    Protected mhtPropVincaControlesBasicos As New Generic.Dictionary(Of Framework.IU.IUComun.IctrlBasicoDN, MV2DN.PropVinc)






    Public Sub New()

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Public Sub New(ByVal pAgVinc As MV2DN.AgrupacionVinc)

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        If pAgVinc Is Nothing Then
            Throw New ApplicationException
        End If
        mAgrupacionVinc = pAgVinc

    End Sub



    Public Property TamañoControlesContenidos() As Integer
        Get
            Return mTamañoControlesContenidos
        End Get
        Set(ByVal value As Integer)
            mTamañoControlesContenidos = value
        End Set
    End Property

    Public Function RecuperarInstanciaMap(ByVal pNombreMapInstancia As String) As MV2DN.InstanciaMapDN Implements MV2DN.IRecuperadorInstanciaMap.RecuperarInstanciaMap

        Dim rp As MV2DN.IRecuperadorInstanciaMap = RecuperarIRecuperadorInstanciaMap(Me.Parent)

        Return rp.RecuperarInstanciaMap(pNombreMapInstancia)


    End Function


    Public Function RecuperarInstanciaMap(ByVal pTipo As System.Type) As MV2DN.InstanciaMapDN Implements MV2DN.IRecuperadorInstanciaMap.RecuperarInstanciaMap
        Dim rp As MV2DN.IRecuperadorInstanciaMap
        rp = Me.BuscarPadreIctrlDinamico
        Return rp.RecuperarInstanciaMap(pTipo)

    End Function


    Private Function RecuperarIRecuperadorInstanciaMap(ByVal padre As Control) As MV2DN.IRecuperadorInstanciaMap

        If TypeOf padre Is MV2DN.IRecuperadorInstanciaMap Then
            Return padre
        Else
            If padre.Parent Is Nothing Then
                Return Nothing
            Else
                Return RecuperarIRecuperadorInstanciaMap(padre.Parent)
            End If

        End If
    End Function

    Public Sub IUaDNgd() Implements IctrlDinamico.IUaDNgd
        For Each micontrol As Framework.IU.IUComun.IctrlBasicoDN In Me.mListaIctrlBasicoDN

            micontrol.IUaDNgd()


        Next
    End Sub


    Private Function CreacionControlesAgrupacion(ByVal pContenedor As Control, ByRef altoAcumulado As Integer) As Control
        Dim alto As Int64


        Select Case Me.mAgrupacionVinc.Map.TipoAgrupacionMap.Nombre


            Case ""
                'Beep()
            Case "Columnas"





                Dim Columnas As New TableLayoutPanel
                Columnas.Height = 0
                '  Columnas.Width = 0
                AddHandler Columnas.DoubleClick, AddressOf mTableLayoutPanel_DoubleClick


                pContenedor.Controls.Add(Columnas)
                Me.ControlDeDimension(Columnas, 0)


                Columnas.ColumnCount = mAgrupacionVinc.ColAgrupacionVinc.Count
                Dim punteroColumna As Integer
                Dim ratio As Integer = 100 \ mAgrupacionVinc.ColAgrupacionVinc.Count


                For Each miAgrupacionVinc As MV2DN.AgrupacionVinc In mAgrupacionVinc.ColAgrupacionVinc


                    Dim cag As ctrlAgrupacionGD
                    cag = New ctrlAgrupacionGD(miAgrupacionVinc)
                    'AddHandler miTabPage.DoubleClick, AddressOf cag.mTableLayoutPanel_DoubleClick
                    AddHandler cag.ControlSeleccionado, AddressOf cagl_ControlSeleccionado

                    Columnas.Controls.Add(cag, punteroColumna, 0)
                    Columnas.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, ratio))
                    cag.Poblar()
                    'If cag.Height < 15 Then
                    '    cag.Height = 15
                    'End If

                    Dim nuevoalto As Integer = cag.Height

                    If nuevoalto > alto Then
                        alto = nuevoalto
                    End If
                    mListaIctrlBasicoDN.Add(cag)
                    punteroColumna += 1
                Next



                Me.ControlDeDimension(Columnas, alto)




            Case "Filas"

                Dim Filas As New TableLayoutPanel

                AddHandler Filas.DoubleClick, AddressOf mTableLayoutPanel_DoubleClick

                pContenedor.Controls.Add(Filas)
                Filas.Dock = DockStyle.Fill

                Filas.RowCount = mAgrupacionVinc.ColAgrupacionVinc.Count
                Dim punteroFila As Integer
                For Each miAgrupacionVinc As MV2DN.AgrupacionVinc In mAgrupacionVinc.ColAgrupacionVinc


                    Dim cag As ctrlAgrupacionGD
                    cag = New ctrlAgrupacionGD(miAgrupacionVinc)
                    AddHandler cag.ControlSeleccionado, AddressOf cagl_ControlSeleccionado

                    Filas.Controls.Add(cag, 0, punteroFila)
                    cag.Dock = DockStyle.Fill
                    cag.Poblar()

                    mListaIctrlBasicoDN.Add(cag)
                    punteroFila += 1
                Next


            Case "Solapas"


                Dim solapas As TabControl
                solapas = New TabControl
                'AddHandler solapas.DoubleClick, AddressOf mTableLayoutPanel_DoubleClick

                pContenedor.Controls.Add(solapas)
                Dim altosolapa As Integer = 30

                Me.ControlDeDimension(solapas, 0)

                ' solapas.Dock = DockStyle.Fill

                For Each miAgrupacionVinc As MV2DN.AgrupacionVinc In mAgrupacionVinc.ColAgrupacionVinc

                    Dim miTabPage As New TabPage(miAgrupacionVinc.Map.NombreVis)
                    solapas.TabPages.Add(miTabPage)


                    Dim cag As ctrlAgrupacionGD
                    cag = New ctrlAgrupacionGD(miAgrupacionVinc)
                    AddHandler miTabPage.DoubleClick, AddressOf cag.mTableLayoutPanel_DoubleClick
                    AddHandler cag.ControlSeleccionado, AddressOf cagl_ControlSeleccionado

                    miTabPage.Controls.Add(cag)
                    'cag.Dock = DockStyle.Fill
                    cag.Poblar()
                    Dim nuevoalto As Integer = cag.Height
                    If nuevoalto > alto Then
                        alto = nuevoalto
                    End If
                    ' alto = cag.Height
                    mListaIctrlBasicoDN.Add(cag)

                Next
                alto += altosolapa
                Me.ControlDeDimension(solapas, alto)

        End Select

        altoAcumulado += alto
        Return Me
    End Function



    Public Sub Poblar() Implements IctrlDinamico.Poblar
        Me.SuspendLayout()

        ' creacion de mi control contenedor
        mListaIctrlBasicoDN.Clear()





        '1º CREACION DEL CONTROL CONTENEDOR
        Me.Padding = New Padding(1)
        Me.Height = 0
        ControlDeDimension(Me, 0)


        mTableLayoutPanel = CrearControlContenedor(Me.mAgrupacionVinc.Map)
        mTableLayoutPanel.SuspendLayout()
        Me.Controls.Add(mTableLayoutPanel)

        ControlDeDimension(mTableLayoutPanel, 0)

        ' mTableLayoutPanel.BackColor = Color.Red


        '2º CREACION DE LOS CONTROLES DE PROPIEDAD
        ' proceso de creacion de los controles  vincilados a propeidad
        Dim alto As Int64
        mTableLayoutPanel.RowCount = mAgrupacionVinc.ColPropVinc.Count
        Dim fila As Int16
        For Each miPropVinc As MV2DN.PropVinc In mAgrupacionVinc.ColPropVinc
            'If miPropVinc.Value IsNot Nothing Then
            Dim micontrol As Control
            micontrol = CrearControlPropiedad(miPropVinc)
            mTableLayoutPanel.Controls.Add(micontrol, 0, fila)
            micontrol.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top Or AnchorStyles.Bottom
            If TypeOf micontrol Is Framework.IU.IUComun.IctrlBasicoDN Then
                Dim micd As Framework.IU.IUComun.IctrlBasicoDN = micontrol
                'If TypeOf micontrol Is IctrlDinamico Then
                mListaIctrlBasicoDN.Add(micontrol)
                'End If

                micd.Poblar()

            End If
            alto += micontrol.Height + micontrol.Margin.Top * 2 + micontrol.Margin.Bottom * 2
            fila += 1
            'Else
            ' Debug.WriteLine("omision por nothing")
            ' End If
        Next


        CreacionControlesAgrupacion(mTableLayoutPanel, alto)

        ControlDeDimension(mTableLayoutPanel, alto)
        ControlDeDimension(Me, alto)


        mTableLayoutPanel.ResumeLayout()


        Me.ResumeLayout()
    End Sub


    Private Sub ControlDeDimension(ByVal pControl As Control, ByVal altoHijosContenidos As Integer)
        If pControl.Dock = DockStyle.Fill Then
            'Beep()
        Else


            pControl.Anchor = AnchorStyles.None
            pControl.Width = pControl.Parent.Width ' el ancho de mi padre 
            ' pControl.ResumeLayout(True)
            If pControl.Height < altoHijosContenidos Then
                pControl.Height = altoHijosContenidos '  debe ser la suma de el alto de los hijos que contengo
                If Not pControl.Height = altoHijosContenidos Then
                    If Me.Parent.Height < altoHijosContenidos Then
                        Me.Parent.Height = altoHijosContenidos
                    End If

                End If
            End If
            pControl.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top Or AnchorStyles.Bottom

        End If
    End Sub
    Private Function CrearControlContenedor(ByVal pAgrupacionMapDN As MV2DN.AgrupacionMapDN) As Control


        Select Case pAgrupacionMapDN.TipoAgrupacionMap.ID

            Case Else
                Dim miTableLayoutPanel As TableLayoutPanel
                miTableLayoutPanel = New TableLayoutPanel
                clasecolor += 10
                'miTableLayoutPanel.BackColor = Color.White
                miTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0)
                miTableLayoutPanel.Padding = New System.Windows.Forms.Padding(0)
                miTableLayoutPanel.Location = New Drawing.Point(0, 0)
                miTableLayoutPanel.Height = 0
                miTableLayoutPanel.Width = 0
                miTableLayoutPanel.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top Or AnchorStyles.Bottom

                Return miTableLayoutPanel

        End Select



    End Function



    Private Function CrearControlPropiedad(ByVal pPropVinc As MV2DN.PropVinc) As Control

        'Dim lbl As Label
        'lbl = New Label
        'lbl.Text = pPropVinc.Map.NombreVis
        'Return lbl


        If Not pPropVinc.Correcta Then
            Dim micontrol As Label
            micontrol = New Label
            micontrol.ForeColor = Color.Red
            micontrol.Text = "propiedad no vinculada: " & pPropVinc.Map.NombreProp & " (" & pPropVinc.Map.NombreVis & ")"
            Return micontrol
        End If

        If pPropVinc.Map.Virtual AndAlso String.IsNullOrEmpty(pPropVinc.Map.ControlAsignado) Then
            Dim micontrol As Label
            micontrol = New Label
            micontrol.ForeColor = Color.Red
            micontrol.Text = "Una propiedad virtual requeire informacion en 'ControlAsignado' (" & pPropVinc.Map.NombreVis & ")"
            Return micontrol

        End If

        ' 1º si tengo un control especificado
        If Not String.IsNullOrEmpty(pPropVinc.Map.ControlAsignado) Then

            Dim icb As Framework.IU.IUComun.IctrlBasicoDN
            ' crear la isntancia solicitada del control
            Dim tipo As System.Type
            Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(pPropVinc.Map.ControlAsignado, Nothing, tipo)
            icb = Activator.CreateInstance(tipo)

            If TypeOf icb Is IctrlDinamico Then
                Dim icd As IctrlDinamico = icb
                icd.DatosControl = pPropVinc.Map.DatosControlAsignado
            Else
                mhtPropVincaControlesBasicos.Add(icb, pPropVinc)
            End If


            If TypeOf icb Is CtrlBusquedaGD Then
                Dim cb As CtrlBusquedaGD = icb
                cb.Map = pPropVinc.Map
            End If





            Return icb
        Else


            'If Not String.IsNullOrEmpty(pPropVinc.Map.DatosControlAsignado) Then
            '    ' 2º se presupone que los datos de control corresponden a un nombre de mapeado

            '    Dim mictrlGD As New ctrlGD
            '    mictrlGD.NombreInstanciaMap = pPropVinc.Map.DatosControlAsignado ' mapeado de visisvilidad
            '    mictrlGD.TipoEntidad = pPropVinc.TipoPropiedad

            '    Return mictrlGD

            'End If


        End If



        If pPropVinc.EsColeccion Then
            ' crear un control de coleccion generico
            Dim icd As IctrlDinamico
            ' crear la isntancia solicitada del control
            'TODO: luis - ctrlListaGD2 ex 1
            icd = New ctrlListaGD2(pPropVinc)
            icd.DatosControl = pPropVinc.Map.DatosControlAsignado ' el mapado de visualizacion para las instancias contenidas en lacol
            Return icd

            Return Nothing
        End If


        ' llegados a este punto lo queiro ver como un tipo por valor

        'If pPropVinc.EsTipoPorReferencia Then
        '    ' crear un control de generico que usara el mapeado por defecto

        '    Dim mictrlGD As New ctrlGD
        '    ' mictrlGD.NombreInstanciaMap = pPropVinc.Map.DatosControlAsignado
        '    mictrlGD.TipoEntidad = pPropVinc.TipoPropiedad
        '    Return mictrlGD

        'Else

        ' se trata de un tipo por valor
        Dim mictrlResumen As ctrlResumenGD
        mictrlResumen = New ctrlResumenGD(pPropVinc)
        Return mictrlResumen

        ' End If


    End Function

    Public Sub DNaIUgd() Implements IctrlDinamico.DNaIUgd
        For Each micontrol As Framework.IU.IUComun.IctrlBasicoDN In Me.mListaIctrlBasicoDN


            If Not TypeOf micontrol Is MV2Controles.IctrlDinamico Then
                Dim pv As MV2DN.PropVinc = mhtPropVincaControlesBasicos(micontrol)
                If pv.Vinculada AndAlso Not pv.PropertyInfoVinc Is Nothing Then
                    micontrol.DN = pv.ValorObjetivo
                End If

            End If



            micontrol.DNaIUgd()


        Next
    End Sub

    Public Property DatosControl() As String Implements IctrlDinamico.DatosControl
        Get

        End Get
        Set(ByVal value As String)

        End Set
    End Property

    Public Event ControlSeleccionado(ByVal sender As Object, ByVal e As ControlSeleccioandoEventArgs) Implements IctrlDinamico.ControlSeleccionado
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


    Private Function BuscarPadreIctrlDinamico() As MV2Controles.IctrlDinamico
        Return BuscarPadreIctrlDinamico(Me.Parent)
    End Function
    Public Property ControlDinamicoSeleccioando() As IctrlDinamico Implements IctrlDinamico.ControlDinamicoSeleccioando
        Get
            Return mControlDinamicoSeleccioando
        End Get
        Set(ByVal value As IctrlDinamico)
            mControlDinamicoSeleccioando = value
            Dim pd As MV2Controles.IctrlDinamico
            pd = BuscarPadreIctrlDinamico()
            If Not pd Is Nothing Then
                pd.ControlDinamicoSeleccioando = mControlDinamicoSeleccioando
            End If


        End Set
    End Property

    Public ReadOnly Property ElementoVinc() As MV2DN.IVincElemento Implements IctrlDinamico.ElementoVinc
        Get
            Return mAgrupacionVinc
        End Get
    End Property

    Public ReadOnly Property Comando() As MV2DN.ComandoInstancia Implements IctrlDinamico.Comando
        Get

        End Get
    End Property

    Public Event ComandoEjecutado(ByVal sender As Object, ByVal e As System.EventArgs) Implements IctrlDinamico.ComandoEjecutado

    Public Event ComandoSolicitado(ByVal sender As Object, ByRef autorizado As Boolean) Implements IctrlDinamico.ComandoSolicitado



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

    Private Sub mTableLayoutPanel_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles mTableLayoutPanel.DoubleClick


        Dim PadreIctrlDinamico As IctrlDinamico = BuscarPadreIctrlDinamico()
        PadreIctrlDinamico.ControlDinamicoSeleccioando = Me

    End Sub
    Private Sub cagl_ControlSeleccionado(ByVal sender As Object, ByVal e As ControlSeleccioandoEventArgs) Handles cagl.ControlSeleccionado



        Dim PadreIctrlDinamico As IctrlDinamico = BuscarPadreIctrlDinamico()
        PadreIctrlDinamico.ControlDinamicoSeleccioando = e.ControlSeleccioando

    End Sub

    Public Function RecuperarControlDinamico(ByVal pElementoMap As MV2DN.ElementoMapDN) As IctrlDinamico Implements IctrlDinamico.RecuperarControlDinamico


        For Each cb As Framework.IU.IUComun.IctrlBasicoDN In mListaIctrlBasicoDN

            If TypeOf cb Is IctrlDinamico Then

                Dim cd As IctrlDinamico = cb

                If cd.ElementoVinc.ElementoMap Is pElementoMap Then
                    Return cd
                Else
                    Dim cds As IctrlDinamico = cd.RecuperarControlDinamico(pElementoMap)
                    If cds IsNot Nothing Then
                        Return cds
                    End If
                End If
            End If
        Next
        Return Nothing

    End Function

    Public Property DN() As Object Implements IctrlBasicoDN.DN
        Get
            Return Me.BuscarPadreIctrlDinamico().DN
        End Get
        Set(ByVal value As Object)
            Me.BuscarPadreIctrlDinamico().DN = value
        End Set
    End Property

    Public Sub SetDN(ByVal entidad As Framework.DatosNegocio.IEntidadDN) Implements Framework.IU.IUComun.IctrlBasicoDN.SetDN
        Dim padre As Framework.IU.IUComun.IctrlBasicoDN = RecuperarPrimerPadreDinamico(Me.Parent)
        padre.SetDN(entidad)

    End Sub

    Private Function RecuperarPrimerPadreDinamico(ByVal control As System.Windows.Forms.Control) As Framework.IU.IUComun.IctrlBasicoDN

        If control Is Nothing Then
            Return Nothing
        End If


        If TypeOf control Is Framework.IU.IUComun.IctrlBasicoDN Then
            Return control
        Else
            Return RecuperarPrimerPadreDinamico(control.Parent)
        End If

    End Function
End Class
