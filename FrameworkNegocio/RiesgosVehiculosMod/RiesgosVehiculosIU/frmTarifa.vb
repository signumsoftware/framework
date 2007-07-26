Imports FN.Seguros.Polizas.DN
Imports FN.RiesgosVehiculos.DN
Imports Framework.IU.IUComun

Public Class frmTarifa
    Inherits MotorIU.FormulariosP.FormularioBase

    Private mControlador As FN.RiesgosVehiculos.IU.Controladores.frmTarifaCtrl

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        mControlador = Me.Controlador()

        If Not Me.Paquete Is Nothing AndAlso Me.Paquete.Contains("DN") Then
            Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN = Me.Paquete("DN")
            Me.ctrlTarifa1.Tarifa = tarifa
            ActualizarValoresRenovacion(tarifa)

            Me.Text = "Tarifa " & tarifa.ID

        End If
    End Sub

#Region "Eventos formulario"

    Private Sub tsbGuardar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsbGuardar.Click
        Try
            mControlador.GuardarTarifa(ctrlTarifa1.Tarifa)
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub tsbNavegarCuestionario_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsbNavegarCuestionario.Click
        Dim cr As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        Dim datosT As DatosTarifaVehiculosDN

        Try
            datosT = CType(ctrlTarifa1.Tarifa.DatosTarifa, DatosTarifaVehiculosDN)
            cr = mControlador.RecuperarCuestionarioR(datosT.HeCuestionarioResuelto)

            If Me.Paquete.ContainsKey("CuestionarioResuelto") Then
                Me.Paquete.Item("CuestionarioResuelto") = cr
            Else
                Me.Paquete.Add("CuestionarioResuelto", cr)
            End If

            Me.cMarco.Navegar("Cuestionario1", Me, Me.MdiParent, TipoNavegacion.CerrarLanzador, Me.GenerarDatosCarga, Me.Paquete, Nothing)

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub chkTarifaRenovacion_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles chkTarifaRenovacion.CheckedChanged
        Try
            HabilitarDeshabilitarRenovacion(chkTarifaRenovacion.Checked)
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub txtBonificacionActual_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtBonificacionActual.LostFocus
        Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN
        Dim valorB As Double

        Try
            tarifa = Me.ctrlTarifa1.Tarifa
            If Not Double.TryParse(txtBonificacionActual.Text, valorB) Then
                Throw New ApplicationException("El valor de bonificación no es correcto")
                txtBonificacionActual.Text = 0
            End If
            CType(tarifa.DatosTarifa, FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN).ValorBonificacion = valorB

            Me.ctrlTarifa1.Tarifa = tarifa

            ActualizacionNivelBonificacion(tarifa)

        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub txtNumSiniestros_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtNumSiniestros.LostFocus
        Dim resultado As Integer

        Try
            If Not Integer.TryParse(txtNumSiniestros.Text, resultado) Then
                resultado = 0
                txtBonificacionActual.Text = resultado
                Throw New ApplicationException("El valor de bonificación no es correcto")
            End If

            Me.ctrlTarifa1.NumeroSiniestros = resultado
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub ctrlTarifa1_EventoTarificar() Handles ctrlTarifa1.EventoTarificar
        Dim valorB As Double

        Try
            If Me.chkTarifaRenovacion.Checked Then
                valorB = CalcularValorBonificacion()
                txtBonificacionActual.Text = valorB
                Me.ctrlTarifa1.Tarifa.DatosTarifa.ValorBonificacion = valorB
                ActualizacionNivelBonificacion(Me.ctrlTarifa1.Tarifa)
            End If
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#End Region

#Region "Métodos"

    Private Sub HabilitarDeshabilitarRenovacion(ByVal estado As Boolean)
        txtNumSiniestros.Enabled = estado
        txtBonificacionActual.Enabled = estado
    End Sub

    Private Sub ActualizacionNivelBonificacion(ByVal tarifa As FN.Seguros.Polizas.DN.TarifaDN)
        Dim nivelB As String
        Dim controlador As New RiesgosVehiculos.IU.Controladores.frmTarifaCtrl()
        Dim riesgo As RiesgosVehiculos.DN.RiesgoMotorDN

        riesgo = CType(tarifa.Riesgo, RiesgosVehiculos.DN.RiesgoMotorDN)

        nivelB = controlador.CalcularNivelBonificacion(tarifa.DatosTarifa.ValorBonificacion, riesgo.ModeloDatos.Categoria, Nothing, tarifa.FEfecto)

        If nivelB Is String.Empty Then
            nivelB = " VALOR NO VÁLIDO "
        End If

        lblNivelBonificacion.Text = nivelB

    End Sub

    'TODO: método provisional, debe ir en la LN de Riesgos Vehículos
    Private Function CalcularValorBonificacion() As Double
        Dim numSiniestros As Integer
        Dim valorBonificacion As Double
        Dim resBonificacion As Double = 1
        Dim resultado As Double

        If Not Integer.TryParse(txtNumSiniestros.Text, numSiniestros) Then
            Throw New ApplicationException("El dato de siniestros debe ser un número entero válido y positivo")
        End If

        If Not Double.TryParse(txtBonificacionActual.Text, valorBonificacion) Then
            Throw New ApplicationException("El valor de bonificación debe ser un número decimal válido")
        End If

        If numSiniestros = 0 Then
            resBonificacion = 0.95
        Else
            resBonificacion = Math.Pow(1.25, numSiniestros)
        End If

        resultado = valorBonificacion * resBonificacion

        If resultado < 0.5 Then
            resultado = 0.5
        End If

        If resultado > 3.5 Then
            resultado = 3.5
        End If

        Return resultado

    End Function

    Private Sub ActualizarValoresRenovacion(ByVal tarifa As TarifaDN)
        Me.txtNumSiniestros.Text = 0
        Me.txtBonificacionActual.Text = tarifa.DatosTarifa.ValorBonificacion
        ActualizacionNivelBonificacion(tarifa)
    End Sub

#End Region




End Class
