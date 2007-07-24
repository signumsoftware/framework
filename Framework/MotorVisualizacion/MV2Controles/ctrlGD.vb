Imports Framework.iu.iucomun
Public Class ctrlGD
    Implements MV2DN.IRecuperadorInstanciaMap
    Implements IctrlDinamico





    Public Event DNaIUgdFInalizado(ByVal sender As Object, ByVal e As EventArgs)



    ' Protected mEditable As Boolean

    Protected mControlDinamicoSeleccioando As IctrlDinamico

    Protected mRecuperadorMap As MV2DN.IRecuperadorInstanciaMap '= New MV2DN.RecuperadorMapeadoXFicheroXMLAD(Framework.Configuracion.AppConfiguracion.DatosConfig("RutaCargaMapVis"))
    Protected mInstanciaVinc As MV2DN.InstanciaVinc
    Protected mInstanciaMapDN As MV2DN.InstanciaMapDN
    Protected mNombreInstanciaMapDN As String
    Protected mTipoEntidad As System.Type
    Protected mDn As Object
    Protected mNombreTipo As String
    Protected mNombreMapeadoNombreDiseñoTipoy As String
    Protected mIGestorPersistencia As MV2DN.IGestorPersistencia = New GestorPersistenciaClienteLN
    Protected mTamañoResultanteVinculacionDN As Int64


    'Public Property Editable() As Boolean
    '    Get
    '        Return Me.mEditable
    '    End Get
    '    Set(ByVal value As Boolean)
    '        Me.mEditable = value
    '    End Set
    'End Property


    Public ReadOnly Property TamañoResultanteVinculacionDN() As Int64
        Get
            Return mTamañoResultanteVinculacionDN
        End Get
    End Property


    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property InstanciaVinc() As MV2DN.InstanciaVinc
        Get
            Return Me.mInstanciaVinc
        End Get
        Set(ByVal value As MV2DN.InstanciaVinc)
            mInstanciaVinc = value

            If value Is Nothing Then

                mTipoEntidad = Nothing
                mNombreTipo = Nothing
                mInstanciaMapDN = Nothing
                mNombreInstanciaMapDN = Nothing
            Else
                mTipoEntidad = mInstanciaVinc.Tipo
                mNombreTipo = mInstanciaVinc.Tipo.Name
                Me.mInstanciaMapDN = mInstanciaVinc.Map
                mNombreInstanciaMapDN = mInstanciaMapDN.Nombre
                Me.DN = mInstanciaVinc.DN
            End If

        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property RecuperadorMap() As MV2DN.IRecuperadorInstanciaMap
        Get
            Return mRecuperadorMap
        End Get
        Set(ByVal value As MV2DN.IRecuperadorInstanciaMap)
            mRecuperadorMap = value
        End Set
    End Property

    Public Property ControlDinamicoSeleccioando() As IctrlDinamico Implements IctrlDinamico.ControlDinamicoSeleccioando
        Get
            Return mControlDinamicoSeleccioando
        End Get
        Set(ByVal value As IctrlDinamico)
            If mControlDinamicoSeleccioando IsNot value Then
                mControlDinamicoSeleccioando = value
                RaiseEvent ControlSeleccionado(Me, New MV2Controles.ControlSeleccioandoEventArgs(value))

            End If

        End Set
    End Property


    Public Property NombreMapeadoNombreDiseñoyTipo() As String
        Get
            Return Me.mNombreMapeadoNombreDiseñoTipoy
        End Get
        Set(ByVal value As String)

            mNombreMapeadoNombreDiseñoTipoy = value

            If value IsNot Nothing Then
                Dim valores() As String

                valores = value.Split("/")

                Me.NombreInstanciaMap = valores(0)
                If valores.Length = 2 Then
                    Me.NombreTipo = valores(1)
                End If
                Me.Poblar()
            Else
                Me.NombreInstanciaMap = Nothing
                Me.NombreTipo = Nothing

            End If


        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property NombreTipo() As String
        Get
            Return Me.mNombreTipo
        End Get
        Set(ByVal value As String)
            ' MessageBox.Show("el tipo:" & value)
            mNombreTipo = value
            RecuperarTipoXNombre()
        End Set
    End Property



    Private Sub RecuperarTipoXNombre()
        Dim mitipo As System.Type
        Dim ensamblado As Reflection.Assembly
        ' asignar el tipo
        If Not String.IsNullOrEmpty(mNombreTipo) Then
            Try
                Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(mNombreTipo, ensamblado, mitipo)
            Catch ex As Exception
                System.Windows.Forms.MessageBox.Show(ex.Message)
                Try
                    Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipoxRuta("D:\Signum\Framework\IU\MV2\Ensamblados\UsuariosDN.dll", mNombreTipo, ensamblado, mitipo)
                Catch ex2 As Exception
                    System.Windows.Forms.MessageBox.Show(ex2.Message)

                    Debug.WriteLine("Error: tipo no encontrado")
                End Try
            End Try

        End If

        mTipoEntidad = mitipo
        If mTipoEntidad Is Nothing Then
            'System.Windows.Forms.MessageBox.Show("es nothing")

        Else
            'System.Windows.Forms.MessageBox.Show(mTipoEntidad.ToString)

        End If
    End Sub


    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property TipoEntidad() As System.Type
        Get
            Return Me.mTipoEntidad
        End Get
        Set(ByVal value As System.Type)
            If Not mTipoEntidad Is value Or mInstanciaVinc Is Nothing Then

                mTipoEntidad = value
                'If mTipoEntidad IsNot Nothing Then
                '    mNombreTipo = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarNombreBusquedaTipo(mTipoEntidad)
                'End If


            End If
        End Set
    End Property


    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property InstanciaMap() As MV2DN.InstanciaMapDN
        Get
            Return Me.mInstanciaMapDN
        End Get
        Set(ByVal value As MV2DN.InstanciaMapDN)
            mInstanciaMapDN = value
            If mInstanciaMapDN Is Nothing Then
                mNombreInstanciaMapDN = ""
            Else
                mNombreInstanciaMapDN = mInstanciaMapDN.Nombre
            End If
        End Set
    End Property

    Public Function GenerarMapeadoBasicoEntidad(ByVal pTipo As System.Type) As MV2DN.InstanciaMapDN
        Return MV2DN.InstanciaMapDN.CrearInstanciaMapDNBase(pTipo)

    End Function
    Public Function GenerarMapeadoBasicoEntidadDN(ByVal pTipo As System.Type) As MV2DN.InstanciaMapDN
        Return MV2DN.InstanciaMapDN.CrearInstanciaMapDNBaseDN(pTipo)

    End Function
    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property NombreInstanciaMap() As String
        Get
            Return mNombreInstanciaMapDN
        End Get
        Set(ByVal value As String)
            If Not mNombreInstanciaMapDN = value Then
                mNombreInstanciaMapDN = value
                ' Me.Poblar()
            End If

        End Set
    End Property



    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property DN() As Object Implements IctrlBasicoDN.DN
        Get
            Return mDn
        End Get
        Set(ByVal value As Object)
            mDn = value
            If Not mDn Is Nothing Then
                Me.TipoEntidad = mDn.GetType
            End If

            Me.Vincular(mDn)
        End Set
    End Property


    Private Sub ControlDeDimension(ByVal altoHijosContenidos As Integer)
        If Me.Dock = DockStyle.Fill Then
            'Beep()
        Else

            Dim anchomaximo As Integer = Me.mInstanciaMapDN.AnchoMaximo

            If anchomaximo = 0 Then
                Me.Width = Me.Parent.Width ' el ancho de mi padre 
            Else
                Me.MaximumSize = New System.Drawing.Size(anchomaximo, 0)
                If Me.ParentForm.Width <= anchomaximo Then
                    Me.Width = Me.ParentForm.Width
                Else
                    Me.Width = anchomaximo
                End If
            End If


            ' If Me.Height < altoHijosContenidos Then
            '  Me.Parent.Height = altoHijosContenidos '+ 30 ' el marcito de abajo
            Me.Height = altoHijosContenidos '  debe ser la suma del alto de los hijos que contengo

            'End If
            If altoHijosContenidos = 0 Then
                Me.Anchor = AnchorStyles.None
            Else
                Me.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top Or AnchorStyles.Bottom
            End If


        End If
    End Sub


    Public Function CargarInstanciaMapSiNoExiste() As MV2DN.InstanciaMapDN
        ' con seguir los dfaos de mapeado (planos)
        If mInstanciaMapDN Is Nothing Then
            ' recuperar el mapeado
            If String.IsNullOrEmpty(mNombreInstanciaMapDN) Then
                InstanciaMap = Me.RecuperarInstanciaMap(mTipoEntidad) ' recupera el mapeado por defecto
            Else
                InstanciaMap = Me.RecuperarInstanciaMap(mNombreInstanciaMapDN) ' recupera el mapeado específico por nombre
            End If

        End If

        Return mInstanciaMapDN
    End Function


    ''' <summary>
    ''' crea los controles contenidos
    ''' para hacerlo requiere un objeto InstanciaVinc y para este requiere un InstanciaMapDN
    ''' de no tenerlo en el atributo mInstanciaMapDN lo solicita al mRecuperadorMap
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub Poblar() Implements IctrlDinamico.Poblar

        Me.Controls.Clear()



        CargarInstanciaMapSiNoExiste()


        If mInstanciaMapDN Is Nothing Then

            Dim milabel As New Label
            If mRecuperadorMap Is Nothing Then
                milabel.Text = "no se pudo recuperar mapeado el recuperador es nothing"
            Else
                milabel.Text = "no se pudo recuperar mapeado para el tipo "
            End If
            Me.Controls.Add(milabel)
            milabel.Dock = DockStyle.Fill

        Else


            If mTipoEntidad Is Nothing Then
                RecuperarTipoXNombre()
            End If



            If mInstanciaVinc Is Nothing Then
                mInstanciaVinc = New MV2DN.InstanciaVinc(mTipoEntidad, mInstanciaMapDN, mRecuperadorMap, Nothing)

            End If
            Me.Controls.Clear()

            ' proceso de creacion de los controles AG y vincilados a propeidad


            ControlDeDimension(0)


            Dim tamaño As Int64 = 0
            Me.Margin = New Padding(0)
            Me.Padding = New Padding(0)
            For Each miAgrupacionVinc As MV2DN.AgrupacionVinc In mInstanciaVinc.ColAgrupacionVinc
                Dim cag As ctrlAgrupacionGD
                cag = New ctrlAgrupacionGD(miAgrupacionVinc)
                Me.Controls.Add(cag)
                cag.Poblar()
                cag.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top Or AnchorStyles.Bottom

                tamaño += cag.Height
                'cag.BackColor = Color.Yellow
            Next
            Debug.WriteLine(tamaño)
            ControlDeDimension(tamaño)

            mTamañoResultanteVinculacionDN = tamaño


        End If







    End Sub


    Private Sub Vincular(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN)


        If mInstanciaMapDN Is Nothing Then
            Me.Poblar()
        End If

        If Not mInstanciaVinc Is Nothing Then
            mInstanciaVinc.DN = pEntidad

            ' refrescar los controles
            Me.DNaIUgd()

        End If

    End Sub


    Public Function RecuperarInstanciaMap(ByVal pNombreMapInstancia As String) As MV2DN.InstanciaMapDN Implements MV2DN.IRecuperadorInstanciaMap.RecuperarInstanciaMap
        'MessageBox.Show("Recupero por nombre:" & pNombreMapInstancia)
        If mRecuperadorMap Is Nothing Then
            Return Nothing
        Else

            Return Me.mRecuperadorMap.RecuperarInstanciaMap(pNombreMapInstancia)

        End If


    End Function

    Private Function RecuperarInstanciaMap(ByVal pTipo As System.Type) As MV2DN.InstanciaMapDN Implements MV2DN.IRecuperadorInstanciaMap.RecuperarInstanciaMap



        If Not pTipo Is Nothing Then

            BuscarRecuperadorMapeado(Me.Parent)

            If Not mRecuperadorMap Is Nothing Then

                Return Me.mRecuperadorMap.RecuperarInstanciaMap(pTipo)
            Else
                'Return Recuperarm()
            End If
        End If


        Return Nothing
    End Function


    Private Function BuscarRecuperadorMapeado(ByVal pContenedor As Control) As MV2DN.IRecuperadorInstanciaMap

        If mRecuperadorMap Is Nothing Then
            If pContenedor Is Nothing Then
                'Return New RecuperadorMapDiseño
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


    Public Sub IUaDNgd() Implements IctrlDinamico.IUaDNgd

        For Each micontrol As Control In Me.Controls

            Dim mictrlDinamico As IctrlDinamico

            If TypeOf micontrol Is IctrlDinamico Then
                mictrlDinamico = micontrol
                mictrlDinamico.IUaDNgd()
            End If

        Next
    End Sub

    Public Sub DNaIUgd() Implements IctrlDinamico.DNaIUgd
        For Each micontrol As Control In Me.Controls

            Dim mictrlDinamico As IctrlDinamico

            If TypeOf micontrol Is IctrlDinamico Then
                mictrlDinamico = micontrol
                mictrlDinamico.DNaIUgd()
            End If

        Next

        RaiseEvent DNaIUgdFInalizado(Me, Nothing)

    End Sub



    Public Property DatosControl() As String Implements IctrlDinamico.DatosControl
        Get

        End Get
        Set(ByVal value As String)

        End Set
    End Property

    Private Sub ctrlGD_ParentChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.ParentChanged

        'MessageBox.Show(Me.Parent Is Nothing)
        If Not String.IsNullOrEmpty(mNombreMapeadoNombreDiseñoTipoy) AndAlso Not (Me.Parent Is Nothing) Then
            'MessageBox.Show("empiexo a poblar")
            'MessageBox.Show(Me.ParentForm Is Nothing)
            'Dim ic As MV2DN.IRecuperadorInstanciaMap
            'ic = Me.ParentForm
            'MessageBox.Show(ic.RecuperarInstanciaMap(Me.TipoEntidad).ToXML)
            'Me.Poblar()
        End If


    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Public Event ControlSeleccionado(ByVal sender As Object, ByVal e As ControlSeleccioandoEventArgs) Implements IctrlDinamico.ControlSeleccionado

    Public Sub Clear()

        Me.mNombreInstanciaMapDN = ""
        Me.mInstanciaMapDN = Nothing
        Me.mNombreMapeadoNombreDiseñoTipoy = ""
        Me.mDn = Nothing
        Me.mInstanciaVinc = Nothing
        Me.mNombreTipo = ""
        Me.mTipoEntidad = Nothing
        mControlDinamicoSeleccioando = Nothing
        Me.Controls.Clear()
    End Sub

    Public ReadOnly Property ElementoVinc() As MV2DN.IVincElemento Implements IctrlDinamico.ElementoVinc
        Get
            Return Me.mInstanciaVinc
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
            If Me.mRecuperadorMap Is Nothing Then
                Me.mRecuperadorMap = BuscarPadreIctrlDinamico.IRecuperadorInstanciaMap
            End If
            Return Me.mRecuperadorMap
        End Get
        Set(ByVal value As MV2DN.IRecuperadorInstanciaMap)
            Me.mRecuperadorMap = value
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

    Public Function RecuperarControlDinamico(ByVal pElementoMap As MV2DN.ElementoMapDN) As IctrlDinamico Implements IctrlDinamico.RecuperarControlDinamico


        For Each cd As IctrlDinamico In Me.Controls
            If cd.ElementoVinc.ElementoMap Is pElementoMap Then
                Return cd
            Else
                Dim cds As IctrlDinamico = cd.RecuperarControlDinamico(pElementoMap)
                If cds IsNot Nothing Then
                    Return cds
                End If
            End If

        Next
        Return Nothing

    End Function


    Public Sub SetDN(ByVal entidad As Framework.DatosNegocio.IEntidadDN) Implements Framework.IU.IUComun.IctrlBasicoDN.SetDN
        Dim pi As Reflection.PropertyInfo = Me.InstanciaVinc.DN.GetType.GetProperty("Tarifa")

        pi.SetValue(Me.InstanciaVinc.DN, entidad, Nothing)
    End Sub
