Public Class frmAcercaDe

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        'TODO: Debería ser dinámico
        'Me.lblVersion.Text = Application.ProductVersion
        Me.lblVersion.Text = "0.0.0.1 CTP"
    End Sub

    Private Sub PictureBox1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PictureBox1.Click
        Try
            Using New AuxIU.CursorScope(Cursors.WaitCursor)
                System.Diagnostics.Process.Start("http://www.signumsoftware.com")
            End Using
        Catch ex As Exception
            MostrarError(ex)
        End Try
        ' Dim f As ClienteAdminLNC.TalonesLNC

    End Sub

    Private Sub PictureBox1_MouseEnter(ByVal sender As Object, ByVal e As System.EventArgs) Handles PictureBox1.MouseEnter
        Me.PictureBox1.BorderStyle = BorderStyle.FixedSingle
    End Sub

    Private Sub PictureBox1_MouseLeave(ByVal sender As Object, ByVal e As System.EventArgs) Handles PictureBox1.MouseLeave
        Me.PictureBox1.BorderStyle = BorderStyle.None
    End Sub

End Class