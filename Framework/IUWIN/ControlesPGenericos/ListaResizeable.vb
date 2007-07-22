Public Class ListaResizeable

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
    End Sub

    Public Property ListBox() As System.Windows.Forms.ListBox
        Get
            Return Me.ListBox1
        End Get
        Set(ByVal value As System.Windows.Forms.ListBox)
            Me.ListBox1 = value
        End Set
    End Property


    'controlamos el ratón para que, si está cerca del borde, pueda resizear la lista al tamaño que desee
    Private Sub ListaResizeable_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseMove
        Try
            MoverRaton(sender, e)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub



    Private Sub ListBox1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles ListBox1.MouseMove
        Try
            MoverRaton(sender, e)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub MoverRaton(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        'Me.Label1.Text = e.Y & " - " & (Me.Height - 4)
        If e.Y >= (Me.Height - 4) Then
            Windows.Forms.Cursor.Current = Windows.Forms.Cursors.SizeNS
        Else
            Windows.Forms.Cursor.Current = Windows.Forms.Cursors.Default
        End If
    End Sub

    Private Resizing As Boolean

    Protected Overrides Sub OnMouseDown(ByVal e As System.Windows.Forms.MouseEventArgs)
        MyBase.OnMouseDown(e)
        Resizeando(e)
    End Sub

    Private Sub ListBox1_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles ListBox1.MouseDown
        Resizeando(e)
    End Sub

    Private Sub Resizeando(ByVal e As System.Windows.Forms.MouseEventArgs)
        If e.Y >= (Me.Height - 4) Then
            Resizing = True
        End If
    End Sub

    Private Sub Resizear(ByVal e As System.Windows.Forms.MouseEventArgs)
        If Resizing Then
            If e.Y >= 54 AndAlso e.Y < Me.Parent.Height Then
                Me.Height = e.Y
            End If
        End If

        Resizing = False
    End Sub

    Protected Overrides Sub OnMouseUp(ByVal e As System.Windows.Forms.MouseEventArgs)
        Resizear(e)
        MyBase.OnMouseUp(e)
    End Sub

    Private Sub ListBox1_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles ListBox1.MouseUp
        Resizear(e)
    End Sub
End Class
