Imports System.Windows.Forms

Public Class DataGridViewXT

#Region "campos"
    Private mNavegable As Boolean
    Private mEliminable As Boolean
    Private mAgregable As Boolean
    Private mFiltrable As Boolean
#End Region

#Region "eventos"
    Public Event Navegar(ByVal SelectedRow As System.Windows.Forms.DataGridViewRow)
    Public Event NavegarMultiple(ByVal SelectedRows As System.Windows.Forms.DataGridViewSelectedRowCollection)
    Public Event Eliminar(ByVal SelectedRow As System.Windows.Forms.DataGridViewRow)
    Public Event EliminarMultiple(ByVal SelectedRows As System.Windows.Forms.DataGridViewSelectedRowCollection)
    Public Event Agregar()
    Public Event MostrarFiltro()
    Public Event Refrescar()

#End Region

#Region "Propiedades"
    Public Property TituloListado() As String
        Get
            Return Me.Label1.Text
        End Get
        Set(ByVal value As String)
            Me.Label1.Text = value
        End Set
    End Property


    ''' <summary>
    ''' Establece u Obtiene si el botón que solicita hacer visible el filtro debe verse
    ''' </summary>
    <System.ComponentModel.DefaultValue(True)> _
    Public Property Filtrable() As Boolean
        Get
            Return mFiltrable
        End Get
        Set(ByVal value As Boolean)
            Me.mFiltrable = value
            Me.Button1.Visible = Me.mFiltrable
        End Set
    End Property

    ''' <summary>
    ''' Expone el control DatagridView que contiene el control
    ''' </summary>
    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public ReadOnly Property DatagridView() As System.Windows.Forms.DataGridView
        Get
            Return Me.DataGridView1
        End Get
    End Property

    ''' <summary>
    ''' Establece u Obtiene si el botón Navegar es visible
    ''' </summary>
    <System.ComponentModel.DefaultValue(True)> _
    Public Property Navegable() As Boolean
        Get
            Return mNavegable
        End Get
        Set(ByVal value As Boolean)
            Me.mNavegable = value
            Me.cmdNavegar.Visible = Me.mNavegable
        End Set
    End Property

    ''' <summary>
    ''' Establece u Obtiene si el botón Eliminar es visible
    ''' </summary>
    ''' 
    <System.ComponentModel.DefaultValue(True)> _
    Public Property Eliminable() As Boolean
        Get
            Return mEliminable
        End Get
        Set(ByVal value As Boolean)
            mEliminable = value
            Me.cmdEliminar.Visible = mEliminable
        End Set
    End Property

    ''' <summary>
    ''' Establece u Obtiene si el botón Agregar es visible
    ''' </summary>
    <System.ComponentModel.DefaultValue(True)> _
    Public Property Agregable() As Boolean
        Get
            Return mAgregable
        End Get
        Set(ByVal value As Boolean)
            mAgregable = value
            Me.cmdAgregar.Visible = mAgregable
        End Set
    End Property


#End Region

