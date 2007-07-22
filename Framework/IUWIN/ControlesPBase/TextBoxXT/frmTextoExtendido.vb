Public Class frmTextoExtendido

    Public Texto As String = String.Empty
    Public Habilitado As Boolean = True

    Private Sub frmTextoExtendido_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Me.TextBox1.Text = Texto

        'MessageBox.Show("Habilitado = " & Habilitado)

        If Not Me.Habilitado Then
            Me.cmd_Cancelar.Visible = False
            Me.TextBox1.ReadOnly = True
        End If
    End Sub

    Private Sub cmd_Cancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Cancelar.Click
        Me.DialogResult = Windows.Forms.DialogResult.Cancel
    End Sub

    Private Sub cmd_Aceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Aceptar.Click
        If Habilitado Then
            Texto = Me.TextBox1.Text
        End If
        Me.DialogResult = Windows.Forms.DialogResult.OK
    End Sub

End Class