End Class



'Public Class RecuperadorMapDiseño
'    Implements MV2DN.IRecuperadorInstanciaMap

'    Public Function RecuperarInstanciaMap(ByVal pNombreMapInstancia As String) As MV2DN.InstanciaMapDN Implements MV2DN.IRecuperadorInstanciaMap.RecuperarInstanciaMap
'        'If pNombreMapInstancia = "PrincipalDN" Then



'        '    Dim instMap As MV2DN.InstanciaMapDN
'        '    Dim agMap As MV2DN.AgrupacionMapDN
'        '    Dim propMap As MV2DN.PropMapDN

'        '    instMap = New MV2DN.InstanciaMapDN
'        '    instMap.Nombre = "Principal vis1"

'        '    Dim tag As New MV2DN.TipoAgrupacionMapDN
'        '    tag.ID = "1"
'        '    tag.Nombre = "Solapa"

'        '    agMap = New MV2DN.AgrupacionMapDN(instMap, tag)
'        '    agMap.NombreVis = "Solapa1"

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "ID"
'        '    propMap.NombreVis = "codigo:"
'        '    agMap.ColPropMap.Add(propMap)

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "Nombre"
'        '    propMap.NombreVis = "NombPrincipal:"
'        '    agMap.ColPropMap.Add(propMap)

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "UsuarioDN.Nombre"
'        '    propMap.NombreVis = "NombUsuario:"
'        '    agMap.ColPropMap.Add(propMap)

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "ColRolDN"
'        '    propMap.NombreVis = "Roles"
'        '    agMap.ColPropMap.Add(propMap)

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "Nombreeeeee"
'        '    propMap.NombreVis = "NoEsta:"
'        '    agMap.ColPropMap.Add(propMap)