#Region "métodos"
    Private Sub cmdExcel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdExcel.Click














        Try
            Me.SaveFileDialog1.AddExtension = True
            Me.SaveFileDialog1.DefaultExt = "csv"
            Me.SaveFileDialog1.Filter = "Fichero separado por comas|.csv"
            'Me.SaveFileDialog1.OverwritePrompt = True
            Me.SaveFileDialog1.RestoreDirectory = True
            Me.SaveFileDialog1.Title = "Exportar a Excel (csv)"
            'Me.SaveFileDialog1.CreatePrompt = True
            Me.SaveFileDialog1.InitialDirectory = System.IO.Path.GetFullPath(Application.ExecutablePath)

            If Me.SaveFileDialog1.ShowDialog() = DialogResult.OK Then
                Dim mifilename As String = Me.SaveFileDialog1.FileName
                Dim archivo As New System.IO.StreamWriter(mifilename, True, System.Text.Encoding.ASCII)

                Try

                    Dim tabla As DataTable = CType(Me.DataGridView1.DataSource, DataTable)

                    'escribimos las cabeceras
                    Dim linea As String = String.Empty
                    'nos saltamos la columna 0
                    For a As Integer = 0 To tabla.Columns.Count - 1
                        Dim dc As DataColumn = tabla.Columns(a)
                        If linea <> String.Empty Then
                            linea += vbTab
                        End If
                        If dc.ColumnName <> dc.Caption AndAlso dc.Caption <> String.Empty Then
                            linea += dc.Caption
                        Else
                            linea += dc.ColumnName
                        End If
                    Next

                    archivo.WriteLine(linea)

                    'escribimos los contenidos
                    For Each r As DataRow In tabla.Rows
                        linea = String.Empty
                        For a As Integer = 0 To tabla.Columns.Count - 1
                            If linea <> String.Empty Then
                                linea += vbTab
                            End If
                            linea += r(a).ToString
                        Next
                        archivo.WriteLine(linea)
                    Next

                    'forzamos la escritura y cerramos
                    archivo.Flush()
                    archivo.Close()
                Catch ex As Exception
                    Throw (ex)
                Finally
                    If Not archivo Is Nothing Then
                        archivo.Dispose()
                    End If

                End Try

                System.Diagnostics.Process.Start(mifilename)

            End If

        Catch ex As Exception
            MostrarError(ex, "Error")
        End Try
    End Sub


    Private Sub cmdNavegar_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmdNavegar.Click
        Try
            If Not HayAlgoSeleccionado() Then
                Advertencia("Debe Seleccionar un elemento de la lista", "Navegar")
                Exit Sub
            End If

            If Me.DataGridView1.MultiSelect Then
                RaiseEvent NavegarMultiple(Me.DataGridView1.SelectedRows)
            Else
                RaiseEvent Navegar(Me.DataGridView1.SelectedRows(0))
            End If
        Catch ex As Exception
            MostrarError(ex, "Navegar")
        End Try
    End Sub

    Private Sub cmdEliminar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdEliminar.Click
        Try
            If Not HayAlgoSeleccionado() Then
                Advertencia("Debe Seleccionar un elemento de la lista", "Eliminar")
                Exit Sub
            End If
            If Me.DataGridView1.MultiSelect Then
                RaiseEvent EliminarMultiple(Me.DataGridView1.SelectedRows)
            Else
                RaiseEvent Eliminar(Me.DataGridView1.SelectedRows(0))
            End If
        Catch ex As Exception
            MostrarError(ex, "Eliminar")
        End Try
    End Sub

    Private Function HayAlgoSeleccionado() As Boolean
        Return (Not Me.DataGridView1.SelectedRows Is Nothing) AndAlso (Me.DataGridView1.SelectedRows.Count <> 0)
    End Function

    Private Sub Advertencia(ByVal mensaje As String, ByVal titulo As String)
        MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
    End Sub

    Private Sub cmdAgregar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAgregar.Click
        Try
            RaiseEvent Agregar()
        Catch ex As Exception
            MostrarError(ex, "Agregar")
        End Try
    End Sub

    'Eliminado porque oculta información en el caso de que 
    'las columnas sean más grndes que el datagrid
    ''' <summary>
    ''' Establece la última columna del datagridview como Fill para que no queden huecos
    ''' al final de los campos
    ''' </summary>
    'Public Overridable Sub DataGridView1_DataSourceChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles DataGridView1.DataSourceChanged
    '    Try
    '        If Not Me.DataGridView1.Columns Is Nothing AndAlso Me.DataGridView1.Columns.Count <> 0 Then
    '            Me.DataGridView1.Columns(Me.DataGridView1.Columns.Count - 1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
    '        End If
    '    Catch ex As Exception
    '        MostrarError(ex)
    '    End Try
    'End Sub

#End Region


    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        RaiseEvent MostrarFiltro()
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        RaiseEvent Refrescar()
    End Sub

    Private Sub FlowLayoutPanel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles FlowLayoutPanel1.Paint

    End Sub


    Public ReadOnly Property PanelComandos() As System.Windows.Forms.FlowLayoutPanel
        Get
            Return FlowLayoutPanel1

        End Get
    End Property
End Class
