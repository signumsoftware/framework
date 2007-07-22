Public Class frmLogin

    Private mControlador As frmLoginCtrl
    Private mPaquete As PaqueteLogin

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = Me.Controlador

        If Not Me.Paquete Is Nothing AndAlso Me.Paquete.ContainsKey("Paquete") Then
            Me.mPaquete = Me.Paquete("Paquete")
            Me.Text = String.Concat(mPaquete.Titulo, " - ", Me.Text)
        Else
            Throw New ApplicationException("No se ha encontrado el paquete de configuración del formulario Login")
        End If
    End Sub

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Aceptar.Click
        Try
            Using New AuxIU.CursorScope(Cursors.WaitCursor)
                If Me.mControlador.LogarseEnSistema(Me.txtNick.Text.Trim(), Me.txtClave.Text.Trim()) Then
                    Me.cMarco.Navegar(mPaquete.FuncionNavegacion, Me, Nothing, MotorIU.Motor.TipoNavegacion.CerrarLanzador, Me.GenerarDatosCarga, Nothing)
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


    Private Sub cmdAceptar_VisibleChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmd_Aceptar.VisibleChanged
        Debug.WriteLine("cmdAceptar.Visible = " & Me.cmd_Aceptar.Visible)
    End Sub
End Class

Public Class PaqueteLogin
    Inherits MotorIU.PaqueteIU

    Public Titulo As String
    Public FuncionNavegacion As String
End Class