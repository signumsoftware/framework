Public Class frmImagen

    Private mip As PaqueteImagen

#Region "inicializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Try
            mip = Me.Paquete("Paquete")
        Catch ex As Exception
            Throw New ApplicationException("Error al recibir el paquete que contiene la Imagen")
        End Try

        Me.ctrlImagen.ContenedorImagen = mip.ContenedorImagen

    End Sub
#End Region


#Region "métodos"
    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Aceptar.Click
        Try
            Dim mic As FN.GestionPagos.DN.ContenedorImagenDN = Me.ctrlImagen.ContenedorImagen
            If mic Is Nothing Then
                MessageBox.Show(Me.ctrlImagen.MensajeError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If

            mip.ContenedorImagen = mic

            Me.Close()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Try
            Me.Close()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#End Region




End Class