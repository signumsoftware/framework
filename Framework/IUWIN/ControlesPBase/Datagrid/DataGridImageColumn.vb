Imports system.ComponentModel
Imports system.Drawing
Imports System.Windows.Forms

Namespace DataGrid

    'Datacolumnstyle que sirve para mostrar imágenes en un datagrid
    Public Class DatagridImageColumn
        Inherits Estilo.ColumnStyle


#Region "atributos"
        Private mMinimoHeight As Integer
        Private mPreferredheight As Integer
        Private mPreferredSize As Size
        Private mColorFondo As Color
#End Region

#Region "constructor"
        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(ByVal pCol As PropertyDescriptor)
            'MyBase.New(pCol)
            MyBase.New()
        End Sub

        Public Sub New(ByVal pCol As PropertyDescriptor, ByVal MinimoHeight As Integer, ByVal PreferredHeight As Integer, ByVal PreferredSize As Size, ByVal ColorFondo As Color)
            'MyBase.New(pCol)
            MyBase.New()
            Me.mMinimoHeight = MinimoHeight
            Me.mPreferredheight = PreferredHeight
            Me.mPreferredSize = PreferredSize
            Me.mColorFondo = ColorFondo
        End Sub

#End Region


#Region "métodos"

        '--------
        'estos métodos deben ser sobrescritos al heredar de datagridcolumnstyle
        '--------

        'No hay nada q abortar
        Protected Overrides Sub Abort(ByVal rowNum As Integer)

        End Sub

        'devolvemos siempre true, no hay nada q confirmar
        Protected Overrides Function Commit(ByVal dataSource As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer) As Boolean
            Return True
        End Function

        'no hay nada q editar
        Protected Overloads Overrides Sub Edit(ByVal source As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer, ByVal bounds As System.Drawing.Rectangle, ByVal [readOnly] As Boolean, ByVal instantText As String, ByVal cellIsVisible As Boolean)

        End Sub

        'devolver aquí el mínmo height que queramos
        Protected Overrides Function GetMinimumHeight() As Integer
            Return mMinimoHeight
        End Function

        'devolver el preferredheight de la imagen
        Protected Overrides Function GetPreferredHeight(ByVal g As System.Drawing.Graphics, ByVal value As Object) As Integer
            Return mPreferredheight
        End Function

        'devuelve le PreferredSize de la imagen
        Protected Overrides Function GetPreferredSize(ByVal g As System.Drawing.Graphics, ByVal value As Object) As System.Drawing.Size
            Return mPreferredSize
        End Function

        'la función PAINT es la que hace todo el trabajo de dibujar la imagen
        'hay 3 sobrecargas de la misma
