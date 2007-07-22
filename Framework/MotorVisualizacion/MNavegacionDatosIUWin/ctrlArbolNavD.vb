Public Class ctrlArbolNavD

    Protected nodoDeCarga As TreeNode
    Protected mRecuperadorMap As MV2DN.IRecuperadorInstanciaMap = New MV2DN.RecuperadorMapeadoXFicheroXMLAD(Framework.Configuracion.AppConfiguracion.DatosConfig("RutaCargaMapVis"))
    Protected mEntNavVincDN As MNavegacionDatosDN.EntNavVincDN
    Public mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN


    Public ReadOnly Property grid() As System.Windows.Forms.DataGridView
        Get
            Return DataGridView1
        End Get

    End Property

    Public Property RecuperadorMap() As MV2DN.IRecuperadorInstanciaMap
        Get
            Return mRecuperadorMap
        End Get
        Set(ByVal value As MV2DN.IRecuperadorInstanciaMap)
            mRecuperadorMap = value
        End Set
    End Property


    Private Sub AgregarNodo(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN, ByVal nodoPadre As TreeNode)

        Dim miEntNavVincDN As MNavegacionDatosDN.EntNavVincDN
        'Dim ln As New MNavegacionDatosLN.MNavDatosLN(Nothing, mRecurso)
        Dim ln As New MNavegacionDatosLNC.MNavDatosLNC
        miEntNavVincDN = ln.RecuperarEntNavVincDN(pEntidad, mRecuperadorMap)

        Dim nodo As TreeNode


        If nodoPadre Is Nothing Then
            nodo = New TreeNode(miEntNavVincDN.InstanciaVinc.Tipo.FullName)
            Me.TreeView1.Nodes.Add(nodo)
        Else
            nodo = New TreeNode(miEntNavVincDN.InstanciaVinc.Tipo.FullName)
            nodoPadre.Nodes.Add(nodo)
        End If



        EntNavVincDNToTreeNode(miEntNavVincDN, nodo)


    End Sub


    Private Sub AgregarNodo(ByVal pTipo As System.Type, ByVal nodoPadre As TreeNode)

        Dim miEntNavVincDN As MNavegacionDatosDN.EntNavVincDN
        'Dim ln As New MNavegacionDatosLN.MNavDatosLN(Nothing, mRecurso)
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
        BuscarrRelacionados(sender, e.Node)

    End Sub



    Private Sub BuscarrRelacionados(ByVal sender As Object, ByVal nodo As TreeNode)

        ' al hacer doble clik se deben recuperar las instancias relacionadas con la intacia referida

        If TypeOf nodo.Tag Is MNavegacionDatosDN.RelEntNavVincDN Then
            Dim RelEntNavVinc As MNavegacionDatosDN.RelEntNavVincDN
            RelEntNavVinc = nodo.Tag
            nodoDeCarga = nodo

            Me.DataGridView1.DataSource = Nothing

            Me.DataGridView1.Refresh()


            'Dim mnd As New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
            Dim mnd As New MNavegacionDatosLNC.MNavDatosLNC

            Me.DataGridView1.DataSource = mnd.RecuperarColHuellas(RelEntNavVinc)



        End If
    End Sub



    Private Sub TreeView1_NodeMouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.TreeNodeMouseClickEventArgs) Handles TreeView1.NodeMouseDoubleClick



        BuscarrRelacionados(sender, e.Node)



    End Sub




    
    
    Private Sub TreeView1_AfterSelect(ByVal sender As System.Object, ByVal e As System.Windows.Forms.TreeViewEventArgs) Handles TreeView1.AfterSelect

    End Sub
End Class
