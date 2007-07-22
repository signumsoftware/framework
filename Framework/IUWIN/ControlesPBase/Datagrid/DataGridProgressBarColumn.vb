Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Reflection

Namespace DataGrid

    'Muestra en una barra el progreso de un determinado valor.
    'Toma el valor de la columna, que debe estar entre 0 y 100
    Public Class DatagridProgressBarColumn
        Inherits Estilo.ColumnStyle

#Region "campos"
        'autoajustaral tamaño
        Private mStretchToFit As Boolean
        'color de la barra de progreso
        Private mColorBarra As Color
#End Region

#Region "constructor"
        Public Sub New()
            Me.EstiloColumna.EstablecerEstilo(4, 8, 4, 8)
            Me.ControlSize = New Size(80, 10) 'ponemos el tamaño por defecto
            Me.Width = Me.GetPreferredSize(Nothing, Nothing).Width
            Me.mStretchToFit = True 'ponemos el ajuste por defecto
            Me.mColorBarra = Color.Green 'ponemos el color por defecto
        End Sub
#End Region

#Region "propiedades"
        'Determina si la barra de progreso se debe expandir o contraer al tamaño
        'de la celda (true) o si debe mantener sus proporciones (false). El valor
        'predeterminado es true.
        Public Property StrechtToFit() As Boolean
            Get
                Return mStretchToFit
            End Get
            Set(ByVal Value As Boolean)
                mStretchToFit = Value
            End Set
        End Property

        'Determina el color de la barra de relleno. Por defecto es verde
        Public Property ColorBarra() As Color
            Get
                Return mColorBarra
            End Get
            Set(ByVal Value As Color)
                mColorBarra = Value
            End Set
        End Property
#End Region

#Region "métodos"
        Protected Overloads Overrides Sub Paint(ByVal g As System.Drawing.Graphics, ByVal bounds As System.Drawing.Rectangle, ByVal [source] As CurrencyManager, ByVal rowNum As Integer, ByVal backBrush As System.Drawing.Brush, ByVal foreBrush As System.Drawing.Brush, ByVal alignToRight As Boolean)
            Dim controlBounds As Rectangle
            Dim fillRect As Rectangle
            Dim maxWidth As Integer
            Dim indexWidth As Double
            Dim p As Pen
            Dim sb As SolidBrush

            Dim DgP As Datagrid.DataGridP
            Dim PincelFondo As Brush

            Dim dv As DataView
            Dim cm As CurrencyManager
            Dim filav As DataRowView
            Dim id As String

            Try
                'ponemos los valores por defecto
                PincelFondo = backBrush.Clone

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
                        End If
                    End If
                End If

                g.FillRectangle(PincelFondo, bounds)

                controlBounds = Me.GetControlBounds(bounds)

                If mStretchToFit Then
                    controlBounds.Width = bounds.Width - (Me.EstiloColumna.Izquierda + Me.EstiloColumna.Derecha)
                    controlBounds.X = bounds.X + Me.EstiloColumna.Izquierda
                End If

                fillRect = New Rectangle(controlBounds.X + 2, controlBounds.Y + 2, controlBounds.Width - 3, controlBounds.Height - 3)
                maxWidth = fillRect.Width
                indexWidth = CDbl(fillRect.Width) / 100 ' determina el ancho de cada índice
                fillRect.Width = CInt(CInt("0" & Me.GetColumnValueAtRow([source], rowNum)) * indexWidth)

                If fillRect.Width > maxWidth Then
                    fillRect.Width = maxWidth
                End If

                p = New Pen(New SolidBrush(Color.Black))

                Try
                    g.DrawRectangle(p, controlBounds)
                Finally
                    p.Dispose()
                End Try

                sb = New SolidBrush(Me.mColorBarra)
                Try
                    g.FillRectangle(sb, fillRect)
                Finally
                    sb.Dispose()
                End Try

            Catch ex As Exception
                Throw ex
            Finally
                PincelFondo.Dispose()
            End Try
        End Sub
#End Region


    End Class

End Namespace