'        '    System.Diagnostics.Debug.WriteLine(instMap.ToXML)
'        '    Return instMap
'        'End If




'        'If pNombreMapInstancia = "RolDN" Then



'        '    Dim instMap As MV2DN.InstanciaMapDN
'        '    Dim agMap As MV2DN.AgrupacionMapDN
'        '    Dim propMap As MV2DN.PropMapDN

'        '    instMap = New MV2DN.InstanciaMapDN
'        '    instMap.Nombre = "Principal vis1"

'        '    Dim tag As New MV2DN.TipoAgrupacionMapDN
'        '    tag.ID = "1"
'        '    tag.Nombre = "Solapa"

'        '    agMap = New MV2DN.AgrupacionMapDN(instMap, tag)
'        '    agMap.NombreVis = "Solapa1"

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "ID"
'        '    propMap.NombreVis = "codigo:"
'        '    agMap.ColPropMap.Add(propMap)

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "Nombre"
'        '    propMap.NombreVis = "Nombre:"
'        '    agMap.ColPropMap.Add(propMap)


'        '    System.Diagnostics.Debug.WriteLine(instMap.ToXML)
'        '    Return instMap
'        'End If
'    End Function

'    Public Function RecuperarInstanciaMap(ByVal pTipo As System.Type) As MV2DN.InstanciaMapDN Implements MV2DN.IRecuperadorInstanciaMap.RecuperarInstanciaMap


