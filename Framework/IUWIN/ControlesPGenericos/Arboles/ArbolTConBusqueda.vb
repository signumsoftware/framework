Imports Framework.DatosNegocio.Arboles
Imports Framework.DatosNegocio

Public Class ArbolTConBusqueda

#Region "eventos"
    Public Event OnElementoSeleccionado(ByRef pElemento As Object)
    Public Event OnElementoLanzado(ByRef pElemnto As Object)
    Public Event BeforeSelect(ByRef ElementoSeleccionado As Object, ByVal e As System.Windows.Forms.TreeViewCancelEventArgs)
#End Region

#Region "atributos"
    Private mNodoPrincipal As INodoDN
    Private mContenido As Boolean = True
    Private mComiencePor As Boolean = False

    Private WithEvents mControlLista As ListaResizeable

    Private WithEvents ListaDeResultados As System.Windows.Forms.ListBox

    Private mCliqueandoResultado As Boolean
#End Region

#Region "inicializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlLista = New ListaResizeable
        Me.mControlLista.Width = Me.txtvBusqueda.Width
        Me.mControlLista.Location = New System.Drawing.Point(Me.txtvBusqueda.Location.X, (Me.txtvBusqueda.Location.Y + Me.txtvBusqueda.Height))
        Me.mControlLista.Visible = False
        Me.mControlLista.Anchor = Windows.Forms.AnchorStyles.Top + Windows.Forms.AnchorStyles.Right
        Me.Controls.Add(Me.mControlLista)

        Me.ListaDeResultados = Me.mControlLista.ListBox1

        AddHandler ListaDeResultados.Click, AddressOf ResultadoClick
        AddHandler ListaDeResultados.VisibleChanged, AddressOf visibleResultadochanged
        AddHandler ListaDeResultados.KeyUp, AddressOf ResultadoKeyUp

        Me.optComiencenPor.Checked = Me.mComiencePor
        Me.optContengan.Checked = Me.mContenido

    End Sub
#End Region

#Region "propiedades"

    <System.ComponentModel.DefaultValue(False)> _
    Public Property BuscarComenzando() As Boolean
        Get
            Return Me.mComiencePor
        End Get
        Set(ByVal value As Boolean)
            Me.mComiencePor = value
            Me.optComiencenPor.Checked = Me.mComiencePor
        End Set
    End Property

    <System.ComponentModel.DefaultValue(True)> _
    Public Property BuscarContenidos() As Boolean
        Get
            Return Me.mContenido
        End Get
        Set(ByVal value As Boolean)
            Me.mContenido = value
            Me.optContengan.Checked = Me.mContenido
        End Set
    End Property

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property GestorPresentacion() As IGestorPresentacionArboles
        Get
            Return Me.ArbolNododeT.GestorPresentacion
        End Get
        Set(ByVal value As IGestorPresentacionArboles)
            Me.ArbolNododeT.GestorPresentacion = value
        End Set
    End Property

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property NodoPrincipal() As INodoDN
        Get
            Return Me.ArbolNododeT.NodoPrincipal
        End Get
        Set(ByVal value As INodoDN)
            Me.mNodoPrincipal = value
            Me.ArbolNododeT.NodoPrincipal = Me.mNodoPrincipal
        End Set
    End Property

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property ElementoSeleccionado() As IEntidadBaseDN
        Get
            Return Me.ArbolNododeT.ElementoSeleccionado
        End Get
        Set(ByVal value As IEntidadBaseDN)
            Me.ArbolNododeT.ElementoSeleccionado = value
        End Set
    End Property

#End Region

#Region "métodos"

#Region "transparentes del Arbol"
    Private Sub ArbolTConBusqueda_BeforeSelect(ByRef ElementoSeleccionado As Object, ByVal e As System.Windows.Forms.TreeViewCancelEventArgs) Handles ArbolNododeT.BeforeSelect
        RaiseEvent BeforeSelect(ElementoSeleccionado, e)
    End Sub

    Private Sub ArbolTConBusqueda_OnElementoLanzado(ByRef pElemnto As Object) Handles ArbolNododeT.OnElementoLanzado
        RaiseEvent OnElementoLanzado(pElemnto)
    End Sub

    Private Sub ArbolTConBusqueda_OnElementoSeleccionado(ByRef pElemento As Object) Handles ArbolNododeT.OnElementoSeleccionado
        RaiseEvent OnElementoSeleccionado(pElemento)
    End Sub

    Public Sub ExpandirArbol()
        Me.ArbolNododeT.ExpandirArbol()
    End Sub

    Public Sub ColapsarArbol()
        Me.ArbolNododeT.ColapsarArbol()
    End Sub

