Imports Framework.IU.IUComun

Public Class frmAutorizacion

    Private mControlador As Controladores.frmAutorizacionctrl

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = Me.Controlador
    End Sub

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Aceptar.Click
        Try
            Using New AuxIU.CursorScope(Cursors.WaitCursor)
                If Me.mControlador.LogarseEnSistema(Me.txtNick.Text.Trim(), Me.txtClave.Text.Trim()) Then
                    Me.cMarco.Navegar("GestorDocumentos", Me, Nothing, TipoNavegacion.CerrarLanzador, Me.GenerarDatosCarga, Nothing)
                Else
                    MessageBox.Show("No se ha podido logar en el sistema." & Chr(13) & Chr(13) & "Compruebe su nombre de usuario y contraseña", "Login", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                End If
            End Using
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Try
            Application.Exit()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub
End Class