'        'If pTipo.Name = "PrincipalDN" Then



'        '    Dim instMap As MV2DN.InstanciaMapDN
'        '    Dim agMap As MV2DN.AgrupacionMapDN
'        '    Dim propMap As MV2DN.PropMapDN

'        '    instMap = New MV2DN.InstanciaMapDN
'        '    instMap.Nombre = "Principal vis1"

'        '    Dim tag As New MV2DN.TipoAgrupacionMapDN
'        '    tag.ID = "1"
'        '    tag.Nombre = "Solapa"

'        '    agMap = New MV2DN.AgrupacionMapDN(instMap, tag)
'        '    agMap.NombreVis = "Solapa1"

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "ID"
'        '    propMap.NombreVis = "codigo:"
'        '    agMap.ColPropMap.Add(propMap)

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "Nombre"
'        '    propMap.NombreVis = "NombPrincipal:"
'        '    agMap.ColPropMap.Add(propMap)

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "UsuarioDN.Nombre"
'        '    propMap.NombreVis = "NombUsuario:"
'        '    agMap.ColPropMap.Add(propMap)

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "ColRolDN"
'        '    propMap.NombreVis = "Roles"
'        '    agMap.ColPropMap.Add(propMap)

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "Nombreeeeee"
'        '    propMap.NombreVis = "NoEsta:"
'        '    agMap.ColPropMap.Add(propMap)