#End Region

#End Region

    Private Sub txtvBusqueda_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles txtvBusqueda.KeyUp
        Try
            If Me.mControlLista.Visible Then
                If e.KeyCode = Windows.Forms.Keys.Down Then
                    Me.ListaDeResultados.SelectedItem = Me.ListaDeResultados.Items(0)
                    Me.ListaDeResultados.Focus()
                End If
            End If

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub txtvBusqueda_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtvBusqueda.TextChanged
        Try
            Dim texto As String = Me.txtvBusqueda.Text '.Trim
            Me.mControlLista.ListBox1.Items.Clear()

            If texto <> String.Empty Then
                Dim milista As New List(Of IEntidadBaseDN)
                BuscarNodo(texto, Me.mNodoPrincipal, milista)
                If milista.Count <> 0 Then
                    Me.mControlLista.ListBox1.Items.AddRange(milista.ToArray())
                    Me.mControlLista.BringToFront()
                    Me.mControlLista.Visible = True
                Else
                    Me.mControlLista.Visible = False
                End If
            Else
                Me.mControlLista.Visible = False
            End If
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub BuscarNodo(ByVal nombre As String, ByVal nodo As INodoDN, ByRef lista As List(Of IEntidadBaseDN))
        If Not nodo Is Nothing Then
            'metemos las hojas
            For Each hoja As IEntidadBaseDN In nodo.ColHojas
                Dim yaestoy As Boolean = False
                If mComiencePor Then
                    If hoja.Nombre.StartsWith(nombre, StringComparison.CurrentCultureIgnoreCase) Then
                        lista.Add(hoja)
                        yaestoy = True
                    End If
                End If
                If mContenido AndAlso (Not yaestoy) Then
                    If hoja.Nombre.ToLower.Contains(nombre.ToLower) Then
                        lista.Add(hoja)
                    End If
                End If
            Next
            'llamamos recursivamente a los hijos
            For Each mihijo As INodoDN In nodo.Hijos
                BuscarNodo(nombre, mihijo, lista)
            Next
        End If
    End Sub

    Private Sub ResultadoKeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs)
        If e.KeyCode = Windows.Forms.Keys.Enter Then
            ResultadoSeleccionado()
        ElseIf e.KeyCode = Windows.Forms.Keys.Escape Then
            Me.txtvBusqueda.Focus()
            Me.mControlLista.Visible = False
        End If
    End Sub

    Private Sub ResultadoSeleccionado()
        Me.Timer1.Enabled = False
        Me.ArbolNododeT.ElementoSeleccionado = Me.mControlLista.ListBox1.SelectedItem
        Me.txtvBusqueda.Text = Me.mControlLista.ListBox1.Text
        Me.mControlLista.Visible = False
        Me.ArbolNododeT.Focus()
    End Sub

    Private Sub ResultadoClick(ByVal sender As Object, ByVal e As EventArgs)
        ResultadoSeleccionado()
    End Sub

    Private Sub visibleResultadochanged(ByVal sender As Object, ByVal e As EventArgs)
        Me.Timer1.Enabled = Me.mControlLista.Visible
    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        If (Not Me.txtvBusqueda.Focused) AndAlso (Not Me.mControlLista.Focused) AndAlso (Not Me.ListaDeResultados.Focused) Then
            Me.mControlLista.Visible = False
        End If
    End Sub

#Region "Panel Configuración"
    Private Sub chkComiencenPor_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles optComiencenPor.CheckedChanged
        Try
            Me.mComiencePor = Me.optComiencenPor.Checked
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub chkContengan_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles optContengan.CheckedChanged
        Try
            Me.mContenido = Me.optContengan.Checked
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub PictureBox1_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles PictureBox1.Click
        Try
            MostrarOcultarCongif()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub MostrarOcultarCongif()
        If Not Me.pnlOpciones.Visible Then
            'hay que mostrar las opciones
            Me.ArbolNododeT.Enabled = False
            Me.txtvBusqueda.Enabled = False
            Me.mControlLista.Visible = False

            Me.pnlOpciones.Visible = True
            Me.pnlOpciones.BringToFront()
            Me.PictureBox1.BringToFront()

            Me.ToolTip.SetToolTip(Me.PictureBox1, "Aceptar")
        Else
            'hay que ocultar las opciones
            Me.ArbolNododeT.Enabled = True
            Me.txtvBusqueda.Enabled = True

            Me.pnlOpciones.Visible = False

            Me.ToolTip.SetToolTip(Me.PictureBox1, "Opciones de búsqueda")
        End If
    End Sub
#End Region

End Class
