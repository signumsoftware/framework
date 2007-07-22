Imports System.Windows.Forms
Imports System.Drawing

Imports Framework.DatosNegocio

Public Class ctrlListadoTipos

#Region "atributos"

    Private mColTipos As IList
    Private mDataTable As DataTable

    Private mTipoSeleccionado As IEntidadDN
    Private mColSeleccionado As IList
    Private mMultiSelect As Boolean

    Private mTipo As Type

#End Region

#Region "Inicializar"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        Me.mColSeleccionado = New ArrayListValidable(Of TipoConOrdenDN)
    End Sub

#End Region

#Region "Propiedades"

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property ColTipos() As IList
        Get
            If IUaDN() Then
                Return Me.mColTipos
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As IList)
            Me.mColTipos = value
            DNaIU(value)
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property MultiSelect() As Boolean
        Get
            Return mMultiSelect
        End Get
        Set(ByVal value As Boolean)
            mMultiSelect = value
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property ColSeleccionado() As IList
        Get
            Return mColSeleccionado
        End Get
        Set(ByVal value As IList)
            If value Is Nothing Then
                value = New ArrayListValidable(Of IEntidadDN)
            End If
            mColSeleccionado = value
            EstablecerColSeleccionados()
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Property Tipo() As Type
        Get
            Return Me.mTipo
        End Get
        Set(ByVal value As Type)
            Me.mTipo = value
        End Set
    End Property

#End Region

#Region "Establecer y rellenar datos"

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Me.mColSeleccionado.Clear()
        MostrarDatos()
    End Sub

    Protected Overrides Function IUaDN() As Boolean
        'no hay nada que comprobar
        Return True
    End Function
#End Region

#Region "eventos"
    Public Event TipoSeleccionado(ByVal pTipo As IEntidadDN, ByVal sender As Object)
#End Region

#Region "Controladores de eventos"

    Private Sub dgpTipos_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles dgpTipos.MouseUp
        Dim pt = New Point(e.X, e.Y)
        Dim hti As DataGrid.HitTestInfo = Me.dgpTipos.HitTest(pt)

        Dim dv As DataView
        Dim cm As CurrencyManager
        Dim filav As DataRowView

        Dim Id As String

        Try
            If Me.dgpTipos.DataSource Is Nothing Then
                'Comprobamos que tengamos un objeto seleccionado 
                NoHayNadaSeleccionado()
                Exit Sub
            End If

            Me.dgpTipos.BackColorResaltado = Color.Yellow

            If (hti.Type = DataGrid.HitTestType.Cell) Then
                Me.dgpTipos.CurrentCell = New DataGridCell(hti.Row, hti.Column)
            End If

            cm = dgpTipos.BindingContext(dgpTipos.DataSource, dgpTipos.DataMember)
            dv = cm.List

            If hti.Row = -1 Then
                'Comprobamos que tengamos un objeto seleccionado 
                NoHayNadaSeleccionado()
                Exit Sub
            End If

            filav = dv(hti.Row)

            Id = filav("ID")

            If Not Me.mMultiSelect Then
                Me.dgpTipos.IDsResaltados.Clear()
                Me.dgpTipos.IDsResaltados.Add(Id)
                Me.mColSeleccionado.Clear()
                Me.mColSeleccionado.Add(filav("Object"))
            Else
                If Me.dgpTipos.IDsResaltados.Contains(Id) Then
                    Me.dgpTipos.IDsResaltados.Remove(Id)
                    Me.mColSeleccionado.Remove(filav("Object"))
                Else
                    Me.dgpTipos.IDsResaltados.Add(Id)
                    Me.mColSeleccionado.Add(filav("Object"))
                End If
            End If

            Me.dgpTipos.Refresh()

            'lanzamos el evento con la observación seleccionada
            RaiseEvent TipoSeleccionado(filav("Object"), Me)

        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub
#End Region