'        '    System.Diagnostics.Debug.WriteLine(instMap.ToXML)
'        '    Return instMap
'        'End If




'        'If pTipo.Name = "RolDN" Then



'        '    Dim instMap As MV2DN.InstanciaMapDN
'        '    Dim agMap As MV2DN.AgrupacionMapDN
'        '    Dim propMap As MV2DN.PropMapDN

'        '    instMap = New MV2DN.InstanciaMapDN
'        '    instMap.Nombre = "Principal vis1"

'        '    Dim tag As New MV2DN.TipoAgrupacionMapDN
'        '    tag.ID = "1"
'        '    tag.Nombre = "Solapa"

'        '    agMap = New MV2DN.AgrupacionMapDN(instMap, tag)
'        '    agMap.NombreVis = "Solapa1"

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "ID"
'        '    propMap.NombreVis = "codigo:"
'        '    agMap.ColPropMap.Add(propMap)

'        '    propMap = New MV2DN.PropMapDN
'        '    propMap.NombreProp = "Nombre"
'        '    propMap.NombreVis = "Nombre:"
'        '    agMap.ColPropMap.Add(propMap)


'        '    System.Diagnostics.Debug.WriteLine(instMap.ToXML)
'        '    Return instMap
'        'End If

'    End Function



'End Class

