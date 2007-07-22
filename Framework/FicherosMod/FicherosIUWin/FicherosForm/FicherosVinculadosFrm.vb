Imports Framework.LogicaNegocios.Transacciones
Public Class FicherosVinculadosFrm
    Inherits MotorIU.FormulariosP.FormularioBase


    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click



        Try



            Me.DataGridView1.DataSource = Nothing
            Dim fas As New Framework.Ficheros.FicherosAS.CajonDocumentoAS
            Me.DataGridView1.DataSource = fas.VincularCajonDocumento.Tables(0)

        Catch ex As Exception

            MostrarError(ex)

        End Try


    End Sub
End Class