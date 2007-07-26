Imports Framework.IU.IUComun

Public Class frmConfiguracionPuntosImpresion

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        If Not Me.Paquete Is Nothing Then
            If Me.Paquete.Contains("ConfiguracionImpresion") Then
                Me.ctrlConfiguracionPuntosImpresion1.ConfiguracionImpresion = Me.Paquete("ConfiguracionImpresion")
            End If

            If Me.Paquete.Contains("ID") Then
                Me.ctrlConfiguracionPuntosImpresion1.ConfiguracionImpresion = LNC.RecuperarConfiguracionImpresion(Me.Paquete("ID"))
            End If
        End If
    End Sub

    Private Sub cmdGuardar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdGuardar.Click
        Try
            Dim ci As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN = Me.ctrlConfiguracionPuntosImpresion1.ConfiguracionImpresion

            If ci Is Nothing Then
                MessageBox.Show(Me.ctrlConfiguracionPuntosImpresion1.MensajeError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If

            Using New AuxIU.CursorScope(Cursors.WaitCursor)
                Me.ctrlConfiguracionPuntosImpresion1.ConfiguracionImpresion = LNC.GuardarConfiguracionImpresionTalon(ci)
            End Using
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdVistaPreliminar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdVistaPreliminar.Click
        Try
            Dim mici As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN = Me.ctrlConfiguracionPuntosImpresion1.ConfiguracionImpresion

            If mici Is Nothing Then
                MessageBox.Show(Me.ctrlConfiguracionPuntosImpresion1.MensajeError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If

            Dim mipago As New FN.GestionPagos.DN.PagoDN
            Dim miei As New FN.Personas.DN.PersonaFiscalDN
            miei.Persona = New FN.Personas.DN.PersonaDN
            miei.Persona.NIF = New FN.Localizaciones.DN.NifDN("00000001R")
            miei.Persona.Nombre = "José"
            miei.Persona.Apellido = "Marmoto Calderilla"

            mipago.Destinatario = miei.EntidadFiscalGenerica
            mipago.Importe = 535.5

            mipago.Talon = New FN.GestionPagos.DN.TalonDN
            mipago.Talon.Pago = mipago

            Dim carta As String = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Curabitur sed dui. Sed rutrum. Nullam non orci. Sed lectus. Sed felis justo, commodo nec, porta ac, volutpat sed, erat. Etiam vitae risus. Sed vel libero eget justo mollis gravida. Vivamus congue quam congue ipsum. Curabitur massa ligula, varius convallis, consectetuer non, aliquam at, leo. Aenean a dui. Aliquam auctor, tortor a pretium tempor, ipsum est semper lectus, non accumsan odio metus sed est. Aliquam elit. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Ut convallis suscipit felis. "
            carta += Chr(13) & Chr(10) & Chr(13) & Chr(10) & "Pellentesque non elit vel magna malesuada porttitor. Fusce sed leo sit amet neque lobortis dapibus. Nullam vitae eros sed dolor viverra congue. Nulla facilisi. Praesent eu est sed nulla bibendum dapibus. Vivamus blandit elit ac ipsum. Cras varius bibendum mauris. Nunc ac orci vitae justo venenatis feugiat. Nullam eget ante. Morbi aliquet quam ut risus. "
            carta += Chr(13) & Chr(10) & Chr(13) & Chr(10) & "Cras ligula purus, auctor in, varius eu, imperdiet sed, turpis. Sed in quam. Aenean nec magna. Sed feugiat dolor. Sed pretium pulvinar nulla. Aenean eget lectus. Integer porta bibendum elit. Vestibulum vitae lorem sed eros vehicula pharetra. Duis eu massa. Sed auctor egestas augue. "
            carta += Chr(13) & Chr(10) & Chr(13) & Chr(10) & "Fusce semper, enim vel ullamcorper accumsan, justo sapien mattis elit, et euismod massa leo quis metus. Proin nibh. Sed vulputate malesuada dui. In aliquet diam eget nunc. Vivamus ut velit. Donec massa. Duis tincidunt odio eget ipsum. Ut ac est in lacus imperdiet egestas. Suspendisse nec erat ut metus sagittis blandit. Nulla leo. Sed pede felis, suscipit et, egestas at, consequat in, augue. Suspendisse sit amet est at arcu auctor eleifend. Sed molestie semper velit. Sed libero. Proin nisi. Duis vehicula sodales turpis. Sed pharetra elit vel mi. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Cras interdum ligula in orci. Ut ac orci eu enim vulputate faucibus. "

            Dim huellartf As New FN.GestionPagos.DN.HuellaContenedorRTFDN(New FN.GestionPagos.DN.ContenedorRTFDN(carta))
            mipago.Talon.HuellaRTF = huellartf

            Dim mitd As New FN.GestionPagos.DN.TalonDocumentoDN
            mitd.NumeroSerie = "1234567890"
            mitd.FechaImpresion = Now.ToLongDateString()
            mitd.Talon = mipago.Talon
            mitd.Talon.ColTalonesImpresos = New FN.GestionPagos.DN.ColTalonDocumentoDN()
            mipago.Talon.ColTalonesImpresos.Add(mitd)

            Dim mipaquete As New FN.GestionPagos.IU.PaqueteImpresion
            mipaquete.ImpresionSilenciosa = False
            mipaquete.TalonDocumento = mitd
            mipaquete.ConfiguracionImpresion = mici
            mipaquete.Prueba = True


            Me.cMarco.Navegar("ImpresionTalon", Me, Me, TipoNavegacion.Modal, mipaquete.GenerarPaquete())

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub
End Class