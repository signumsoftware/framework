Public Class frmContenedorRTF

    Private mHuellaRTF As FN.GestionPagos.DN.HuellaContenedorRTFDN
    Private mContenedorRTF As FN.GestionPagos.DN.ContenedorRTFDN

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        Dim midn As Object
        Try
            midn = Me.Paquete("DN")
        Catch ex As Exception
            Throw New ApplicationException("El paquete que contiene el RTF está vacío (" & ex.Message & ")", ex)
        End Try

        If TypeOf midn Is FN.GestionPagos.DN.HuellaContenedorRTFDN Then
            Me.mHuellaRTF = midn
            LNC.CargarHuella(Me.mHuellaRTF)
            Me.mContenedorRTF = Me.mHuellaRTF.EntidadReferida
        ElseIf TypeOf midn Is FN.GestionPagos.DN.ContenedorRTFDN Then
            Me.mContenedorRTF = midn
        End If

        Me.ctrlContenedorRTF.ContenedorRTF = Me.mContenedorRTF
    End Sub

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Aceptar.Click
        Try
            Dim mic As FN.GestionPagos.DN.ContenedorRTFDN = Me.ctrlContenedorRTF.ContenedorRTF
            If mic Is Nothing Then
                MessageBox.Show(Me.ctrlContenedorRTF.MensajeError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If
            Me.mContenedorRTF = mic
            If Not Me.mHuellaRTF Is Nothing Then
                Me.Paquete("DN") = Me.mHuellaRTF
            Else
                Me.Paquete("DN") = Me.mContenedorRTF
            End If
            Me.Close()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub cmd_Cancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Cancelar.Click
        Try
            Me.Close()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub
End Class