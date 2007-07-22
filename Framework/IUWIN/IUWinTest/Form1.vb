Imports Framework.DatosNegocio.Arboles
Imports Framework.DatosNegocio

Public Class Form1

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Me.TextBox1.Extendido = True
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        Me.TextBox1.Extendido = False
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        Me.TextBox1.Enabled = Not Me.TextBox1.Enabled
    End Sub

    Private num As Integer = 0

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        Select Case num
            Case Is = 0
                Me.TextBox1.BackColor = Color.AliceBlue
                num += 1
            Case Is = 1
                Me.TextBox1.BackColor = Color.LightPink
                num += 1
            Case Is = 2
                Me.TextBox1.BackColor = Color.LightBlue
                num += 1
            Case Is = 3
                Me.TextBox1.BackColor = SystemColors.WindowText
                num = 0
        End Select

        Me.Text = Me.TextBox1.BackColor.ToString
    End Sub

    Private Sub Form1_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Me.Text = Me.TextBox1.BackColor.ToString
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        Try
            Me.Label1.Text = AuxIU.ConversorTextoANumero.ConvertirNumeroATexto(CDbl(Me.TextBox2.Text))
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click
        Dim d As Double = Me.TextBox4.Text
        Dim o As Object = d
        Me.TextBox3.Text = o
    End Sub
End Class


