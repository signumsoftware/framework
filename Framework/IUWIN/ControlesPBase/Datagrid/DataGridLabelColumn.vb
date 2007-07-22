Imports System
Imports System.Drawing
Imports System.Diagnostics
Imports System.Windows.Forms

Namespace Datagrid

    'Datacolumnstyle que sirve para mostrar texto en un datagrid.
    'Es muy parecido al DatagridTextBoxColumn, con la diferencia de que 
    'al activar la celda el texto no se selecciona, y el usuario no 
    'puede modificar el contenido.

    'Cuando el control Datagrid invoca al método, el diseño del texto se
    'configura a través del objeto StringFormat y el texto porcesado a
    'través del método DrawString del objeto Graphics.

    Public Class DatagridLabelColumn
        Inherits Estilo.ColumnStyle

#Region "propiedades"

#End Region

#Region "constructor"
        Public Sub New()
            Me.ControlSize = New Size(150, 25)
            Me.Width = Me.GetPreferredSize(Nothing, Nothing).Width
        End Sub
#End Region

#Region "métodos"
        Protected Overloads Overrides Sub Paint(ByVal g As System.Drawing.Graphics, ByVal bounds As System.Drawing.Rectangle, ByVal [source] As CurrencyManager, ByVal rowNum As Integer, ByVal backBrush As System.Drawing.Brush, ByVal foreBrush As System.Drawing.Brush, ByVal alignToRight As Boolean)
            Dim sf As StringFormat
            Dim boundsF As RectangleF
            Dim DgP As Datagrid.DataGridP
            Dim PincelFondo As Brush
            Dim PincelFuente As Brush


            Dim dv As DataView
            Dim cm As CurrencyManager
            Dim filav As DataRowView
            Dim id As String

            Try
                'establecemos valores por defecto
                PincelFondo = backBrush.Clone
                PincelFuente = foreBrush.Clone

                'Try
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
                            'comprobamos si hay un forecolor para resaltados
                            If Not DgP.ForeColorResaltado.ToString = Color.Empty.ToString Then
                                PincelFuente = New SolidBrush(DgP.ForeColorResaltado)
                            End If
                        End If
                    End If


                End If
                'Catch ex As Exception
                '    'no pasa nada, se dibuja con los valores por defecto
                'End Try



                sf = New StringFormat
                sf.Alignment = StringAlignment.Far
                sf.LineAlignment = StringAlignment.Center
                sf.FormatFlags = StringFormatFlags.DirectionRightToLeft Or StringFormatFlags.FitBlackBox
                g.FillRectangle(PincelFondo, bounds)

                boundsF = New System.Drawing.RectangleF(CType(bounds.X, Single), CType(bounds.Y, Single), CType(bounds.Width, Single), CType(bounds.Height, Single))

                g.DrawString(Me.GetColumnValueAtRow([source], rowNum).ToString(), Me.DataGridTableStyle.DataGrid.Font, PincelFuente, boundsF, sf)

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

