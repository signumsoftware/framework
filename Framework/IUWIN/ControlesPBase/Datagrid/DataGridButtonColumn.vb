Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Reflection

Namespace Datagrid

    'es una columna de botones. Además de suscribirse a los eventos
    'MouseDown y MouseOver, lanza un evento cuando se ha hecho click en uno 
    'de los botones de lacuadrícula
    Public Class DatagridButtonColumn
        Inherits Estilo.ColumnStyle


#Region "campos"
        Private mPresionadoBounds As Rectangle
        Private mFocoRectangle As Rectangle

        Delegate Sub ButtonColumnClickHandler(ByVal e As ButtonColumnEventArgs)
        Public Event Click As ButtonColumnClickHandler
#End Region

#Region "constructor"
        Public Sub New()
            Me.ControlSize = New Size(80, 24)
            Me.EstiloColumna.EstablecerEstilo(4, 8, 4, 8)
            Me.Width = Me.GetPreferredSize(Nothing, Nothing).Width
        End Sub
#End Region

#Region "métodos"


        Protected Overrides Sub SetDataGridInColumn(ByVal value As System.Windows.Forms.DataGrid)
            MyBase.SetDataGridInColumn(value)

            'se suscribe a los eventos del datagrid
            AddHandler Me.DataGridTableStyle.DataGrid.MouseDown, AddressOf DataGrid_MouseDown
            AddHandler Me.DataGridTableStyle.DataGrid.MouseUp, AddressOf DataGrid_MouseUp

        End Sub

        Private Sub DataGrid_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)

            Dim hti As System.Windows.Forms.DataGrid.HitTestInfo = Me.DataGridTableStyle.DataGrid.HitTest(e.X, e.Y)

            If hti.Column = -1 Then
                Exit Sub
            End If

            ' nos aseguramos de que el estado del botón es alguno de los siguientes:
            '		1. se ha presionado el botón izqdo del ratón,
            '		2. el cursos está sobre una celda
            '		3. la celda pertenece a éste estilo
            If e.Button = MouseButtons.Left And hti.Type = System.Windows.Forms.DataGrid.HitTestType.Cell And TypeOf Me.DataGridTableStyle.GridColumnStyles(hti.Column) Is DatagridButtonColumn Then

                'en vez de implementar todo el método que simula que se presiona
                'un botón en el ára del cursor, se crea un rectángulo de 1x1 para
                'representar la posición del cursor. Después, como cellBounds representa
                'las dimensiones de la celda entera, lo necesitamos para calcular las dimensiones
                'del botón. Por último, con IntersectsWith somos capaces de determinar donde se 
                'cruzan los dos rectángulos.

                Dim cursorRect As New Rectangle(e.X, e.Y, 1, 1)
                Dim cellBounds As Rectangle = Me.DataGridTableStyle.DataGrid.GetCellBounds(hti.Row, hti.Column)

                Dim buttonBounds As Rectangle = Me.GetControlBounds(cellBounds)

                If cursorRect.IntersectsWith(buttonBounds) Then

                    mPresionadoBounds = cellBounds

                    'como el método Invalidate del DatagridColumnStyle invalidará
                    'la región entera de la columna, en su lugar usamos la referencia 
                    'al datagrid a través de nuestro DataGridTableStyle para invalidar
                    'sólo una región específica dle mismo
                    Me.DataGridTableStyle.DataGrid.Invalidate(cellBounds)
                End If
            End If
        End Sub

        Private Sub DataGrid_MouseUp(ByVal sender As Object, ByVal e As MouseEventArgs)

            Dim hti As System.Windows.Forms.DataGrid.HitTestInfo = Me.DataGridTableStyle.DataGrid.HitTest(e.X, e.Y)

            If Not mPresionadoBounds.Equals(Rectangle.Empty) Then

                Dim cellBounds As Rectangle = Me.DataGridTableStyle.DataGrid.GetCellBounds(hti.Row, hti.Column)

                If mPresionadoBounds.Equals(cellBounds) Then

                    'el cursor se encuentra en la misma celda en la que se ha hecho click.
                    'Ahora comprobamos si se ha hecho click dentro del botón
                    Dim cursorRect As New Rectangle(e.X, e.Y, 1, 1)

                    Dim buttonBounds As Rectangle = Me.GetControlBounds(cellBounds)

                    If cursorRect.IntersectsWith(buttonBounds) Then

                        Dim ds As Object = Me.DataGridTableStyle.DataGrid.DataSource
                        Dim dataMember As String = Me.DataGridTableStyle.DataGrid.DataMember
                        Dim cm As CurrencyManager = CType(Me.DataGridTableStyle.DataGrid.BindingContext(ds, dataMember), CurrencyManager)

                        Dim buttonValue As String = CStr("" & Me.GetColumnValueAtRow(cm, hti.Row))

                        If buttonValue.ToLower().Equals("start") Then
                            buttonValue = "Stop"
                        Else
                            If buttonValue.ToLower().Equals("stop") Then
                                buttonValue = "Start"
                            End If
                        End If

                        Me.SetColumnValueAtRow(cm, hti.Row, buttonValue)

                        RaiseEvent Click(New ButtonColumnEventArgs(hti.Row, hti.Column, buttonValue))

                    End If
                End If


                mPresionadoBounds = Rectangle.Empty
                Me.DataGridTableStyle.DataGrid.Invalidate(cellBounds)
            End If
        End Sub


        'el método paint
        Protected Overloads Overrides Sub Paint(ByVal g As System.Drawing.Graphics, ByVal bounds As System.Drawing.Rectangle, ByVal [source] As CurrencyManager, ByVal rowNum As Integer, ByVal backBrush As System.Drawing.Brush, ByVal foreBrush As System.Drawing.Brush, ByVal alignToRight As Boolean)
            Dim controlBounds As Rectangle
            Dim drawFocusRectangle As Boolean
            Dim focusBounds As Rectangle
            Dim fontBoundsF As System.Drawing.RectangleF
            Dim bs As ButtonState
            Dim sf As New StringFormat

            Dim DgP As Datagrid.DataGridP

            Dim PincelFondo As Brush = Nothing
            Dim PincelFuente As Brush = Nothing

            Dim dv As DataView
            Dim cm As CurrencyManager
            Dim filav As DataRowView
            Dim id As String

            Try
                'establecemos valores por defecto
                PincelFondo = backBrush.Clone
                PincelFuente = foreBrush.Clone


                'determinamos si estamos en un DatagridP
                If TypeOf Me.DataGridTableStyle.DataGrid Is Datagrid.DataGridP Then
                    DgP = Me.DataGridTableStyle.DataGrid

                    'comprobamos si tenemos q estar resaltados o no

                    If DgP.Resaltar Then

                        'obtenemos el dataview q hay sobre el datatable para poder acceder a las filas
                        'reordenadas
                        cm = DgP.BindingContext(DgP.DataSource, DgP.DataMember)
                        dv = cm.List

                        filav = dv(rowNum)

                        id = CStr("" & filav.Item(Me.ColumnaID))

                        'If DgP.IDsResaltados.Contains(DgP.Item(rowNum, Me.ColumnaID)) Then
                        If DgP.IDsResaltados.Contains(id) Then
                            'comprobamos si hay un color para resaltados
                            If Not DgP.BackColorResaltado.ToString = Color.Empty.ToString Then
                                PincelFondo = New SolidBrush(DgP.BackColorResaltado)
                            End If
                            'comprobamos si hay un forecolor
                            If Not DgP.ForeColorResaltado.ToString = Color.Empty.ToString Then
                                PincelFuente = New SolidBrush(DgP.ForeColorResaltado)
                            End If
                        End If

                    End If
                End If



                g.FillRectangle(PincelFondo, bounds)

                controlBounds = Me.GetControlBounds(bounds)
                drawFocusRectangle = True

                focusBounds = controlBounds
                focusBounds.Inflate(-4, -4)

                fontBoundsF = New System.Drawing.RectangleF(CType(bounds.X, Single), CType(bounds.Y, Single), CType(bounds.Width, Single), CType(bounds.Height, Single))

                fontBoundsF.Inflate(-3, -3)

                bs = ButtonState.Inactive

                'If Not m_depressedBounds.Equals(Rectangle.Empty) And m_depressedBounds.Equals(bounds) Then
                '    bs = ButtonState.Pushed
                'Else
                '    drawFocusRectangle = False
                'End If

                ControlPaint.DrawButton(g, controlBounds, bs)

                sf = New StringFormat
                sf.Alignment = StringAlignment.Center
                sf.LineAlignment = StringAlignment.Center
                sf.FormatFlags = StringFormatFlags.DirectionRightToLeft Or StringFormatFlags.FitBlackBox

                g.DrawString(Me.GetColumnValueAtRow([source], rowNum).ToString(), Me.DataGridTableStyle.DataGrid.Font, PincelFuente, fontBoundsF, sf)

                'If drawFocusRectangle Then
                '    ControlPaint.DrawFocusRectangle(g, m_focusRectangle, Color.Red, Control.DefaultBackColor)
                'End If

            Catch ex As Exception
                Throw ex
            Finally
                PincelFondo.Dispose()
                PincelFuente.Dispose()
            End Try

        End Sub

#End Region


    End Class

End Namespace
