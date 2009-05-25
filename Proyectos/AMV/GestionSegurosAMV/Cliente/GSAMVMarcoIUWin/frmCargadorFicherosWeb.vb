Imports System.IO

Public Class frmCargadorFicherosWeb

    Private mBytes As Byte()

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        If OpenFileDialog1.ShowDialog(Me) = Windows.Forms.DialogResult.OK Then
            mBytes = File.ReadAllBytes(OpenFileDialog1.FileName)
            Label1.Text = Path.GetFileName(OpenFileDialog1.FileName)
        End If

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click

        If mBytes IsNot Nothing Then

            Dim gsamvas As New GSAMVAS.CuestionarioAS()
            Dim errores As String() = gsamvas.CargarFicheroWeb(mBytes)

            ListBox1.Items.Clear()
            ListBox1.Items.AddRange(errores)

            mBytes = Nothing

        End If

    End Sub
End Class