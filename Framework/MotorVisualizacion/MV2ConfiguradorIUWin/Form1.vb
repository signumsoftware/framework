Imports System.Reflection
Imports MotorBusquedaBasicasDN
Imports Framework.IU.IUComun

Public Class Form1

    Dim lsitaxmlDesHacer As New List(Of String)
    Dim lsitaxmlReHacer As New List(Of String)
    Dim mAssembly As Reflection.Assembly
    Dim mAssemblyControlador As Reflection.Assembly
    Dim mtipo As System.Type = Nothing

    Dim mieMapCopaido As MV2DN.IElemtoMap
    Dim misPares As List(Of ParTipoMapeado)



    'Private Sub Inicializar()

    'End Sub



    Private Sub Toxml()
        Try
            Me.txtbMapXml.Text = Me.CtrlGD1.InstanciaMap.ToXML

            If lsitaxmlDesHacer.Count = 0 OrElse lsitaxmlDesHacer.Item(lsitaxmlDesHacer.Count - 1) <> txtbMapXml.Text Then
                lsitaxmlDesHacer.Add(txtbMapXml.Text)
            End If
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try


    End Sub

    Private Sub FromXML()

 
        Try
            Dim miInstanciaMapDN As New MV2DN.InstanciaMapDN
            Dim tr As New IO.StringReader(Me.txtbMapXml.Text)
            miInstanciaMapDN.FromXML(tr)
            Me.CtrlGD1.Clear()
            Me.CtrlGD1.TipoEntidad = mtipo
            Me.CtrlGD1.InstanciaMap = miInstanciaMapDN
            Me.CtrlGD1.Poblar()
            Me.Inicializar()


            ' refrescar los controles pg
            Me.PropertyGrid1.SelectedObject = Nothing
            Me.PropertyGrid2.SelectedObject = miInstanciaMapDN

            ' refrescar el lsitado de operaciones


            Me.ListBox1.DataSource = Nothing
            Me.ListBox1.Items.Clear()
            Me.ListBox1.DisplayMember = "NombreVis"
            Me.ListBox1.DataSource = miInstanciaMapDN.ColComandoMap

            ReflejarEstructuraIElemtoMapContenedor2()


        Catch ex As Exception
            MessageBox.Show(ex.Message)

        End Try




    End Sub

 




    Private Sub CrearTipo()

        For Each tipo As System.Type In mAssembly.GetTypes
            If tipo.Name = Me.ComboBox1.Text Then
                mtipo = tipo
            End If
        Next


        ' vincular el tipo al tree
        VincualrTipoATree(mtipo, Me.TreeView1)


    End Sub

    Private Sub CrearTipoControlador()

        Dim mitipo As Type

        For Each tipo As System.Type In Me.mAssemblyControlador.GetTypes
            If tipo.Name = Me.ComboBox3.Text Then
                mitipo = tipo
            End If
        Next

        ' vincularlo a la lsita


        'For Each mi As Reflection.MethodInfo In mitipo.GetMethods

        '    mi.Name

        'Next
        Me.ListBox2.DataSource = Nothing
        Me.ListBox2.Items.Clear()
        Me.ListBox2.DisplayMember = "Name"
        Me.ListBox2.DataSource = mitipo.GetMethods


    End Sub





    Private Sub Button12_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        refrescarFromXML()
    End Sub


    Private Sub ReflejarEstructuraIElemtoMapContenedor2()
        Me.TreeView2.Nodes.Clear()
        Me.TreeView2.Nodes.Add(ReflejarEstructuraIElemtoMapContenedor(New TreeNode, Me.CtrlGD1.InstanciaMap))
        Me.TreeView2.ExpandAll()
    End Sub
    Private Function ReflejarEstructuraIElemtoMapContenedor(ByVal pNodo As TreeNode, ByVal pIElemtoMapContenedor As MV2DN.IElemtoMapContenedor) As TreeNode
        ' miInstanciaMapDN = Me.CtrlGD1.InstanciaMap
        'pNodo.Name = pIElemtoMapContenedor.NombreVis
        pNodo.Tag = pIElemtoMapContenedor
        pNodo.Text = pIElemtoMapContenedor.NombreVis & "   (" & CType(pIElemtoMapContenedor, Object).GetType.ToString & ")"
        If pIElemtoMapContenedor.ColIElemtoMap IsNot Nothing Then
            For Each miIElemtoMap As MV2DN.IElemtoMap In pIElemtoMapContenedor.ColIElemtoMap
                Dim tn As New TreeNode
                tn.Text = miIElemtoMap.NombreVis & "   (" & CType(miIElemtoMap, Object).GetType.ToString & ")"
                tn.Tag = miIElemtoMap
                pNodo.Nodes.Add(tn)

            Next
        End If

        If pIElemtoMapContenedor.ColIElemtoMapContenedor IsNot Nothing Then
            For Each miIElemtoMapContenedor As MV2DN.IElemtoMapContenedor In pIElemtoMapContenedor.ColIElemtoMapContenedor
                Dim tn As New TreeNode
                pNodo.Nodes.Add(tn)
                ReflejarEstructuraIElemtoMapContenedor(tn, miIElemtoMapContenedor)
            Next
        End If

        If TypeOf pIElemtoMapContenedor Is MV2DN.ElementoMapDN Then

            Dim miemap As MV2DN.ElementoMapDN = pIElemtoMapContenedor

            Dim Agrupadortn As New TreeNode
            pNodo.Nodes.Add(Agrupadortn)
            Agrupadortn.Text = "EntradaMapNavBuscador"
            Agrupadortn.Tag = miemap.ColEntradaMapNavBuscadorDN

            For Each miEntradaMapNavBuscador As MV2DN.EntradaMapNavBuscadorDN In miemap.ColEntradaMapNavBuscadorDN
                Dim tn As New TreeNode
                Agrupadortn.Nodes.Add(tn)
                tn.Text = miEntradaMapNavBuscador.NombreVis
                tn.Tag = miEntradaMapNavBuscador

                IncluirComandos(miEntradaMapNavBuscador, tn)


            Next


            IncluirComandos(miemap, Agrupadortn)

        End If

        Return pNodo
    End Function



    Private Sub IncluirComandos(ByVal pElemento As MV2DN.ElementoMapDN, ByVal pNodo As TreeNode)
        Dim Agrupadortn As New TreeNode
        pNodo.Nodes.Add(Agrupadortn)
        Agrupadortn.Text = "ColComandoMap"
        Agrupadortn.Tag = pElemento.ColComandoMap


        For Each miComandoMap As MV2DN.ComandoMapDN In pElemento.ColComandoMap
            Dim tn As New TreeNode
            Agrupadortn.Nodes.Add(tn)
            tn.Text = miComandoMap.NombreVis
            tn.Tag = miComandoMap
        Next


    End Sub


    Private Sub refrescarToxml()
        Me.Toxml()
        Me.FromXML()

        ' ReflejarEstructuraIElemtoMapContenedor2()


        Do While Me.lsitaxmlDesHacer.Count > 15
            Me.lsitaxmlDesHacer.RemoveAt(0)
        Loop
        Do While Me.lsitaxmlReHacer.Count > 15
            Me.lsitaxmlReHacer.RemoveAt(0)
        Loop
    End Sub
    Private Sub refrescarFromXML()
        Me.FromXML()
        'Me.Toxml()
        ' cargarPropiedades()
    End Sub



    Private Sub CtrlGD1_ControlSeleccionado(ByVal sender As Object, ByVal e As MV2Controles.ControlSeleccioandoEventArgs) Handles CtrlGD1.ControlSeleccionado

        If Me.CtrlGD1.ControlDinamicoSeleccioando Is Nothing Then
            SelecionarElementoMap(Nothing)
        Else
            SelecionarElementoMap(e.ControlSeleccioando.ElementoVinc.ElementoMap)
        End If


    End Sub



    Private Sub SelecionarElementoMap(ByVal pIElemtoMap As MV2DN.IElemtoMap)
        If pIElemtoMap Is Nothing Then
            Me.txtbNombrePropSelecinada.Text = ""
            Me.PropertyGrid1.SelectedObject = Nothing
        Else
            Me.txtbNombrePropSelecinada.Text = pIElemtoMap.NombreVis
            Me.PropertyGrid1.SelectedObject = pIElemtoMap
            Me.CtrlGD1.ControlDinamicoSeleccioando = Me.CtrlGD1.RecuperarControlDinamico(pIElemtoMap)

        End If

    End Sub


    Public Sub CrearMApeadoBasico()
        Me.CtrlGD1.Clear()
        Try
            CrearTipo()

            Me.CtrlGD1.TipoEntidad = mtipo
            Me.CtrlGD1.InstanciaMap = Me.CtrlGD1.GenerarMapeadoBasicoEntidadDN(mtipo)
            Me.CtrlGD1.Poblar()
            Me.Inicializar()
            If Not Me.CtrlGD1.InstanciaVinc Is Nothing Then
                Me.txtNombreMapeadoEspecifico.Text = Me.CtrlGD1.InstanciaVinc.Map.Nombre
                PropertyGrid2.SelectedObject = Me.CtrlGD1.InstanciaVinc.Map

            End If
        Catch ex As Exception
            MessageBox.Show(ex.ToString)

        End Try



    End Sub


    Private Sub Button3_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        CrearMApeadoBasico()
    End Sub

    Private Sub eliminar()
        Dim elmSel As MV2DN.ElementoMapDN
        elmSel = Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap
        Me.CtrlGD1.InstanciaMap.EliminarElementoMap(elmSel)

        refrescarToxml()
    End Sub

    Private Sub Button4_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        eliminar()

    End Sub




    Private Sub Button12_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button12.Click
        Me.refrescarFromXML()
    End Sub

    Private Sub Button5_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        Me.refrescarToxml()
    End Sub

    Private Sub cargarPropiedades()
        If Me.TreeView1.SelectedNode.Tag IsNot Nothing Then
            Dim pi As Reflection.PropertyInfo
            pi = Me.TreeView1.SelectedNode.Tag
            If Not TypeOf pi Is IList Then
                VincualrTipoANodo(pi.PropertyType, Me.TreeView1.SelectedNode)
            End If

        End If
    End Sub

    Private Sub Button13_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button13.Click
        cargarPropiedades()
    End Sub

    Private Sub VincualrTipoANodo(ByVal pTipo As System.Type, ByVal pNodo As TreeNode)
        pNodo.Nodes.Clear()


        For Each pi As Reflection.PropertyInfo In Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarPropiedades(pTipo)
            Dim tn As New TreeNode
            tn.Text = pi.Name
            tn.Tag = pi

            pNodo.Nodes.Add(tn)

        Next


    End Sub

    Private Sub VincualrTipoATree(ByVal pTipo As System.Type, ByVal pTreeView As Windows.Forms.TreeView)
        pTreeView.Nodes.Clear()
        Dim tn As New TreeNode
        tn.Text = pTipo.Name
        tn.Tag = pTipo
        pTreeView.Nodes.Add(tn)
        VincualrTipoANodo(pTipo, tn)

    End Sub

    'Private Sub TreeView1_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles TreeView1.Click


    '    If TypeOf Me.TreeView1.SelectedNode.Tag Is Reflection.PropertyInfo Then

    '        Dim tn As TreeNode
    '        Dim pi As Reflection.PropertyInfo
    '        tn = Me.TreeView1.SelectedNode
    '        pi = tn.Tag
    '        VincualrTipoANodo(pi.PropertyType, tn)

    '    End If




    'End Sub



    Private Function ObtenerRutaPropiedadSelecciandaArbolPropiedades() As String
        Dim nombreProp As String = ""

        If TypeOf Me.TreeView1.SelectedNode.Tag Is Reflection.PropertyInfo Then
            Dim tn As TreeNode
            Dim pi As Reflection.PropertyInfo
            tn = Me.TreeView1.SelectedNode

            Do
                pi = tn.Tag
                nombreProp = pi.Name & "." & nombreProp
                tn = tn.Parent
            Loop Until (tn.Tag Is TreeView1.Nodes(0).Tag)


            nombreProp = nombreProp.Substring(0, nombreProp.Length - 1)

        End If


        Return nombreProp
    End Function


    Private Sub TreeView1_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles TreeView1.DoubleClick



        Try





            Dim nombreProp As String = ObtenerRutaPropiedadSelecciandaArbolPropiedades()



            If Me.CtrlGD1.ControlDinamicoSeleccioando Is Nothing Then
                MessageBox.Show("Debe selecionar un elemento de destino")

            Else
                ' adicion de la propiedad al pimer contenedor del elemento selecioando
                Dim elementoContenedorMap As MV2DN.AgrupacionMapDN

                If TypeOf Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap Is MV2DN.AgrupacionMapDN Then
                    elementoContenedorMap = Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap
                Else
                    elementoContenedorMap = Me.CtrlGD1.InstanciaMap.ElementoContenedor(Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap)
                End If

                lblDestinoControl.Text = elementoContenedorMap.NombreVis

                Dim pm As New MV2DN.PropMapDN
                pm.NombreProp = nombreProp
                pm.NombreVis = nombreProp
                elementoContenedorMap.ColPropMap.Add(pm)

            End If

            Me.refrescarToxml()

        Catch ex As Exception
            MessageBox.Show("No has elejido un destino para la propiedad")
        End Try

    End Sub


    Private Sub subir()
        If Me.CtrlGD1.ControlDinamicoSeleccioando IsNot Nothing Then
            Dim elmSel As MV2DN.ElementoMapDN

            elmSel = Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap
            Me.CtrlGD1.InstanciaMap.MoverNElementoMap(-1, elmSel)
            Me.CtrlGD1.ControlDinamicoSeleccioando = Nothing
            refrescarToxml()
        End If

    End Sub


    Private Sub Button14_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button14.Click
        subir()

    End Sub


    Private Sub bajar()
        If Me.CtrlGD1.ControlDinamicoSeleccioando IsNot Nothing Then
            Dim elmSel As MV2DN.ElementoMapDN
            elmSel = Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap
            Me.CtrlGD1.InstanciaMap.MoverNElementoMap(1, elmSel)
            Me.CtrlGD1.ControlDinamicoSeleccioando = Nothing

            refrescarToxml()
        End If


    End Sub

    Private Sub Button15_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button15.Click
        bajar()
    End Sub

    Private Sub guardarMApeadoBasicoColeccion()

        guardarMApeado("BASICA-COL")

    End Sub
    Private Sub guardarMApeadoBasicoEntidad()

        guardarMApeado("BASICA-ENT")

    End Sub
    Private Sub guardarMApeado(ByVal Nombre As String)

        '  IO.File.WriteAllText(Me.txtbRutaMap.Text & "\" & Me.ComboBox1.Text & "-" & Nombre & ".xml", Me.txtbMapXml.Text, New System.Text.ASCIIEncoding)
        ' IO.File.WriteAllText(Me.txtbRutaMap.Text & "\" & Me.ComboBox1.Text & "-" & Nombre & ".xml", Me.txtbMapXml.Text)


        Dim tipo As System.Type = Me.CtrlGD1.InstanciaVinc.Tipo
        Dim vc As New Framework.TiposYReflexion.DN.VinculoClaseDN(tipo)
        Dim ruta As String
        ruta = Me.txtbRutaMap.Text & "\" & vc.NombreClase & "-" & Nombre & ".xml"



        If IO.File.Exists(ruta) Then
            If Not MessageBox.Show("Ya exite un mapeado base para la entidad queire sobreescribirlo", "Aviso", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) = Windows.Forms.DialogResult.Yes Then
                Exit Sub
            End If
            IO.File.Delete(ruta)
        End If



        'IO.File.WriteAllText(ruta, Me.txtbMapXml.Text, New System.Text.UTF8Encoding)
        IO.File.WriteAllText(ruta, Me.txtbMapXml.Text)

    End Sub
    Private Sub Button11_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button11.Click
        guardarMApeado(Me.txtbNombreFicheroMap.Text)
    End Sub


    Private Sub fijarMapeados()
        Me.FolderBrowserDialog1.ShowDialog()
        Me.txtbRutaMap.Text = Me.FolderBrowserDialog1.SelectedPath

        Dim rm As MV2DN.RecuperadorMapeadoXFicheroXMLAD
        rm = Me.CtrlGD1.RecuperadorMap
        rm.RutaDirectorioMapeados = Me.txtbRutaMap.Text
    End Sub
    Private Sub Button10_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button10.Click
        fijarMapeados()
    End Sub

    Private Sub Button16_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button16.Click
        Me.CtrlGD1.DN = Activator.CreateInstance(mtipo)
    End Sub

    Private Sub cargarElEnsambladoControlador()

        Try


            mAssemblyControlador = Reflection.Assembly.LoadFile(Me.TextBox2.Text)
            Me.ComboBox3.Items.Clear()
            Dim listatipos As Type()
            listatipos = mAssemblyControlador.GetTypes
            '  Array.Sort(listatipos)
            For Each tipo As System.Type In listatipos
                Me.ComboBox3.Items.Add(tipo.Name)
            Next
            ComboBox3.Sorted = True
        Catch ex As Exception

        End Try
    End Sub
    Private Sub cargarElEnsamblado()

        Try


            mAssembly = Reflection.Assembly.LoadFile(Me.TextBox1.Text)
            Me.ComboBox1.Items.Clear()
            Dim listatipos As Type()
            listatipos = mAssembly.GetTypes
            '  Array.Sort(listatipos)
            For Each tipo As System.Type In listatipos
                Me.ComboBox1.Items.Add(tipo.Name)
            Next
            ComboBox1.Sorted = True
        Catch ex As Exception

        End Try
    End Sub


    Public Sub CargarEnsambladoControlador()
        Me.OpenFileDialog1.InitialDirectory = TextBox2.Text
        Me.OpenFileDialog1.Filter = "*.dll"
        Me.OpenFileDialog1.ShowDialog()
        Me.TextBox2.Text = Me.OpenFileDialog1.FileName
        ' cargar el ensambblado y todos los tipos de las clases que se declaran

        cargarElEnsambladoControlador()


    End Sub

    Public Sub CargarEnsamblado()
        Me.OpenFileDialog1.InitialDirectory = TextBox1.Text
        Me.OpenFileDialog1.ShowDialog()
        Me.TextBox1.Text = Me.OpenFileDialog1.FileName
        ' cargar el ensambblado y todos los tipos de las clases que se declaran

        cargarElEnsamblado()


    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        CargarEnsamblado()

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        MapeadoPorDefecto()


    End Sub



    Private Sub MapeadoPorDefecto()

        CrearTipo()
        Me.lsitaxmlDesHacer.Clear()
        Me.lsitaxmlReHacer.Clear()
        Me.CtrlGD1.Clear()
        Me.CtrlGD1.TipoEntidad = mtipo
        Me.CtrlGD1.Poblar()
        Me.Inicializar()
        If Not Me.CtrlGD1.InstanciaVinc Is Nothing Then
            Me.txtNombreMapeadoEspecifico.Text = Me.CtrlGD1.InstanciaVinc.Map.Nombre
            PropertyGrid2.SelectedObject = Me.CtrlGD1.InstanciaVinc.Map

        End If
        Me.Toxml()

        Me.refrescarToxml()
    End Sub

    Private Sub Button9_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button9.Click


        CrearTipo()
        Me.CtrlGD1.Clear()
        Me.CtrlGD1.TipoEntidad = mtipo
        Me.CtrlGD1.InstanciaMap = Me.CtrlGD1.GenerarMapeadoBasicoEntidad(mtipo)
        Me.CtrlGD1.Poblar()
        Me.Inicializar()
        If Not Me.CtrlGD1.InstanciaVinc Is Nothing Then
            Me.txtNombreMapeadoEspecifico.Text = Me.CtrlGD1.InstanciaVinc.Map.Nombre
            PropertyGrid2.SelectedObject = Me.CtrlGD1.InstanciaVinc.Map

        End If
    End Sub

    Private Sub Button8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button8.Click

        CargarMapEspecifico()

    End Sub



    Public Sub CargarMapEspecifico()
        CrearTipo()
        Me.CtrlGD1.Clear()
        Me.OpenFileDialog2.InitialDirectory = Me.txtbRutaMap.Text

        If String.IsNullOrEmpty(Me.txtNombreMapeadoEspecifico.Text) Then
            Me.OpenFileDialog2.ShowDialog()
            Me.txtNombreMapeadoEspecifico.Text = IO.Path.GetFileNameWithoutExtension(Me.OpenFileDialog2.FileName)

        End If
        Me.CtrlGD1.InstanciaVinc = Nothing
        Me.CtrlGD1.InstanciaMap = Nothing

        Me.CtrlGD1.TipoEntidad = mtipo
        Me.CtrlGD1.NombreInstanciaMap = Me.txtNombreMapeadoEspecifico.Text '.Split("-")(0)
        Me.CtrlGD1.Poblar()
        Me.Inicializar()
        If Me.CtrlGD1 IsNot Nothing AndAlso Me.CtrlGD1.InstanciaVinc IsNot Nothing Then
            PropertyGrid2.SelectedObject = Me.CtrlGD1.InstanciaVinc.Map

        End If
    End Sub

    Private Sub SubMapear()
        'Debug.WriteLine(Me.CtrlGD1.ControlDinamicoSeleccioando)


        Dim pv As MV2DN.PropVinc
        pv = Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc

        Me.OpenFileDialog2.InitialDirectory = Me.txtbRutaMap.Text


        Me.OpenFileDialog2.ShowDialog()
        pv.Map.DatosControlAsignado = IO.Path.GetFileNameWithoutExtension(Me.OpenFileDialog2.FileName)

        refrescarToxml()


        ' Me.CtrlGD1.ControlDinamicoSeleccioando
    End Sub

    Private Sub btnSubMapeado_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSubMapeado.Click
        SubMapear()
    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click
        Toxml()
    End Sub

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button7.Click
        FromXML()
    End Sub

    Private Sub Button17_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button17.Click
        Me.guardarMApeadoBasicoEntidad()
    End Sub

    Private Sub Button18_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button18.Click
        Me.guardarMApeadoBasicoColeccion()
    End Sub

    Private Sub Form1_Load_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        'cargarElEnsamblado()
        Try

            ' cargar los elemtnos
            CargarPares()


            Me.ComboBox2.DataSource = [Enum].GetValues(GetType(MV2DN.TiposAgrupacionMap))

            If Not Framework.Configuracion.AppConfiguracion.DatosConfig Is Nothing AndAlso Framework.Configuracion.AppConfiguracion.DatosConfig.ContainsKey("RutaCargaMapVis") Then

                Dim directorio As String = Framework.Configuracion.AppConfiguracion.DatosConfig("RutaCargaMapVis")
                Me.CtrlGD1.RecuperadorMap = New MV2DN.RecuperadorMapeadoXFicheroXMLAD(directorio)
                Me.txtbRutaMap.Text = directorio

            Else
                Dim directorio As String = "D:\Signum\Signum\Mapeados"
                Me.txtbRutaMap.Text = directorio
                Me.CtrlGD1.RecuperadorMap = New MV2DN.RecuperadorMapeadoXFicheroXMLAD(directorio)
            End If
        Catch ex As Exception

        End Try


    End Sub

    Private Sub CargarPares()
        Dim filest As IO.FileStream
        Try

            filest = New IO.FileStream(Application.StartupPath & "\colParesTipoMapeado.xml", IO.FileMode.Open)
            Dim xmlf As System.Xml.Serialization.XmlSerializer
            xmlf = New System.Xml.Serialization.XmlSerializer(GetType(List(Of ParTipoMapeado)))
            misPares = xmlf.Deserialize(filest)

        Catch ex As Exception
            Beep()
        Finally
            If filest IsNot Nothing Then
                filest.Close()
                filest.Dispose()
            End If

        End Try
        If misPares Is Nothing Then
            misPares = New List(Of ParTipoMapeado)
        End If

        Me.ListBox3.DataSource = Nothing
        Me.ListBox3.DisplayMember = "Mapeado"
        Me.ListBox3.DataSource = misPares
        Me.ListBox3.Refresh()
    End Sub



    Private Sub GuardarPares()
        Dim filest As IO.FileStream
        Try

            filest = New IO.FileStream(Application.StartupPath & "\colParesTipoMapeado.xml", IO.FileMode.OpenOrCreate)
            Dim xmlf As System.Xml.Serialization.XmlSerializer
            xmlf = New System.Xml.Serialization.XmlSerializer(GetType(List(Of ParTipoMapeado)))
            xmlf.Serialize(filest, misPares)

        Catch ex As Exception
            Beep()
        Finally
            If filest IsNot Nothing Then
                filest.Close()
                filest.Dispose()
            End If

        End Try


    End Sub


    Private Sub Button19_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button19.Click



        If Me.CtrlGD1.ControlDinamicoSeleccioando Is Nothing Then
            MessageBox.Show("debe selecionar un elemento contenedor")

        Else
            Dim elementoContenedorMap As MV2DN.AgrupacionMapDN

            If TypeOf Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap Is MV2DN.AgrupacionMapDN Then
                elementoContenedorMap = Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap
            Else
                elementoContenedorMap = Me.CtrlGD1.InstanciaMap.ElementoContenedor(Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap)
            End If

            Dim ta As New MV2DN.TipoAgrupacionMapDN
            ta.Nombre = Me.ComboBox2.Text

            Dim ag As MV2DN.AgrupacionMapDN

            ag = New MV2DN.AgrupacionMapDN
            ag.TipoAgrupacionMap = ta

            ' quien contine a aquien

            elementoContenedorMap.AgruparCon(ag)

            Me.refrescarToxml()

        End If






    End Sub

    Private Sub btnNavegarABuscador_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNavegarABuscador.Click

        ' crear el paquete
        'Dim paquete As New Hashtable
        ''paquete.Add("NombreInstanciaMapVis", Me.CtrlGD1.InstanciaVinc.Map.DatosNavegacion)
        ''If Me.CtrlGD1.DN Is Nothing Then
        ''    paquete.Add("tipo", Me.CtrlGD1.TipoEntidad)
        ''Else
        ''    paquete.Add("DN", Me.CtrlGD1.DN)
        ''End If

        'paquete.Add("tipo", Me.CtrlGD1.TipoEntidad)
        'Me.cMarco.Navegar("Filtro", Me.ParentForm, Nothing, TipoNavegacion.Normal, Me.GenerarDatosCarga, paquete)


        Dim paquete As New Hashtable
        Dim ParametroCargaEstructuraDN As ParametroCargaEstructuraDN

        Try
            ParametroCargaEstructuraDN = Me.CtrlGD1.IGestorPersistencia.RecuperarParametroBusqueda(Me.CtrlGD1.ElementoVinc)
        Catch ex As Exception
            Me.MostrarError(ex)
            Exit Sub
        End Try

        Try
            ParametroCargaEstructuraDN.TipodeEntidad = Me.CtrlGD1.TipoEntidad

            Dim miPaqueteFormularioBusqueda As New MotorBusquedaDN.PaqueteFormularioBusqueda
            miPaqueteFormularioBusqueda.ParametroCargaEstructura = ParametroCargaEstructuraDN
            miPaqueteFormularioBusqueda.MultiSelect = False
            miPaqueteFormularioBusqueda.Agregable = True 'me.CtrlGD1.InstanciaVinc.Map.insta

            paquete.Add("PaqueteFormularioBusqueda", miPaqueteFormularioBusqueda)


            cMarco.Navegar("Filtro", Me, Nothing, TipoNavegacion.Normal, Me.GenerarDatosCarga, paquete)

        Catch ex As Exception
            Me.MostrarError(ex)
        End Try






    End Sub

    Private Sub btnNavegarAFG_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNavegarAFG.Click
        ' crear el paquete
        Dim paquete As New Hashtable
        paquete.Add("NombreInstanciaMapVis", Me.CtrlGD1.InstanciaVinc.Map.DatosBusqueda)
        If Me.CtrlGD1.DN Is Nothing Then
            paquete.Add("tipo", Me.CtrlGD1.TipoEntidad)
        Else
            paquete.Add("DN", Me.CtrlGD1.DN)
        End If

        Me.cMarco.Navegar("FG", Me.ParentForm, Nothing, TipoNavegacion.Normal, Me.GenerarDatosCarga, paquete)

    End Sub

    Private Sub btnMapearTipo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnMapearTipo.Click
        If Me.TreeView1.SelectedNode.Tag IsNot Nothing Then
            Dim pi As Reflection.PropertyInfo
            pi = Me.TreeView1.SelectedNode.Tag

            Dim vc As New Framework.TiposYReflexion.DN.VinculoClaseDN(pi.PropertyType)

            Dim dir As String
            If Me.OpenFileDialog1.InitialDirectory.ToString.Contains(".dll") Then
                dir = IO.Path.GetDirectoryName(Me.OpenFileDialog1.InitialDirectory.ToString)
            Else
                dir = Me.OpenFileDialog1.InitialDirectory.ToString
            End If


            Me.TextBox1.Text = dir & "\" & vc.NombreEnsambladoCorto & ".dll"
            cargarElEnsamblado()
            Me.ComboBox1.Text = vc.NombreClase
        End If
    End Sub

    Private Sub Button20_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button20.Click
        Me.CargarEnsambladoControlador()
    End Sub

    Private Sub ComboBox3_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComboBox3.SelectedIndexChanged
        CrearTipoControlador()

    End Sub

    Private Sub ListBox2_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles ListBox2.DoubleClick

        ' se añaden comandos no operaciones
        Dim CM As New MV2DN.ComandoMapDN
        Dim vm As New Framework.TiposYReflexion.DN.VinculoMetodoDN(Me.ListBox2.SelectedItem)
        CM.NombreVis = vm.NombreMetodo
        CM.Nombre = vm.NombreMetodo
        CM.VinculoMetodo = vm

        Dim miInstanciaMapDN As MV2DN.ElementoMapDN ' = Me.CtrlGD1.InstanciaVinc.Map

        ' asignacion del elemento destino
        If Me.PropertyGrid1.SelectedObject Is Nothing Then
            miInstanciaMapDN = Me.CtrlGD1.InstanciaVinc.Map
        Else
            miInstanciaMapDN = Me.PropertyGrid1.SelectedObject
        End If
        '    Me.CtrlGD1.ControlDinamicoSeleccioando = Me.CtrlGD1.RecuperarControlDinamico(pIElemtoMap)



        miInstanciaMapDN.ColComandoMap.Add(CM)
        Me.refrescarToxml()





    End Sub




    Private Sub TreeView1_AfterSelect(ByVal sender As System.Object, ByVal e As System.Windows.Forms.TreeViewEventArgs) Handles TreeView1.AfterSelect
        Me.TxtRutaPropiedad.Text = "@" & ObtenerRutaPropiedadSelecciandaArbolPropiedades()
    End Sub


    Private Sub cortarNodoMap_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cortarNodoMap.Click
        ' se pretende sacar un elemento de mapado de su contenedor
        Try


            Dim nodo As TreeNode = Me.TreeView2.SelectedNode

            Dim ie As MV2DN.IElemtoMap = nodo.Tag
            mieMapCopaido = ie

            Dim iec As MV2DN.IElemtoMapContenedor = nodo.Parent.Tag
            iec.EliminarElementoMap(ie)

            Me.refrescarToxml()

            '        Me.ReflejarEstructuraIElemtoMapContenedor2()
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Private Sub PegarENNodoMap_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PegarENNodoMap.Click

        If mieMapCopaido IsNot Nothing Then
            Dim nodo As TreeNode = Me.TreeView2.SelectedNode
            Dim iec As MV2DN.IElemtoMapContenedor = nodo.Tag
            iec.AñadirElementoMap(Me.mieMapCopaido)
            Me.refrescarToxml()

        End If


    End Sub

    Private Sub PegarSobreNodoMap_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PegarSobreNodoMap.Click
        If mieMapCopaido IsNot Nothing Then
            Dim nodo As TreeNode = Me.TreeView2.SelectedNode
            Dim elemento As MV2DN.IElemtoMap = nodo.Tag
            Dim iec As MV2DN.IElemtoMapContenedor = nodo.Parent.Tag
            iec.AñadirElementoMapEnRelacion(Me.mieMapCopaido, elemento, MV2DN.Posicion.Antes)
            Me.refrescarToxml()

        End If
    End Sub

    Private Sub PegarBajoNodoMap_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PegarBajoNodoMap.Click
        If mieMapCopaido IsNot Nothing Then
            Dim nodo As TreeNode = Me.TreeView2.SelectedNode
            Dim elemento As MV2DN.IElemtoMap = nodo.Tag
            Dim iec As MV2DN.IElemtoMapContenedor = nodo.Parent.Tag
            iec.AñadirElementoMapEnRelacion(Me.mieMapCopaido, elemento, MV2DN.Posicion.Despues)
            Me.refrescarToxml()

        End If
    End Sub

    Private Sub TreeView2_AfterSelect(ByVal sender As System.Object, ByVal e As System.Windows.Forms.TreeViewEventArgs) Handles TreeView2.AfterSelect

    End Sub

    Private Sub TreeView2_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles TreeView2.DoubleClick

        If TreeView2.SelectedNode IsNot Nothing Then
            If TypeOf TreeView2.SelectedNode.Tag Is MV2DN.IElemtoMap Then
                SelecionarElementoMap(TreeView2.SelectedNode.Tag)
            End If


        End If
    End Sub

    Private Sub TabPage5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TabPage5.Click

    End Sub

    'Private Sub Button21_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button21.Click
    '    Actualizar()
    'End Sub

    Private Sub Actualizar()
        Toxml()
        FromXML()
    End Sub

    Private Sub SubAñadir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SubAñadir.Click

        If Me.CtrlGD1.ControlDinamicoSeleccioando Is Nothing Then
            MessageBox.Show("debe selecionar un elemento contenedor")

        Else
            Dim elementoContenedorMap As MV2DN.AgrupacionMapDN

            If TypeOf Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap Is MV2DN.AgrupacionMapDN Then
                elementoContenedorMap = Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap
            Else
                elementoContenedorMap = Me.CtrlGD1.InstanciaMap.ElementoContenedor(Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap)
            End If

            Dim ta As New MV2DN.TipoAgrupacionMapDN
            ta.Nombre = Me.ComboBox2.Text

            Dim ag As MV2DN.AgrupacionMapDN

            ag = New MV2DN.AgrupacionMapDN
            ag.TipoAgrupacionMap = ta

            ' quien contine a aquien
            ag.InstanciaContenedora = elementoContenedorMap.InstanciaContenedora
            elementoContenedorMap.ColAgrupacionMap.Add(ag)

            Me.refrescarToxml()

        End If



    End Sub

    Private Sub Button21_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button21.Click

        If lsitaxmlDesHacer.Count = 0 Then
            'Beep()
        Else
            If txtbMapXml.Text = lsitaxmlDesHacer.Item(lsitaxmlDesHacer.Count - 1) Then

                lsitaxmlReHacer.Add(lsitaxmlDesHacer.Item(lsitaxmlDesHacer.Count - 1))
            End If
            txtbMapXml.Text = lsitaxmlDesHacer.Item(lsitaxmlDesHacer.Count - 1)
            lsitaxmlReHacer.Add(txtbMapXml.Text)
            lsitaxmlDesHacer.RemoveAt(lsitaxmlDesHacer.Count - 1)
            Me.refrescarFromXML()
        End If

    End Sub

    Private Sub Button23_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button23.Click

        If lsitaxmlReHacer.Count = 0 Then
            'Beep()
        Else
            txtbMapXml.Text = lsitaxmlReHacer.Item(lsitaxmlReHacer.Count - 1)
            lsitaxmlDesHacer.Add(txtbMapXml.Text)
            lsitaxmlReHacer.RemoveAt(lsitaxmlReHacer.Count - 1)
            Me.refrescarFromXML()
        End If

    End Sub

    Private Sub ListBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedIndexChanged
        Me.PropertyGrid3.SelectedObject = Nothing
        Me.PropertyGrid3.SelectedObject = ListBox1.SelectedItem
    End Sub

    Private Sub Button24_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button24.Click


        Dim miInstanciaMapDN As MV2DN.InstanciaMapDN = Me.CtrlGD1.InstanciaVinc.Map
        miInstanciaMapDN.ColComandoMap.Remove(Me.ListBox1.SelectedItem)
        Me.refrescarToxml()
    End Sub



    Private Sub Button25_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button25.Click

        Dim pt As New ParTipoMapeado
        pt.Ensamblado = Me.TextBox1.Text
        pt.Tipo = Me.ComboBox1.Text
        pt.Mapeado = txtNombreMapeadoEspecifico.Text
        Me.misPares.Add(pt)

        Me.ListBox3.DataSource = Nothing
        Me.ListBox3.DisplayMember = "Mapeado"
        Me.ListBox3.DataSource = misPares
        Me.ListBox3.Refresh()

        Me.GuardarPares()

    End Sub

  
 
    Private Sub Button26_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button26.Click
        Try
            Dim pt As ParTipoMapeado = ListBox3.SelectedItem
            Me.misPares.Remove(pt)
            Me.ListBox3.DataSource = Nothing
        Catch ex As Exception

        End Try


        Me.GuardarPares()

        Me.ListBox3.DisplayMember = "Mapeado"
        Me.ListBox3.DataSource = misPares
        Me.ListBox3.Refresh()

    End Sub

    Private Sub ListBox3_MouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles ListBox3.MouseDoubleClick
        Dim pt As ParTipoMapeado = ListBox3.SelectedItem

        Me.TextBox1.Text = pt.Ensamblado
        Me.ComboBox1.Text = pt.Tipo
        cargarElEnsamblado()
        txtNombreMapeadoEspecifico.Text = pt.Mapeado
        Me.CargarMapEspecifico()
    End Sub

   

    Private Sub Button27_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button27.Click

        Try

            Dim nombreProp As String = TextBox3.Text

            If Me.CtrlGD1.ControlDinamicoSeleccioando Is Nothing Then
                MessageBox.Show("Debe selecionar un elemento de destino")

            Else
                ' adicion de la propiedad al pimer contenedor del elemento selecioando
                Dim elementoContenedorMap As MV2DN.AgrupacionMapDN

                If TypeOf Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap Is MV2DN.AgrupacionMapDN Then
                    elementoContenedorMap = Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap
                Else
                    elementoContenedorMap = Me.CtrlGD1.InstanciaMap.ElementoContenedor(Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap)
                End If

                lblDestinoControl.Text = elementoContenedorMap.NombreVis

                Dim pm As New MV2DN.PropMapDN
                pm.Virtual = True
                pm.NombreProp = ""
                pm.NombreVis = nombreProp
                elementoContenedorMap.ColPropMap.Add(pm)

            End If

            Me.refrescarToxml()

        Catch ex As Exception
            MessageBox.Show("No has elejido un destino para la propiedad")
        End Try
    End Sub

    Private Sub Button28_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button28.Click
        Try

            Dim nombreProp As String = TextBox3.Text

            If Me.CtrlGD1.ControlDinamicoSeleccioando Is Nothing Then
                MessageBox.Show("Debe selecionar un elemento de destino")

            Else
                ' adicion de la propiedad al pimer contenedor del elemento selecioando
                Dim elementoContenedorMap As MV2DN.AgrupacionMapDN

                If TypeOf Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap Is MV2DN.AgrupacionMapDN Then
                    elementoContenedorMap = Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap
                Else
                    elementoContenedorMap = Me.CtrlGD1.InstanciaMap.ElementoContenedor(Me.CtrlGD1.ControlDinamicoSeleccioando.ElementoVinc.ElementoMap)
                End If

                lblDestinoControl.Text = elementoContenedorMap.NombreVis

                Dim pm As New MV2DN.PropMapDN
                pm.Virtual = True
                pm.NombreProp = ""
                pm.NombreVis = nombreProp
                pm.ControlAsignado = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.TipoToString(GetType(MV2Controles.CtrlBusquedaGD))
                elementoContenedorMap.ColPropMap.Add(pm)

            End If

            Me.refrescarToxml()

        Catch ex As Exception
            MessageBox.Show("No has elejido un destino para la propiedad")
        End Try
    End Sub

    Private Sub ListBox2_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox2.SelectedIndexChanged

    End Sub
End Class


<Serializable()> Public Class ParTipoMapeado
    Protected mEnsamblado As String
    Protected mTipo As String
    Protected mMapeado As String
    Public Property Ensamblado() As String
        Get
            Return Me.mEnsamblado
        End Get
        Set(ByVal value As String)
            Me.mEnsamblado = value
        End Set
    End Property
    Public Property Tipo() As String
        Get
            Return Me.mTipo
        End Get
        Set(ByVal value As String)
            Me.mTipo = value
        End Set
    End Property
    Public Property Mapeado() As String
        Get
            Return Me.mMapeado
        End Get
        Set(ByVal value As String)
            Me.mMapeado = value
        End Set
    End Property
End Class
