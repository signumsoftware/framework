Public Class frmPruebaPlantillaReemplazo
    Private mControlador As frmPruebaPlantillaReemplazoctrl

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = Me.Controlador

        If Not Me.Paquete Is Nothing AndAlso Me.Paquete.Contains("Texto") Then
            Dim milistareemplazos As List(Of FN.GestionPagos.DN.ReemplazosTextoCartasDN) = Me.mControlador.RecuperarTodosReemplazos

            Dim miOrigen As New FN.GestionPagos.DN.OrigenDN()
            Dim mitipo As New FN.GestionPagos.DN.TipoEntidadOrigenDN()
            mitipo.Nombre = "Siniestro de Moto"
            miOrigen.TipoEntidadOrigen = mitipo
            miOrigen.IDEntidad = "09876-54321"

            Dim p As New FN.GestionPagos.DN.PagoDN
            p.Origen = miOrigen
            p.Importe = 1234.5

            Dim pe As New FN.Personas.DN.PersonaFiscalDN
            pe.Persona = New FN.Personas.DN.PersonaDN
            pe.Persona.Nombre = "José"
            pe.Persona.Apellido = "Marmoto Calderilla"
            pe.IdentificacionFiscal = New FN.Localizaciones.DN.NifDN("00000001R")

            p.Destinatario = pe.EntidadFiscalGenerica

            p.Talon = New FN.GestionPagos.DN.TalonDN
            p.Talon.Pago = p
            p.Talon.HuellaRTF = New FN.GestionPagos.DN.HuellaContenedorRTFDN(New FN.GestionPagos.DN.ContenedorRTFDN(Me.Paquete("Texto")))

            Dim miTalonDoc As New FN.GestionPagos.DN.TalonDocumentoDN()
            miTalonDoc.Talon = p.Talon
            miTalonDoc.FechaTalon = New Date(Now.Year, Now.Month, Now.Day)
            miTalonDoc.NumeroSerie = "123456789"

            Dim dirEnvio As New FN.Localizaciones.DN.DireccionNoUnicaDN()
            dirEnvio.CodPostal = ""
            dirEnvio.TipoVia = New FN.Localizaciones.DN.TipoViaDN("Avenida")
            dirEnvio.Via = "de Bruselas, 38"
            dirEnvio.Localidad = New FN.Localizaciones.DN.LocalidadDN("Alcobendas", New FN.Localizaciones.DN.ProvinciaDN("Madrid", New FN.Localizaciones.DN.PaisDN("España")))
            miTalonDoc.Talon.DireccionEnvio = dirEnvio


            For Each reemplazo As FN.GestionPagos.DN.ReemplazosTextoCartasDN In milistareemplazos
                reemplazo.ReemplazarTexto(miTalonDoc)
            Next

            Dim str As String = CType(miTalonDoc.HuellaRTF.EntidadReferida, FN.GestionPagos.DN.ContenedorRTFDN).RTF

            If str.StartsWith("{\rtf1") Then
                'es un rtf
                Me.RichTextBox1.Rtf = str

            Else
                'es texto plano
                Me.RichTextBox1.Text = str
            End If

        End If
    End Sub


    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAceptar.Click
        Try
            Me.Close()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub
End Class