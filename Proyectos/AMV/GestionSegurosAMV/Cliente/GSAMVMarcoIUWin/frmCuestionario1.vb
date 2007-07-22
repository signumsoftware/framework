Public Class frmCuestionario1

#Region "Atributos"

    Private mControlador As GSAMVControladores.ctrlCuestionarioFrm
    Private mPresupuesto As FN.Seguros.Polizas.DN.PresupuestoDN

#End Region

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        mControlador = Me.Controlador()

        'si el paquete es nulo, recupero el cuestionario actual (OJO fecha efecto) y se lo paso al control
        Dim cuestionario As Framework.Cuestionario.CuestionarioDN.CuestionarioDN = Nothing
        Dim cuestionarioResuelto As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN = Nothing

        If Me.Paquete Is Nothing OrElse Not Me.Paquete.Contains("CuestionarioResuelto") Then
            cuestionario = mControlador.RecuperarCuestionarioFecha(Now())
        Else
            cuestionarioResuelto = CType(Me.Paquete.Item("CuestionarioResuelto"), Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN)
        End If

        If cuestionario IsNot Nothing Then
            Me.CtrlCuestionarioTarificacion1.Cuestionario = cuestionario
        End If

        If cuestionarioResuelto IsNot Nothing Then
            Me.CtrlCuestionarioTarificacion1.CuestionarioResuelto = cuestionarioResuelto
        End If

        AjustarResize()

    End Sub

    Private Sub CtrlCuestionarioTarificacion1_CuestionarioFinalizado() Handles CtrlCuestionarioTarificacion1.CuestionarioFinalizado
        Dim pr As FN.Seguros.Polizas.DN.PresupuestoDN
        Try
            Using New AuxIU.CursorScope()
                'Dim mipaquete As New Hashtable()
                'mipaquete.Add("Presupuesto", mControlador.GenerarPresupuestoxCuestionarioRes(Me.CtrlCuestionarioTarificacion1.CuestionarioResuelto))
                'Me.cMarco.Navegar("TarificarPresupuesto", Me, Me.ParentForm, MotorIU.Motor.TipoNavegacion.CerrarLanzador, mipaquete)

                'If Me.Paquete.Contains("TipoDevuelto") AndAlso Me.Paquete.Item("TipoDevuelto") = GetType(Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN).FullName Then
                '    Me.Paquete.Add("DN", Me.CtrlCuestionarioTarificacion1.CuestionarioResuelto)
                '    Me.Close()
                'Else
                pr = mControlador.GenerarPresupuestoxCuestionarioRes(Me.CtrlCuestionarioTarificacion1.CuestionarioResuelto)

                Me.Paquete.Add("DN", pr)
                Me.cMarco.Navegar("FG", Me, Me.MdiParent, MotorIU.Motor.TipoNavegacion.CerrarLanzador, Me.GenerarDatosCarga, Me.Paquete, Nothing)
                'End If
            End Using

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub CtrlCuestionarioTarificacion1_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles CtrlCuestionarioTarificacion1.Resize
        Try
            AjustarResize()
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Private Sub AjustarResize()
        Me.Height = Me.CtrlCuestionarioTarificacion1.Bottom + 50

    End Sub
End Class