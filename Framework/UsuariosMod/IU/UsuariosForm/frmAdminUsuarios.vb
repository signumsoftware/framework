#Region "Importaciones"

Imports Framework.Usuarios.DN

#End Region

Public Class frmAdminUsuarios

#Region "Atributos"

    Private mControlador As Framework.Usuarios.IUWin.Controladores.ctrlAdminUsuariosForm
    Private mNickOLD As String

    'Private mModoAdmin As ModoAdmin

#End Region

#Region "Inicializar"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        mControlador = Me.Controlador

        'Recuperamos el usuario por el ID
        Dim miPrincipal As PrincipalDN

        Try
            miPrincipal = Nothing

            If Me.Paquete.Contains("ID") AndAlso Me.Paquete("ID") IsNot Nothing AndAlso Not String.IsNullOrEmpty(Me.Paquete("ID").ToString()) Then
                miPrincipal = Me.mControlador.ObtenerPrincipal(Me.Paquete("ID").ToString())
                mNickOLD = miPrincipal.UsuarioDN.Nombre
            Else
                miPrincipal = Nothing
            End If

            'If Me.Paquete.Contains("Modo") AndAlso Not String.IsNullOrEmpty(Me.Paquete("Modo").ToString()) Then
            '    mModoAdmin = Paquete("Modo")
            'End If

            Me.CtrlUsuarios1.Principal = miPrincipal

            'If mModoAdmin = ModoAdmin.Consultar Then
            '    Me.CtrlUsuarios1.ModoConsulta = True
            'End If

            ActualizarBotonAltaBaja(miPrincipal)

        Catch ex As Exception
            Me.MostrarError(ex, Me)
        End Try

    End Sub

#End Region

#Region "Delegados de Eventos"

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAceptar.Click
        Try
            Me.Aceptar()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Try
            Me.Close()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdAltaBaja_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAltaBaja.Click
        Try
            AltaBaja()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

#End Region

#Region "Métodos"

    Private Sub Aceptar()
        Dim miPrincipal As PrincipalDN

        miPrincipal = CtrlUsuarios1.Principal

        If miPrincipal Is Nothing Then
            MessageBox.Show("Los datos del usuario no son correctos")
            Return
        End If

        If CtrlUsuarios1.ModificarDI Then
            Dim miDI As DatosIdentidadDN
            miDI = CtrlUsuarios1.DatosIdentidad

            If miDI Is Nothing Then
                MessageBox.Show("Los datos del usuario no son correctos")
                Return
            End If

            mControlador.GuardarPrincipal(miPrincipal, miDI, mNickOLD)
        Else
            mControlador.GuardarPrincipal(miPrincipal)
        End If

        Close()
    End Sub

    Private Sub AltaBaja()
        Dim miPrincipal As PrincipalDN
        Dim miDI As DatosIdentidadDN

        miPrincipal = CtrlUsuarios1.Principal

        If miPrincipal Is Nothing Then
            MessageBox.Show("Los datos del usuario no son correctos")
            Return
        End If

        If miPrincipal.Baja Then
            miDI = CtrlUsuarios1.DatosIdentidad
            mControlador.AltaPrincipal(miPrincipal, miDI)
        Else
            mControlador.BajaPrincipal(miPrincipal)
        End If

        Close()

    End Sub

    Private Sub ActualizarBotonAltaBaja(ByVal principal As PrincipalDN)
        If principal Is Nothing Then
            cmdAltaBaja.Enabled = False
            Return
        End If

        If principal.Baja Then
            cmdAltaBaja.Text = "Alta"
            cmdAceptar.Enabled = False
        Else
            cmdAltaBaja.Text = "Baja"
            cmdAceptar.Enabled = True
        End If

    End Sub

#End Region


End Class
