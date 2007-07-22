Imports AmvDocumentosDN

Public Class frmOperacionEnFichero

#Region "Atributos"

    Private mControlador As frmOperacionEnFicheroCtrl

#End Region

#Region "Inicializar"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = Me.Controlador

        Dim miOperacion As OperacionEnRelacionENFicheroDN

        Try
            miOperacion = Nothing

            If Me.Paquete.Contains("ID") AndAlso Not String.IsNullOrEmpty(Me.Paquete("ID").ToString()) Then
                miOperacion = Me.mControlador.RecuperarOperacionxID(Me.Paquete("ID").ToString())
            End If

            If miOperacion IsNot Nothing Then
                Me.CtrlOperacionEnFichero1.OperacionEnRelacionFichero = miOperacion
            Else
                Throw New ApplicationException("No hay ninguna operación")
            End If


        Catch ex As Exception
            MostrarError(ex, ex.Message)
            Close()
        End Try

    End Sub

#End Region

#Region "Manejadores de eventos"

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAceptar.Click
        Try
            Close()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub frmOperacionEnFichero_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyUp
        Try
            CtrlOperacionEnFichero1.RecogerTeclaAbreviada(e)
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

#End Region


End Class