Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Reflection
Imports System.Windows.Forms

Namespace Datagrid


    'Esta clase hereda de datagrid, y está pensada para poder utlizar los datacolumnstyles
    'personalizados. Utiliza doublebuffer para evitar el parpadeo, que si no puede hacer
    'que en función del redibujo del grid se muestren con errores algunos de los elementos
    'de los datacolumnstyles personalizados. Además, controla el tamaño mínimo de las filas
    'y las columnas, y no permite que se resizeen por debajo de esos valores, ya que si no
    'los datacolumnstyles personalizados comienzan a dibujarse mal.

    Public Class DataGridP
        Inherits System.Windows.Forms.DataGrid

#Region "campos"
        Private mColumnResize As Boolean = False
        Private mRowResize As Boolean = False
        Private mColumnIzquierda As Integer = 0
        Private mRowTop As Integer = 0
        Private mrowIndex As Integer = -1
        Private mColumnIndex As Integer = -1
        Private mPreferredWidth As Integer = -1
        Private mPreferredHeight As Integer = -1
        'un arraylist en el que tenemos los IDs del campo ID que
        'queremos pintar como resaltados
        Private mIDsResaltados As New ArrayList 'lo instanciamos
        'el backcolor que van a tener las filas resaltadas
        Private mBackColorResaltado As Color
        'el forecolor que van a tener las filas resaltadas
        Private mForeColorResaltado As Color
        'el número de la columna que guarda el ID en el datasource
        Private mColumnaID As Integer
        'si hay que activar el Resaltado
        Private mResaltar As Boolean
#End Region

#Region "constructor"
        Public Sub New()
            'usamos el doble buffer para evitar el parpadeo
            Me.SetStyle(ControlStyles.DoubleBuffer, True)
        End Sub

        Public Sub New(ByVal pBackColorResaltado As Color)
            'usamos el doble buffer para evitar el parapdeo
            Me.SetStyle(ControlStyles.DoubleBuffer, True)
            'ponemos el colorde fondo
            Me.mBackColorResaltado = pBackColorResaltado
        End Sub
#End Region

#Region "propiedades"

        'establece y devuelve si se debe o no resaltar al hacer click
        Public Property Resaltar() As Boolean
            Get
                Return mResaltar
            End Get
            Set(ByVal Value As Boolean)
                mResaltar = Value
            End Set
        End Property

        'OJO: debe ser el índice en el que se encuentra la Columna q va a ser el ID
        'en el DATASOURCE, no el índice de la columna en el DATATABLESTYLE (si es q existe en él)
        Public Property ColumnaID() As Integer
            Get
                Return mColumnaID
            End Get
            Set(ByVal Value As Integer)
                mColumnaID = Value
            End Set
        End Property


        'devuelve o establece el forecolor de las celdas q están realtadas
        Public Property ForeColorResaltado() As Color
            Get
                Return mForeColorResaltado
            End Get
            Set(ByVal Value As Color)
                mForeColorResaltado = Value
            End Set
        End Property

        'devuelve o establece la col de IDs que están resaltados
        Public Property IDsResaltados() As ArrayList
            Get
                Return mIDsResaltados
            End Get
            Set(ByVal Value As ArrayList)
                mIDsResaltados = Value
            End Set
        End Property

        'establecemos el color de fondo de una fila resaltada
        '(cuyo campo ID se encuentre en la col de reslatados)
        Public Property BackColorResaltado() As Color
            Get
                Return mBackColorResaltado
            End Get
            Set(ByVal Value As Color)
                mBackColorResaltado = Value
            End Set
        End Property
#End Region

#Region "métodos"