#Region "Paint"
        Protected Overloads Overrides Sub Paint(ByVal g As System.Drawing.Graphics, ByVal bounds As System.Drawing.Rectangle, ByVal source As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer)
            Dim PincelFondo As SolidBrush
            Dim Imagen As Bitmap
            Dim miObjeto As Object
            Dim mipos As Point

            Dim DgP As Datagrid.DataGridP

            Dim dv As DataView
            Dim cm As CurrencyManager
            Dim filav As DataRowView
            Dim id As String

            Try
                'establecemos el pincel de fondo
                PincelFondo = New SolidBrush(mColorFondo)

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


                'obtenemos la imagen que corresponde pintar
                miObjeto = getcolumnvalueatrow(source, rowNum)

                If TypeOf miObjeto Is Bitmap Then
                    Imagen = miObjeto
                End If

                'pintamos el rectángulo
                g.FillRectangle(PincelFondo, bounds.X, bounds.Y, bounds.Width, bounds.Y)

                'pintamos la imagen

                If Not Imagen Is Nothing Then
                    mipos = EstablecerPosicion(Imagen, bounds)

                    g.DrawImageUnscaled(Imagen, mipos)
                    'g.DrawImageUnscaled(Imagen, bounds.X + (bounds.Width - Imagen.Width), bounds.Y, Imagen.Width, Imagen.Height)
                Else
                    'MyBase.Paint(g, bounds, source, rowNum, PincelFondo, New SolidBrush(CType(Me.Container, DataGridTableStyle).ForeColor), Me.Alignment)
                End If
            Catch ex As Exception
                Throw ex
            Finally
                PincelFondo.Dispose()
            End Try
        End Sub

        Protected Overloads Overrides Sub Paint(ByVal g As System.Drawing.Graphics, ByVal bounds As System.Drawing.Rectangle, ByVal source As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer, ByVal alignToRight As Boolean)
            Dim PincelFondo As SolidBrush
            Dim Imagen As Bitmap
            Dim miObjeto As Object
            Dim miPos As Point
            Dim DgP As Datagrid.DataGridP

            Dim dv As DataView
            Dim cm As CurrencyManager
            Dim filav As DataRowView
            Dim id As String

            Try
                'establecemos el pincel de fondo
                PincelFondo = New SolidBrush(mColorFondo)

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


                'obtenemos la imagen que corresponde pintar
                miObjeto = getcolumnvalueatrow(source, rowNum)

                If TypeOf miObjeto Is Bitmap Then
                    Imagen = miObjeto
                End If

                'pintamos el rectángulo
                g.FillRectangle(PincelFondo, bounds.X, bounds.Y, bounds.Width, bounds.Y)

                'pintamos la imagen
                If Not Imagen Is Nothing Then
                    miPos = EstablecerPosicion(Imagen, bounds)
                    g.DrawImageUnscaled(Imagen, miPos)
                    'g.DrawImageUnscaled(Imagen, bounds.X + (bounds.Width - Imagen.Width), bounds.Y, Imagen.Width, Imagen.Height)
                Else
                    'MyBase.Paint(g, bounds, source, rowNum, PincelFondo, New SolidBrush(CType(Me.Container, DataGridTableStyle).ForeColor), alignToRight)
                End If
            Catch ex As Exception
                Throw ex
            Finally
                PincelFondo.Dispose()
            End Try
        End Sub

        Protected Overloads Overrides Sub Paint(ByVal g As System.Drawing.Graphics, ByVal bounds As System.Drawing.Rectangle, ByVal source As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer, ByVal backBrush As System.Drawing.Brush, ByVal foreBrush As System.Drawing.Brush, ByVal alignToRight As Boolean)
            Dim Imagen As Bitmap
            Dim miObjeto As Object
            Dim miPos As Point
            Dim DgP As Datagrid.DataGridP
            Dim PincelFondo As Brush

            Dim dv As DataView
            Dim cm As CurrencyManager
            Dim filav As DataRowView
            Dim id As String

            Try
                PincelFondo = backBrush.Clone

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
                        End If
                    End If
                End If
                'Catch ex As Exception
                '    'no pasa nada, dibujamos con los valores por defecto
                'End Try


                'obtenemos la imagen que corresponde pintar
                miObjeto = getcolumnvalueatrow(source, rowNum)

                If TypeOf miObjeto Is Bitmap Then
                    Imagen = miObjeto
                End If

                'rellenamos el rectángulo
                g.FillRectangle(PincelFondo, bounds.X, bounds.Y, bounds.Width, bounds.Height)

                'dibujamos la imagen
                If Not Imagen Is Nothing Then
                    miPos = EstablecerPosicion(Imagen, bounds)
                    g.DrawImageUnscaled(Imagen, miPos)
                    'g.DrawImageUnscaled(Imagen, bounds.X + (bounds.Width - Imagen.Width), bounds.Y, Imagen.Width, Imagen.Height)
                Else
                    'MyBase.Paint(g, bounds, source, rowNum, backBrush, foreBrush, alignToRight)
                End If
            Catch ex As Exception
                Throw ex
            Finally
                PincelFondo.Dispose()
            End Try
        End Sub

        Private Function EstablecerPosicion(ByVal Imagen As Bitmap, ByVal bounds As Rectangle) As Point
            Dim posX As Integer
            Dim posY As Integer

            Try
                'ponemos la posición vertical
                Select Case Me.ControlVertAlignment
                    Case Estilo.ControlVertAlignment.Top
                        posY = bounds.Y
                    Case Estilo.ControlVertAlignment.Centrado
                        posY = bounds.Y + ((bounds.Height / 2) - (Imagen.Height / 2))
                    Case Estilo.ControlVertAlignment.Bottom
                        posY = bounds.Bottom - Imagen.Height
                End Select

                'ponemos la posición horizontal
                Select Case Me.ControlHorizAlignment
                    Case Estilo.ControlHorizAlignment.Centrado
                        posX = bounds.X + ((bounds.Width / 2) - (Imagen.Width / 2))
                    Case Estilo.ControlHorizAlignment.Derecha
                        posX = bounds.Right - Imagen.Width
                    Case Estilo.ControlHorizAlignment.Izquierda
                        posX = bounds.Left
                End Select

                Return New Point(posX, posY)
            Catch ex As Exception
                Throw ex
            End Try

        End Function

#End Region


#End Region


    End Class

#Region "ejemplo de utilización"
    'Como vemos, el funcionamiento es exactamente igual que con cualquier columnstyle
    'lo único que tenemos que hacer es que la columna a la que está enlazado el
    'DataGrid (mapeada por el mappingname) contenga en ese campo un Image
    '
    '-----------------------------------------------------> crear el datastyle y el columnstyle
    'dim DGStyle as datagridstyle
    'dim ColumnaImagen as DataGridColumnStyle
    '(...)
    '
    '-----------------------------------------------------> establecer props del columnstyle, como con cualquiera
    'ColumnaImagen = new DataGridImageColumn();
    'ColumnaImagen.MappingName = "Imagenes";
    'ColumnaImagen.HeaderText = "Bitmap";
    'ColumnaImagen.Width = 100;
    '
    '-----------------------------------------------------> añadir el columnstyle al datastyle
    '
    'DGStyle.GridColumnStyles.Add(ColumnaImagen);
    '
    '-----------------------------------------------------> anadir el datstyle al datagrid
    'Datagrid1.TableStyles.Add(DGStyle)
    '
    '
#End Region

End Namespace

