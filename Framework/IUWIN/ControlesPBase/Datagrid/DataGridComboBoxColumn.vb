Imports System
Imports System.Drawing
Imports System.Diagnostics
Imports System.Windows.Forms


Namespace DataGrid

    'muestra un combobox dentro de una celda del datagrid
    'Al crear la columna, se crea un objeto combobox y se etsablecen los valores de propiedad deseados
    Public Class DatagridComboBoxColumn
        Inherits Estilo.ColumnStyle

#Region "propiedades"
        Private mComboBox As ComboBox
        Private mFilaEditadaAntes As Integer
        Private mDisplayMember As String
#End Region

#Region "constructor"
        Public Sub New(ByVal miarr As ArrayList, ByVal pDisplayMember As String, ByVal SelectedItem As Object)
            mComboBox = New ComboBox
            mComboBox.DropDownStyle = ComboBoxStyle.DropDownList
            mComboBox.Visible = False
            'agregamos un delegado al evento sizechanged
            AddHandler mComboBox.SizeChanged, AddressOf ComboBox_SizeChanged

            'ponemos el tamaño
            Me.ControlSize = mComboBox.Size
            Me.EstiloColumna.EstablecerEstilo(4, 8, 4, 8)
            Me.Width = Me.GetPreferredSize(Nothing, Nothing).Width

            'ponemos el displaymember
            DisplayMember = pDisplayMember

            'rellenamos el combo
            If Not miarr Is Nothing Then
                mComboBox.Items.AddRange(miarr.ToArray)
            End If

            'establecemos el elemento determinado por defecto
            mComboBox.SelectedItem = SelectedItem

        End Sub
#End Region

#Region "propiedades"
        'la propiedad que se va a mostrar en el text del combobox
        Public Property DisplayMember() As String
            Get
                Return mComboBox.DisplayMember
            End Get
            Set(ByVal Value As String)
                mComboBox.DisplayMember = Value
            End Set
        End Property

        'el combobox que se encuentra dentro de la celda
        Public ReadOnly Property ComboBox() As ComboBox
            Get
                Return mComboBox
            End Get
        End Property

        'sobrescribimos la propiedad controlsize para leer y establecer
        'el tamaó del combobox
        Public Overrides Property ControlSize() As Size
            Get
                Return mComboBox.Size
            End Get
            Set(ByVal Value As Size)
                mComboBox.Size = Value
            End Set
        End Property
#End Region

#Region "métodos"

        'cancelan la edición
        Protected Overrides Sub Abort(ByVal rowNum As Integer)
            mComboBox.Visible = False
        End Sub

        Protected Overloads Overrides Sub Edit(ByVal [source] As CurrencyManager, ByVal rowNum As Integer, ByVal bounds As Rectangle, ByVal [readOnly] As Boolean, ByVal instantText As String, ByVal cellIsVisible As Boolean)
            Dim p As Point
            Dim controlBounds As Rectangle
            Dim cursorBounds As Rectangle

            Try
                'obtenemos las coordenadas del cursor
                p = Me.DataGridTableStyle.DataGrid.PointToClient(Cursor.Position)

                'obtenemos el bound del control
                controlBounds = Me.GetControlBounds(bounds)

                'obtenemos el bound del cursor
                cursorBounds = (New Rectangle(p.X, p.Y, 1, 1))


                mComboBox.SelectedIndex = CInt("0" & Me.GetColumnValueAtRow([source], rowNum))

                mComboBox.Location = New Point(controlBounds.X, controlBounds.Y)
                mComboBox.Visible = True

                If cursorBounds.IntersectsWith(controlBounds) Then
                    mComboBox.DroppedDown = True
                End If

                mFilaEditadaAntes = rowNum
            Catch ex As Exception
                Throw ex
            End Try

        End Sub

        'Terminar la Edición
        Protected Overrides Function Commit(ByVal dataSource As CurrencyManager, ByVal rowNum As Integer) As Boolean

            If mFilaEditadaAntes = rowNum Then
                Me.SetColumnValueAtRow(dataSource, rowNum, mComboBox.SelectedIndex)
            End If

            mComboBox.Visible = False

            Return True
        End Function

        Protected Overrides Sub SetDataGridInColumn(ByVal value As System.Windows.Forms.DataGrid)

            MyBase.SetDataGridInColumn(value)

            If Not value.Controls.Contains(mComboBox) Then
                value.Controls.Add(mComboBox)
            End If
        End Sub

        'el método paint
        'el control del cuadro combinado de esta columna se traza con la ayuda de la clase ControlPaint.
        'Se trata deu na clase sellada con una gran variedad de métodos estáticos, que, al ser llamados,
        'representan algunos de los controles básicos de windows y sus elementos correspondientes.
        'Uno de los métodos de esta clase es DrawComboButton, que traza un 'glyph" de flecha abajo,
        'similar al q aparece en el extremo derecho de un combobox. Su uso junto con el método DrawBorder3D 
        'proporciona una réplica del objeto combobox, que en realidad sólo se muestra en el Edit.
        Protected Overloads Overrides Sub Paint(ByVal g As System.Drawing.Graphics, ByVal bounds As System.Drawing.Rectangle, ByVal [source] As CurrencyManager, ByVal rowNum As Integer, ByVal backBrush As System.Drawing.Brush, ByVal foreBrush As System.Drawing.Brush, ByVal alignToRight As Boolean)
            Dim sf As New StringFormat
            Dim controlBounds As Rectangle
            Dim colValue As Integer
            Dim selectedItem As String
            Dim textRegionF As RectangleF
            Dim buttonBounds As Rectangle

            Try
                g.FillRectangle(New SolidBrush(Color.White), bounds)

                sf = New StringFormat
                sf.Alignment = StringAlignment.Near
                sf.LineAlignment = StringAlignment.Center

                controlBounds = Me.GetControlBounds(bounds)

                colValue = CInt("0" & Me.GetColumnValueAtRow([source], rowNum))

                selectedItem = mComboBox.Items(colValue).ToString()

                textRegionF = New RectangleF(controlBounds.X + 1, controlBounds.Y + 4, controlBounds.Width - 3, CInt(g.MeasureString(selectedItem, mComboBox.Font).Height))

                g.DrawString(selectedItem, mComboBox.Font, foreBrush, textRegionF)

                ControlPaint.DrawBorder3D(g, controlBounds, Border3DStyle.Sunken)

                buttonBounds = controlBounds
                buttonBounds.Inflate(-2, -2)

                ControlPaint.DrawComboButton(g, buttonBounds.X + (controlBounds.Width - 20), buttonBounds.Y, 16, 17, ButtonState.Normal)

            Catch ex As Exception
                Throw ex
            End Try
        End Sub


        'controla el cambio de tamaño y ajusta el combobox
        Public Sub ComboBox_SizeChanged(ByVal sender As Object, ByVal e As EventArgs)
            Me.ControlSize = mComboBox.Size
            Me.Width = Me.GetPreferredSize(Nothing, Nothing).Width
            Me.Invalidate()
        End Sub

#End Region





    End Class

End Namespace
