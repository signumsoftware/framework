Public Class frmPresupuesto
    Inherits MotorIU.FormulariosP.FormularioBase

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        If Not Me.Paquete Is Nothing AndAlso Me.Paquete.Contains("Presupuesto") Then
            Dim p As FN.Seguros.Polizas.DN.PresupuestoDN = Me.Paquete("Presupuesto")
            Me.ctrlPresupuesto1.Presupuesto = p
            Me.Text = "Presupuesto " & p.ID
        End If
    End Sub

    Private Sub cmd_Aceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Aceptar.Click
        Try
            Dim p As FN.Seguros.Polizas.DN.PresupuestoDN = Me.ctrlPresupuesto1.Presupuesto
            If p Is Nothing Then
                MessageBox.Show(Me.ctrlPresupuesto1.MensajeError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If
            If Me.Paquete.Contains("Presupuesto") Then
                Me.Paquete("Presupusto") = p
            Else
                Me.Paquete.Add("Presupuesto", p)
            End If
            Me.Close()
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub
End Class