Imports AmvDocumentosDN

Public Class frmOperador

#Region "Atributos"

    Private mControlador As frmOperadorControlador

    'Private mModoAdmin As ModoAdmin
    Private mObtenerHuellaUser As Boolean

#End Region

#Region "Inicializar"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        mControlador = Me.Controlador

        'Recuperamos el operador por el ID
        Dim miOperador As OperadorDN

        Try
            miOperador = Nothing

            If Me.Paquete.Contains("ID") AndAlso Me.Paquete("ID") IsNot Nothing AndAlso Not String.IsNullOrEmpty(Me.Paquete("ID").ToString()) Then
                miOperador = Me.mControlador.RecuperarOperador(Me.Paquete("ID").ToString())
            Else
                miOperador = Nothing
            End If

            'If Me.Paquete.Contains("Modo") AndAlso Not String.IsNullOrEmpty(Me.Paquete("Modo").ToString()) Then
            '    mModoAdmin = Paquete("Modo")
            'End If

            Me.CtrlOperador1.Operador = miOperador

            ActualizarModoEdicionFormulario(miOperador)

        Catch ex As Exception
            Me.MostrarError(ex, Me)
        End Try

    End Sub

#End Region

#Region "Delegados eventos"

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Aceptar.Click
        Try
            Aceptar()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Try
            Close()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdAltaBaja_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmdAltaBaja.Click
        Try
            AltaBaja()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

#End Region

#Region "Métodos"

    Private Sub Aceptar()
        'If mModoAdmin <> ModoAdmin.Consultar Then
        Dim miOperador As OperadorDN = CtrlOperador1.Operador

        If miOperador Is Nothing Then
            MessageBox.Show("Los datos del operador no son correctos", "Operador", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return
        End If

        mControlador.GuardarOperador(miOperador)

        If Paquete.Contains("HuellaEntidadUser") Then
            Paquete.Add("HuellaEntidadUser", New HuellaOperadorDN(miOperador))
        Else
            Paquete.Item("HuellaEntidadUser") = New HuellaOperadorDN(miOperador)
        End If

        'End If

        Me.Close()
    End Sub

    Private Sub AltaBaja()
        Dim miOperador As OperadorDN = CtrlOperador1.Operador

        If miOperador Is Nothing Then
            MessageBox.Show("Los datos del operador no son correctos")
            Return
        End If

        If miOperador.Baja Then
            mControlador.ReactivarOperador(miOperador.ID)
        Else
            mControlador.BajaOperador(miOperador.ID)
        End If

        Close()

    End Sub

    Private Sub ActualizarModoEdicionFormulario(ByVal operador As OperadorDN)
        cmdAltaBaja.Enabled = False

        If operador IsNot Nothing Then
            cmdAltaBaja.Enabled = True

            If operador.Baja Then
                Me.CtrlOperador1.ModoConsulta = True
                cmdAltaBaja.Text = "Alta"
                cmd_Aceptar.Enabled = False
            Else
                cmdAltaBaja.Text = "Baja"
                cmd_Aceptar.Enabled = True
            End If
        End If

    End Sub

#End Region

    Private Sub frmOperador_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub
End Class
