Imports Framework.LogicaNegocios.Transacciones
Imports Framework.IU.IUComun

Public Class ctrlArbolNavD2

    Public Event NodoDobleClik(ByVal sender As Object, ByVal e As ctrlArbolNavD2EventArgs)

    Protected nodoDeCarga As TreeNode
    Protected mRecuperadorMap As MV2DN.IRecuperadorInstanciaMap = New MV2DN.RecuperadorMapeadoXFicheroXMLAD(Framework.Configuracion.AppConfiguracion.DatosConfig("RutaCargaMapVis"))
    Protected mEntNavVincDN As MNavegacionDatosDN.EntNavVincDN
    Public mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN




    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(True)> Public Property RecuperadorMap() As MV2DN.IRecuperadorInstanciaMap
        Get
            Return mRecuperadorMap
        End Get
        Set(ByVal value As MV2DN.IRecuperadorInstanciaMap)
            mRecuperadorMap = value
        End Set
    End Property



    Private Sub AgregarMapeadoTipoRelacionado(ByVal pEntradaMapNavBuscador As MV2DN.EntradaMapNavBuscadorDN, ByVal nodoPadre As TreeNode)


        Dim nodo As TreeNode
        Dim texto As String = pEntradaMapNavBuscador.Nombrevis

        If nodoPadre Is Nothing Then
            nodo = New TreeNode(texto)
            Me.TreeView1.Nodes.Add(nodo)
        Else
            nodo = New TreeNode(texto)
            nodoPadre.Nodes.Add(nodo)
        End If

        nodo.Tag = pEntradaMapNavBuscador

    End Sub

    Private Sub AgregarNodo(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN, ByVal nodoPadre As TreeNode)




        ' Using tr As New Transaccion ' TODO: provisional hay que cambiarlo por los correspondientes AS


        Dim miEntNavVincDN As MNavegacionDatosDN.EntNavVincDN
        ' Dim ln As New MNavegacionDatosLN.MNavDatosLN(Transaccion.Actual, Recurso.Actual)
        Dim ln As New MNavegacionDatosLNC.MNavDatosLNC


        Try
            miEntNavVincDN = ln.RecuperarEntNavVincDN(pEntidad, mRecuperadorMap)
        Catch ex As Exception
            'Debug.WriteLine(ex.Message)
            'Beep()
            Exit Sub
        End Try

        Dim nodo As TreeNode
        Dim texto As String = miEntNavVincDN.InstanciaVinc.Map.NombreVis

        If miEntNavVincDN.InstanciaVinc.Vinculada Then
            texto += " " & miEntNavVincDN.InstanciaVinc.DN.ToString
        End If



        If nodoPadre Is Nothing Then
            nodo = New TreeNode(texto)
            Me.TreeView1.Nodes.Add(nodo)
        Else
            nodo = New TreeNode(texto)
            nodoPadre.Nodes.Add(nodo)
        End If



        EntNavVincDNToTreeNode(miEntNavVincDN, nodo)

        '    tr.Confirmar()

        'End Using






    End Sub


    Private Sub AgregarNodo(ByVal pTipo As System.Type, ByVal nodoPadre As TreeNode)

        Dim miEntNavVincDN As MNavegacionDatosDN.EntNavVincDN
        ' Dim ln As New MNavegacionDatosLN.MNavDatosLN(Nothing, mRecurso)
        Dim ln As New MNavegacionDatosLNC.MNavDatosLNC

        miEntNavVincDN = ln.RecuperarEntNavVincDN(pTipo, mRecuperadorMap)

        If nodoPadre Is Nothing Then
            nodoPadre = New TreeNode(miEntNavVincDN.InstanciaVinc.Tipo.FullName)
            Me.TreeView1.Nodes.Add(nodoPadre)
        End If

        EntNavVincDNToTreeNode(miEntNavVincDN, nodoPadre)

    End Sub



    Private Function EncontrarNodoObjetivo(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN, ByVal pNodoInicial As TreeNode) As TreeNode


        If pNodoInicial Is Nothing Then

            If Me.TreeView1.Nodes.Count = 0 Then
                Return Nothing
            End If

            pNodoInicial = Me.TreeView1.Nodes(0)

        End If

        Dim entidad As Framework.DatosNegocio.EntidadDN

        If TypeOf pNodoInicial.Tag Is MNavegacionDatosDN.RelEntNavVincDN Then

            Dim relentnav As MNavegacionDatosDN.RelEntNavVincDN = pNodoInicial.Tag
            entidad = relentnav.PropVinc.InstanciaVincReferida.DN
        Else

            Dim EntNavVinc As MNavegacionDatosDN.EntNavVincDN = pNodoInicial.Tag
            entidad = EntNavVinc.InstanciaVinc.DN

        End If


        If entidad.GUID = pEntidad.GUID Then
            Return pNodoInicial
        Else
            For Each trn As TreeNode In pNodoInicial.Nodes
                Dim nodo As TreeNode = EncontrarNodoObjetivo(pEntidad, trn)

                If Not nodo Is Nothing Then
                    Return nodo
                End If

            Next
        End If



    End Function


    Public Function VincularEntidad(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN)

        Dim nodoDestino As TreeNode = EncontrarNodoObjetivo(pEntidad, Nothing)


        If nodoDestino Is Nothing Then
            AgregarNodo(pEntidad, nodoDeCarga)
        Else

            Me.TreeView1.SelectedNode = nodoDestino
            Me.TreeView1.Select()
        End If


    End Function

    Public Function VincularTipo(ByVal ptipo As Type)
        AgregarNodo(ptipo, Nothing)
    End Function

    Public Function VincularBusquedaATipo(ByVal pcolEntradaMapNavBuscador As MV2DN.ColEntradaMapNavBuscadorDN)

        For Each miEntradaMapNavBuscador As MV2DN.EntradaMapNavBuscadorDN In pcolEntradaMapNavBuscador
            AgregarMapeadoTipoRelacionado(miEntradaMapNavBuscador, Nothing)

        Next

    End Function
    Private Sub EntNavVincDNToTreeNode(ByVal pEntNavVincDN As MNavegacionDatosDN.EntNavVincDN, ByVal nodoPadre As TreeNode)






        If pEntNavVincDN Is Nothing Then


            Throw New ApplicationException("no dispone de un mapeado de visivilidad para ea entida")

        Else

            nodoPadre.Tag = pEntNavVincDN

            For Each re As MNavegacionDatosDN.RelEntNavVincDN In pEntNavVincDN.ColREentNavVincDN
                Dim sufijoCol As String = ""
                If re.RelacionEntidadesNav.Cardinalidad = MNavegacionDatosDN.CardinalidadRelacion.CeroAUno AndAlso re.DireccionLectura = MNavegacionDatosDN.DireccionesLectura.Directa Then


                    'Dim mitn As New TreeNode(re.NombreVis & sufijoCol)
                    'mitn.Tag = re
                    'nodoPadre.Nodes.Add(mitn)
                    AñadirNodoANodo(nodoPadre, re, re.NombreVis & sufijoCol)

                Else

                    sufijoCol = "(s)"
                    If re.DireccionLectura = MNavegacionDatosDN.DireccionesLectura.Directa Then


                        'Dim mitn As New TreeNode(re.NombreVis & sufijoCol)
                        'mitn.Tag = re
                        'nodoPadre.Nodes.Add(mitn)
                        AñadirNodoANodo(nodoPadre, re, re.NombreVis & sufijoCol)
                    Else

                        'Dim mitn As New TreeNode(re.PropVinc.InstanciaVinc.Map.NombreVis & sufijoCol)
                        'mitn.Tag = re
                        'nodoPadre.Nodes.Add(mitn)
                        AñadirNodoANodo(nodoPadre, re, re.PropVinc.InstanciaVinc.Map.NombreVis & sufijoCol)
                    End If



                End If


            Next
        End If


    End Sub
    ''' <summary>
    ''' evita que se añadan varias entradas de la misma propiedad
    ''' </summary>
    ''' <param name="nodoPadre"></param>
    ''' <param name="re"></param>
    ''' <remarks></remarks>
    Private Sub AñadirNodoANodo(ByVal nodoPadre As TreeNode, ByVal re As MNavegacionDatosDN.RelEntNavVincDN, ByVal nombrevis As String)



        For Each nodo As TreeNode In nodoPadre.Nodes
            Dim re2 As MNavegacionDatosDN.RelEntNavVincDN = nodo.Tag
            If re2.PropVinc.PropertyInfoVinc Is re.PropVinc.PropertyInfoVinc Then
                Exit Sub
            End If
        Next
        Dim mitn As New TreeNode(nombrevis)
        mitn.Tag = re
        nodoPadre.Nodes.Add(mitn)
    End Sub
    Private Sub TreeView1_AfterExpand(ByVal sender As Object, ByVal e As System.Windows.Forms.TreeViewEventArgs) Handles TreeView1.AfterExpand
        'BuscarrRelacionados(sender, e.Node)

    End Sub


    Private Sub BuscarrRelacionados(ByVal sender As Object, ByVal nodo As TreeNode)


        Dim paquete As Hashtable
        Dim formulariop As MotorIU.FormulariosP.IFormularioP = Me.ParentForm







        If TypeOf nodo.Tag Is MNavegacionDatosDN.EntNavVincDN Then

            Dim EntNavVinc As MNavegacionDatosDN.EntNavVincDN = nodo.Tag
            Dim e As New ctrlArbolNavD2EventArgs
            e.huella = New Framework.DatosNegocio.HEDN
            e.huella.AsignarEntidadReferida(EntNavVinc.InstanciaVinc.DN)
            RaiseEvent NodoDobleClik(Me, e)
            Exit Sub

        End If

        If TypeOf nodo.Tag Is MV2DN.EntradaMapNavBuscadorDN Then

            Dim icd As Framework.IU.IUComun.IctrlBasicoDN = formulariop

            Dim emap As MV2DN.EntradaMapNavBuscadorDN = nodo.Tag

            'Dim colvc As New List(Of ValorCampo)

            'Dim vc As New ValorCampo
            'vc.NombreCampo = emap.NombreCampo
            'vc.Operador = MotorBusquedaDN.OperadoresAritmeticos.igual
            'vc.Valor = emap.RecuperarValor(icd.DN)
            'colvc.Add(vc)

            'Dim pb As New ParametroCargaEstructuraDN()
            'pb.CargarDesdeTexto(emap.NombreVista)
            'pb.TipodeEntidad = emap.Tipo
            'pb.Titulo = emap.Nombrevis

            'MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(formulariop, emap.Tipo, colvc, pb)

            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(formulariop, emap, icd.DN)
            Exit Sub
        End If


        If TypeOf nodo.Tag Is MNavegacionDatosDN.RelEntNavVincDN Then
            Dim RelEntNavVinc As MNavegacionDatosDN.RelEntNavVincDN
            RelEntNavVinc = nodo.Tag
            nodoDeCarga = nodo

            Dim mnd As New MNavegacionDatosLNC.MNavDatosLNC


            Dim pr As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN
            If RelEntNavVinc.DireccionLectura = MNavegacionDatosDN.DireccionesLectura.Reversa Then
                pr = New Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN(RelEntNavVinc.PropVinc.PropertyInfoVinc, RelEntNavVinc.PropVinc.InstanciaVincReferida.DN.ID, RelEntNavVinc.PropVinc.InstanciaVincReferida.DN.GUID)
            Else

                If RelEntNavVinc.PropVinc.ValorObjetivo Is Nothing Then

                    MessageBox.Show("La entidad no esta creada")
                    Exit Sub
                Else
                    Dim e As New ctrlArbolNavD2EventArgs
                    e.huella = New Framework.DatosNegocio.HEDN
                    Dim valor As Object = RelEntNavVinc.PropVinc.ValorObjetivo
                    If valor IsNot Nothing Then
                        e.huella.AsignarEntidadReferida(RelEntNavVinc.PropVinc.ValorObjetivo)
                    End If

                    RaiseEvent NodoDobleClik(Me, e)
                    Exit Sub

                End If

            End If







            Dim tiporeferido, tipoOriginal As System.Type



            If RelEntNavVinc.DireccionLectura = MNavegacionDatosDN.DireccionesLectura.Reversa Then
                ' paquete = MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(formulariop, pr.Propiedad.ReflectedType, TipoNavegacion.Modal, pr)
                tiporeferido = pr.Propiedad.ReflectedType
            Else
                tiporeferido = pr.Propiedad.PropertyType
            End If
            tipoOriginal = tiporeferido

            ' si se trata una heulla
            If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(tiporeferido) Then
                Dim tf As Framework.TiposYReflexion.DN.FijacionDeTipoDN
                tiporeferido = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(tiporeferido, tf)
            End If



            paquete = MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(formulariop, tiporeferido, TipoNavegacion.Modal, pr)

            Dim misIds As IList(Of String)
            If paquete.ContainsKey("IDMultiple") Then
                misIds = paquete("IDMultiple")
            Else
                Dim miId As String = paquete("ID")
                misIds = New List(Of String)
                If Not String.IsNullOrEmpty(miId) Then

                    misIds.Add(miId)
                End If

            End If




            ' tratar los ides devueltos

            Select Case misIds.Count


                Case 0
                    ' no hacer nada


                Case Is = 1
                    ' añadir la huella y avisar al contenedor
                    If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(tipoOriginal) Then
                        Dim e As New ctrlArbolNavD2EventArgs
                        e.huella = Activator.CreateInstance(tipoOriginal)
                        e.huella.AsignarDatosBasicos(tiporeferido, misIds(0), "") ' TODO: debiera rellenarse el GUID

                        RaiseEvent NodoDobleClik(Me, e)
                    Else
                        Dim e As New ctrlArbolNavD2EventArgs
                        e.huella = New Framework.DatosNegocio.HEDN(tiporeferido, misIds(0), "") ' TODO: debiera rellenarse el GUID
                        RaiseEvent NodoDobleClik(Me, e)
                    End If




                Case Is > 1
                    ' añadir las huellas y cargar el primer objeto




            End Select






        End If



    End Sub



    Private Sub TreeView1_NodeMouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.TreeNodeMouseClickEventArgs) Handles TreeView1.NodeMouseDoubleClick



        BuscarrRelacionados(sender, e.Node)



    End Sub






 
    Private Sub TreeView1_AfterSelect(ByVal sender As System.Object, ByVal e As System.Windows.Forms.TreeViewEventArgs) Handles TreeView1.AfterSelect

    End Sub
End Class


Public Class ctrlArbolNavD2EventArgs
    Inherits EventArgs
    Public huella As Framework.DatosNegocio.IHuellaEntidadDN
End Class