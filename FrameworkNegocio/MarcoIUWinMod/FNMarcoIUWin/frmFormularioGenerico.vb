Public Class frmFormularioGenerico

    Private Sub frmFormularioGenerico_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        ' ocultarMostrarBotones
        OculatarMostrarBotones()

        ' cargar los datos

        Dim tipo As System.Type = Nothing
        Dim entidad As Framework.DatosNegocio.EntidadBaseDN = Nothing
        Dim nombreMapeadoVis As String = Nothing
        If Not Me.Paquete Is Nothing Then


            If Me.Paquete.Contains("TipoEntidad") Then
                tipo = Me.Paquete("TipoEntidad")
            End If
            If Me.Paquete.Contains("DN") Then
                entidad = Me.Paquete("DN")
                tipo = entidad.GetType
            Else

                If tipo IsNot Nothing Then

                    If Me.Paquete.Contains("ID") Then
                        Dim identidad As String = Me.Paquete("ID")
                        ' se recupera la entidad del sistema 
                        Dim mias As New Framework.AS.MV2AS
                        entidad = mias.RecuperarDNGenerico(New Framework.DatosNegocio.HEDN(tipo, identidad, Nothing))

                    Else

                        entidad = Activator.CreateInstance(tipo)
                    End If

                End If
            End If

            Me.CtrlGD1.TipoEntidad = tipo

            If Me.Paquete.Contains("NombreInstanciaMapVis") Then
                nombreMapeadoVis = Me.Paquete("NombreInstanciaMapVis")
                Me.CtrlGD1.NombreInstanciaMap = nombreMapeadoVis




            End If

            Dim recmap As New MV2DN.RecuperadorMapeadoXFicheroXMLAD(Framework.Configuracion.AppConfiguracion.DatosConfig("RutaCargaMapVis"))
            Me.CtrlGD1.RecuperadorMap = recmap
            If nombreMapeadoVis Is Nothing Then


                Me.CtrlGD1.InstanciaMap = recmap.RecuperarInstanciaMap(tipo)


            End If


            If Me.CtrlGD1.InstanciaMap Is Nothing Then
                Me.CtrlGD1.InstanciaMap = Me.CtrlGD1.GenerarMapeadoBasicoEntidadDN(tipo)
            End If


            Me.CtrlGD1.Poblar()
            Me.Inicializar()
            Me.CtrlGD1.DN = entidad
            Me.Inicializar()
            'If Not entidad Is Nothing Then

            'End If

        End If


    End Sub



    Private Sub OculatarMostrarBotones()

        If Me.Modal Then
            Me.btnGuardar.Visible = False
        Else
            Me.btnAceptar.Visible = False

        End If


    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancelar.Click
        Me.Close()

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAceptar.Click
        Me.CtrlGD1.IUaDNgd()
        If Not Me.Paquete Is Nothing Then
            If Me.Paquete.ContainsKey("DN") Then
                Me.Paquete.Item("DN") = Me.CtrlGD1.DN

            Else
                Me.Paquete.Add("DN", Me.CtrlGD1.DN)

            End If
        End If
        Me.Close()
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGuardar.Click
        Me.CtrlGD1.IUaDNgd()
        ' cofigo listo para guardar

        Dim mias As New Framework.AS.MV2AS
        Me.CtrlGD1.DN = mias.GuardarDNGenerico(Me.CtrlGD1.DN, Nothing, Me)
        Me.Inicializar()

    End Sub

End Class