#Region "Métodos"

    Public Overrides Sub Refresh()
        Try
            MyBase.Refresh()
            Me.MostrarDatos()
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub MostrarDatos()
        Dim miTipo As IEntidadDN
        Dim miTableStyle As DataGridTableStyle
        Dim ColNombre As ControlesPBase.Datagrid.DatagridLabelColumn
        Dim ColOrden As ControlesPBase.Datagrid.DatagridLabelColumn
        Dim miRow As DataRow
        Dim miObj As Object

        Try
            'lo 1º, limpiamos el datagrid
            Me.dgpTipos.DataSource = Nothing

            'vemos si está vacío
            If Not Me.mColTipos Is Nothing Then
                miTableStyle = New DataGridTableStyle

                Me.mDataTable = New DataTable

                'Se añade primero la columna del ID----
                Me.mDataTable.Columns.Add(New DataColumn("ID", GetType(String)))
                '----
                Me.mDataTable.Columns.Add(New DataColumn("Nombre", GetType(String)))
                Me.mDataTable.Columns.Add(New DataColumn("Orden", GetType(String)))
                Me.mDataTable.Columns.Add(New DataColumn("Object", GetType(IEntidadDN)))

                'rellenamos los gridcolumnstyles

                ColNombre = New ControlesPBase.Datagrid.DatagridLabelColumn
                ColNombre.HeaderText = "Nombre"
                ColNombre.MappingName = "Nombre"
                ColNombre.Width = 100

                ColOrden = New ControlesPBase.Datagrid.DatagridLabelColumn
                ColOrden.HeaderText = "Orden"
                ColOrden.MappingName = "Orden"
                ColOrden.Width = 60
                ColOrden.ControlHorizAlignment = ControlesPBase.Datagrid.Estilo.ControlHorizAlignment.Derecha
                ColOrden.ControlVertAlignment = ControlesPBase.Datagrid.Estilo.ControlVertAlignment.Centrado
                'colorden.o

                'agregamos los gridcolumnstyles al tablestyle
                'en el orden correspondiente
                miTableStyle.GridColumnStyles.Add(ColNombre)
                miTableStyle.GridColumnStyles.Add(ColOrden)

                miTableStyle.RowHeadersVisible = False

                miObj = System.Activator.CreateInstance(Me.mTipo)

                For Each miTipo In Me.mColTipos
                    miRow = Me.mDataTable.NewRow

                    If miTipo.ID <> "" Then
                        miRow("ID") = miTipo.ID
                    Else
                        miRow("ID") = miObj.GetHashCode()
                    End If

                    miObj = miTipo
                    miRow("Nombre") = miObj.Nombre
                    miRow("Orden") = miObj.Orden

                    miRow("Object") = miTipo

                    'agregamos el row al datatable
                    Me.mDataTable.Rows.Add(miRow)

                Next

                miTableStyle.AlternatingBackColor = Color.LightBlue
                miTableStyle.SelectionBackColor = Color.Blue
                miTableStyle.SelectionForeColor = Color.White

                'asignamos el tablestyle
                Me.dgpTipos.TableStyles.Clear()
                Me.dgpTipos.TableStyles.Add(miTableStyle)


                'asignamos el datasource
                Me.dgpTipos.DataSource = Me.mDataTable
                Me.dgpTipos.Refresh()

            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub EstablecerColSeleccionados()
        Dim miTipo As IEntidadDN
        Dim miObj As Object

        Try
            Me.dgpTipos.IDsResaltados.Clear()

            miObj = System.Activator.CreateInstance(Me.mTipo)

            For Each miTipo In Me.mColSeleccionado
                If miTipo.ID <> "" Then
                    Me.dgpTipos.IDsResaltados.Add(miTipo.ID)
                Else
                    miObj = miTipo
                    Me.dgpTipos.IDsResaltados.Add(miObj.GetHashCode())
                End If
            Next
            Me.dgpTipos.Refresh()
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub NoHayNadaSeleccionado()
        Try
            'lanzamos el evento, pero diciendo que no ha seleccionado nada
            Me.mColSeleccionado = New ArrayList()
            Me.EstablecerColSeleccionados()
            RaiseEvent TipoSeleccionado(Nothing, Me)
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

#End Region

End Class
