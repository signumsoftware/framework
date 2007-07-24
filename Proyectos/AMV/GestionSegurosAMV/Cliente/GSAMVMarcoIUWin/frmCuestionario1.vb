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
        Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN
        Try
            Using New AuxIU.CursorScope()
                Dim cr As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN = Me.CtrlCuestionarioTarificacion1.CuestionarioResuelto

                If Me.Paquete.ContainsKey("SoloCuestionario") AndAlso Me.Paquete.Item("SoloCuestionario") Then
                    Me.Paquete.Add("DN", cr)
                    Me.Close()
                Else
                    If cr.ColRespuestaDN.RecuperarRespuestaaxPregunta("TarificacionPrueba").IValorCaracteristicaDN.Valor Then
                        Dim tiempoTarificado As New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias()
                        tiempoTarificado.Anyos = 1
                        tarifa = mControlador.GenerarTarifaxCuestionarioRes(cr, tiempoTarificado)

                        Me.Paquete.Add("DN", tarifa)
                        Me.cMarco.Navegar("TarificarPrueba", Me, Me.MdiParent, MotorIU.Motor.TipoNavegacion.CerrarLanzador, Me.GenerarDatosCarga, Me.Paquete, Nothing)
                    Else
                        pr = mControlador.GenerarPresupuestoxCuestionarioRes(cr)

                        Me.Paquete.Add("DN", pr)
                        Me.cMarco.Navegar("FG", Me, Me.MdiParent, MotorIU.Motor.TipoNavegacion.CerrarLanzador, Me.GenerarDatosCarga, Me.Paquete, Nothing)
                    End If
                End If



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