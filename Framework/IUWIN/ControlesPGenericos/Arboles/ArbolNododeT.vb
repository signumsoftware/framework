Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Arboles
Imports System.Windows.Forms



Public Class ArbolNododeT
    Inherits MotorIU.ControlesP.BaseControlP


#Region "eventos"
    Public Event OnElementoSeleccionado(ByRef pElemento As Object)
    Public Event OnElementoLanzado(ByRef pElemnto As Object)
    Public Event BeforeSelect(ByRef ElementoSeleccionado As Object, ByVal e As System.Windows.Forms.TreeViewCancelEventArgs)
#End Region

#Region "atributos"
    Private mGestorPresentacion As IGestorPresentacionArboles
    Private mNodoPrincipal As INodoDN
    Private mElementoSeleccionado As IEntidadBaseDN
#End Region

#Region "inicializador"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
    End Sub
#End Region

#Region "propiedades"
    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property GestorPresentacion() As IGestorPresentacionArboles
        Get
            Return mGestorPresentacion
        End Get
        Set(ByVal value As IGestorPresentacionArboles)
            mGestorPresentacion = value
            If Not Me.mGestorPresentacion Is Nothing Then
                Dim miimagelist As New ImageList()
                Me.mGestorPresentacion.ExponerImagenes(miimagelist)
                Me.TreeView1.ImageList = miimagelist
            End If
            ' Me.mGestorPresentacion.ExponerImagenes(Me.ImageList1)
        End Set
    End Property

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property NodoPrincipal() As INodoDN
        Get
            If IUaDN() Then
                Return mNodoPrincipal
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As INodoDN)
            mNodoPrincipal = value
            DNaIU(mNodoPrincipal)
        End Set
    End Property

    Private Seleccionando As Boolean = False

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property ElementoSeleccionado() As IEntidadBaseDN
        Get
            Return mElementoSeleccionado
        End Get
        Set(ByVal value As IEntidadBaseDN)
            If Not Seleccionando Then
                'recorremos el arbol y, si lo encontramos, lo ponemos como seleccionado
                If value Is Nothing Then
                    Me.TreeView1.SelectedNode = Nothing
                Else
                    SeleccionarObjeto(value, Me.TreeView1.TopNode)
                End If
            End If
        End Set
    End Property

    Private Sub SeleccionarObjeto(ByVal pObjeto As IEntidadBaseDN, ByVal pNodoActual As TreeNode)
        Seleccionando = True

        Dim miclave As String = CType(pObjeto, Object).GetType.ToString & pObjeto.GUID

        Dim mitreenodecol As TreeNode() = Me.TreeView1.Nodes.Find(miclave, True)

        If Not mitreenodecol Is Nothing AndAlso mitreenodecol.GetLength(0) <> 0 Then
            Me.TreeView1.SelectedNode = mitreenodecol(0)
        End If

        Seleccionando = False
    End Sub

#End Region

#Region "Establecer y Rellenar Datos"
    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Using New AuxIU.CursorScope(Cursors.WaitCursor)
            Dim miNodo As INodoDN
            miNodo = CType(pDN, INodoDN)

            'limpiamos el treeview
            Me.TreeView1.Nodes.Clear()

            'si no tenemos un gestor de presentación nos creamos y asignamos
            'el gestor de presentación de árboles por defecto
            If Me.mGestorPresentacion Is Nothing Then
                Me.GestorPresentacion = New GestorPresentacionArbolesDefecto
            End If

            If Not Me.mNodoPrincipal Is Nothing Then
                Me.TreeView1.Nodes.Add(Me.RellenarElementos(mNodoPrincipal))
            End If

            Me.TreeView1.Refresh()
        End Using
    End Sub

    Private Function RellenarElementos(ByVal pNodo As INodoDN) As System.Windows.Forms.TreeNode
        If Not pNodo Is Nothing Then

            Dim textoamostrar As String = String.Empty
            Dim keyimagen As String = String.Empty
            Dim keyimagenseleccionada As String = String.Empty

            Me.mGestorPresentacion.GenerarElementoParaImageList(pNodo, textoamostrar, keyimagen, keyimagenseleccionada)

            'creamos el nodo padre
            'Dim mitreenode As New TreeNode(textoamostrar, indeximagen, indeximagenseleccionada)

            Dim mitreenode As New TreeNode
            mitreenode.Text = textoamostrar
            mitreenode.ImageKey = keyimagen
            mitreenode.SelectedImageKey = keyimagenseleccionada
            mitreenode.Tag = pNodo
            mitreenode.Name = CType(pNodo, Object).GetType.ToString & pNodo.GUID

            'le ponemos las hojas que tenga este nodo
            If Not pNodo.ColHojas Is Nothing Then
                For Each mientidad As IEntidadBaseDN In pNodo.ColHojas
                    Dim texto As String = ""
                    Dim keynormal As String = String.Empty
                    Dim keyselected As String = String.Empty
                    Me.mGestorPresentacion.GenerarElementoParaImageList(mientidad, texto, keynormal, keyselected)

                    'Dim mihoja As New TreeNode(texto, indexnormal, indexselected)
                    Dim mihoja As New TreeNode
                    mihoja.Text = texto
                    mihoja.ImageKey = keynormal
                    mihoja.SelectedImageKey = keyselected
                    mihoja.Tag = mientidad
                    mihoja.Name = CType(mientidad, Object).GetType.ToString & mientidad.GUID
                    mitreenode.Nodes.Add(mihoja)
                Next
            End If

            'ahora le ponemos los nodos que contenga
            If Not pNodo.Hijos Is Nothing Then
                For Each minodo As INodoDN In pNodo.Hijos
                    mitreenode.Nodes.Add(Me.RellenarElementos(minodo))
                Next
            End If

            'devolvemos el treenode formado
            Return mitreenode
        End If

        Return Nothing
    End Function

    Protected Overrides Function IUaDN() As Boolean
        Return True
    End Function
#End Region

#Region "Manejadores Eventos"

    Private Sub TreeView1_AfterSelect(ByVal sender As Object, ByVal e As System.Windows.Forms.TreeViewEventArgs) Handles TreeView1.AfterSelect
        Me.mElementoSeleccionado = e.Node.Tag
        RaiseEvent OnElementoSeleccionado(mElementoSeleccionado)
    End Sub

    Private Sub TreeView1_BeforeSelect(ByVal sender As Object, ByVal e As System.Windows.Forms.TreeViewCancelEventArgs) Handles TreeView1.BeforeSelect
        Dim elemento As Object = Nothing
        If Not e.Node Is Nothing Then
            elemento = e.Node.Tag
        End If
        RaiseEvent BeforeSelect(elemento, e)
    End Sub

    Private Sub TreeView1_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles TreeView1.KeyUp
        If e.KeyCode = Keys.Enter Then
            SeleccionarElemento()
        End If
    End Sub

    Private Sub SeleccionarElemento()
        If Not TreeView1.SelectedNode Is Nothing Then
            RaiseEvent OnElementoLanzado(TreeView1.SelectedNode.Tag)
        Else
            RaiseEvent OnElementoLanzado(Nothing)
        End If
    End Sub

    Private Sub TreeView1_MouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles TreeView1.MouseDoubleClick
        SeleccionarElemento()
    End Sub
#End Region

#Region "Métodos"

    Public Sub ExpandirArbol()
        Me.TreeView1.ExpandAll()
    End Sub

    Public Sub ColapsarArbol()
        Me.TreeView1.CollapseAll()
    End Sub

#End Region


End Class