#End Region

        Protected Overrides Sub OnMouseDown(ByVal e As System.Windows.Forms.MouseEventArgs)
            Dim hti As HitTestInfo
            Dim ts As DataGridTableStyle

            Try
                MyBase.OnMouseDown(e)

                hti = Me.HitTest(e.X, e.Y)

                If Me.TableStyles.Count <> 0 Then
                    ts = Me.TableStyles(0)

                    If hti.Type = System.Windows.Forms.DataGrid.HitTestType.ColumnResize And hti.Column <> -1 Then
                        mRowResize = False
                        mColumnResize = True
                        mColumnIzquierda = GetColumnIzquierda(hti.Column)
                        mColumnIndex = hti.Column
                        If TypeOf ts.GridColumnStyles(0) Is Estilo.ColumnStyle Then
                            mPreferredWidth = CType(ts.GridColumnStyles(hti.Column), Estilo.ColumnStyle).MinimunWidth
                        Else
                            mPreferredWidth = 10
                        End If
                    Else
                        If hti.Type = HitTestType.RowResize And hti.Row <> -1 Then
                            mColumnResize = False
                            mRowResize = True
                            mRowTop = GetRowTop(hti.Row)
                            mrowIndex = hti.Row

                        End If
                    End If
                End If


            Catch ex As Exception
                Throw ex
            End Try

        End Sub



        Protected Overrides Sub OnMouseUp(ByVal e As MouseEventArgs)
            Dim ts As DataGridTableStyle
            Dim hti As HitTestInfo

            MyBase.OnMouseUp(e)

            If Me.TableStyles.Count <> 0 Then
                ts = Me.TableStyles(0)
                hti = Me.HitTest(e.X, e.Y)

                If mColumnResize Then

                    If e.X < mColumnIzquierda + mPreferredWidth Then
                        ts.GridColumnStyles(mColumnIndex).Width = mPreferredWidth
                    End If

                    mColumnResize = False

                Else
                    If mRowResize Then

                        If e.Y < mRowTop + mPreferredHeight Then
                            SetRowHeight(mrowIndex, mPreferredHeight)
                        End If

                        mRowResize = False
                    End If
                End If
            End If


        End Sub


        ' como una fila está compuesta por diferentes filas, tenemos que recorrerlas
        'y encontrar el miminumheight más alto
        Private Function CalcularPreferredHeight() As Integer
            Dim ts As DataGridTableStyle
            Dim maxHeight As Integer
            Dim cs As Estilo.ColumnStyle

            ts = Me.TableStyles(0)
            maxHeight = 0

            For Each cs In ts.GridColumnStyles
                maxHeight = Math.Max(maxHeight, cs.MinimunHeight)
            Next cs

            Return maxHeight
        End Function

        Private Function GetRowTop(ByVal rowNum As Integer) As Integer
            Dim dg As New System.Windows.Forms.DataGrid
            Dim dgMethod As MethodInfo

            dg = New System.Windows.Forms.DataGrid
            dgMethod = dg.GetType().GetMethod("GetRowTop", BindingFlags.Instance Or BindingFlags.NonPublic) ', BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
            Return CInt(dgMethod.Invoke(Me, New Object() {rowNum}))
        End Function

        Private Sub SetRowHeight(ByVal rowIndex As Integer, ByVal height As Integer)
            Dim dg As New System.Windows.Forms.DataGrid
            Dim dgRowsInfo As PropertyInfo
            Dim rows As Object()
            Dim dgRowHeightInfo As PropertyInfo

            dg = New System.Windows.Forms.DataGrid
            dgRowsInfo = dg.GetType().GetProperty("DataGridRows", BindingFlags.Instance Or BindingFlags.NonPublic) ', BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
            rows = CType(dgRowsInfo.GetValue(Me, Nothing), Object())
            dgRowHeightInfo = rows(rowIndex).GetType().GetProperty("Height", BindingFlags.Instance Or BindingFlags.Public)

            dgRowHeightInfo.SetValue(rows(rowIndex), height, Nothing)
        End Sub

        Private Function GetColumnIzquierda(ByVal columnNum As Integer) As Integer
            Dim ts As DataGridTableStyle
            Dim columnIzquierda As Integer
            Dim i As Integer

            ts = TableStyles(0)

            columnIzquierda = 0

            If ts.RowHeadersVisible Then
                columnIzquierda = ts.RowHeaderWidth
            End If

            For i = 0 To columnNum - 1
                columnIzquierda += ts.GridColumnStyles(i).Width
            Next i

            Return columnIzquierda
        End Function

    End Class
End Namespace

