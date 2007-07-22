Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Arboles

Public Class ArbolNododeTxLista


#Region "Atributos"
    Private mElementosLista As ArrayList

    Private mSoloLectura As Boolean
#End Region

#Region "Propiedades"

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property ElementosLista() As ArrayList
        Get
            If IUaDN() Then
                Return mElementosLista
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As ArrayList)
            mElementosLista = value
            DNaIU(mElementosLista)
        End Set
    End Property

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public WriteOnly Property NodoPrincipalArbol() As INodoDN
        Set(ByVal value As INodoDN)
            Me.ArbolNododeT1.NodoPrincipal = value
        End Set
    End Property

    Public WriteOnly Property GestorPresentacion() As IGestorPresentacion
        Set(ByVal value As IGestorPresentacion)
            Me.ArbolNododeT1.GestorPresentacion = value
        End Set
    End Property

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public WriteOnly Property SoloLectura() As Boolean
        Set(ByVal value As Boolean)
            mSoloLectura = value
            ActualizarSoloLectura()
        End Set
    End Property
#End Region

#Region "Establecer y rellenar datos"

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Dim miEntidad As Framework.DatosNegocio.IEntidadBaseDN
        Dim pElementosLista As ArrayList = CType(pDN, ArrayList)

        MyBase.DNaIU(pDN)

        Me.lbLista.Items.Clear()

        'metemos los elementos en el listbox
        If Not pElementosLista Is Nothing Then
            For Each miEntidad In pElementosLista
                Me.lbLista.Items.Add(miEntidad)
            Next
        End If

        Me.lbLista.Refresh()

    End Sub

    Protected Overrides Function IUaDN() As Boolean
        'ponemos los elementos en la colección
        If Me.mElementosLista Is Nothing Then
            Me.mElementosLista = New ArrayList
        End If

        mElementosLista.Clear()

        mElementosLista.AddRange(Me.lbLista.Items)

        Return True
    End Function

#End Region

#Region "Manjejadores Eventos"

    Private Sub cmdDesplegarArbol_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdDesplegarArbol.Click
        Try
            Me.ArbolNododeT1.ExpandirArbol()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdColapsarArbol_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdColapsarArbol.Click
        Try
            Me.ArbolNododeT1.ColapsarArbol()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdPasar1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdPasar1.Click
        Try
            AgregarNodoArbol(ArbolNododeT1.ElementoSeleccionado)
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdPasarTodos_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdPasarTodos.Click
        Try
            AgregarTodosArbol()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdQuitarTodos_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdQuitarTodos.Click
        Try
            QuitarTodosLista()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdQuitar1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdQuitar1.Click
        Try
            QuitarElementoLista()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub lbLista_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles lbLista.DoubleClick
        Try
            If Not mSoloLectura Then
                QuitarElementoLista()
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub ArbolNododeT1_DoubleClick(ByRef pElemnto As Object) Handles ArbolNododeT1.OnElementoLanzado
        Try
            If Not mSoloLectura Then
                AgregarNodoArbol(pElemnto)
            End If
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

#End Region

#Region "Métodos"

    Private Sub ActualizarSoloLectura()
        If mSoloLectura Then
            cmdPasar1.Enabled = False
            cmdPasarTodos.Enabled = False
            cmdQuitar1.Enabled = False
            cmdQuitarTodos.Enabled = False
        Else
            cmdPasar1.Enabled = True
            cmdPasarTodos.Enabled = True
            cmdQuitar1.Enabled = True
            cmdQuitarTodos.Enabled = True
        End If
    End Sub

#End Region

#Region "Métodos Árbol a Lista"

    Private Sub AgregarNodoArbol(ByVal nodo As EntidadBaseDN)
        If nodo Is Nothing Then
            Return
        End If

        If TypeOf nodo Is INodoDN Then
            Dim listaNodos As IList
            Dim miNodo As Object
            miNodo = nodo

            listaNodos = miNodo.RecuperarColHojasConenidas()

            For Each elto As EntidadBaseDN In listaNodos
                AgregarElementoaLista(elto)
            Next
        Else
            AgregarElementoaLista(nodo)
        End If

    End Sub

    Private Sub AgregarTodosArbol()
        Dim nodo As EntidadBaseDN = ArbolNododeT1.NodoPrincipal
        AgregarNodoArbol(nodo)
    End Sub

    Private Sub AgregarElementoaLista(ByVal elemento As EntidadBaseDN)
        Dim entidad As EntidadBaseDN

        For Each entidad In Me.lbLista.Items
            If entidad.EsIgualRapido(elemento) Then
                Exit Sub
            End If
        Next

        Me.lbLista.Items.Add(elemento)
    End Sub

    Private Sub QuitarElementoLista()
        If Me.lbLista.SelectedItem IsNot Nothing Then
            Me.lbLista.Items.Remove(Me.lbLista.SelectedItem)
            Me.lbLista.Refresh()
        End If
    End Sub

    Private Sub QuitarTodosLista()
        Me.lbLista.Items.Clear()
        Me.lbLista.Refresh()
    End Sub

#End Region


End Class
