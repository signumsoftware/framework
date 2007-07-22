Public Class frmPlantillaCarta

#Region "atributos"
    Private mControlador As frmPlantillaCartactrl
    Private miPaquete As PaquetePlantillaCarta
#End Region

#Region "inicializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = Me.Controlador

        If Not Me.Paquete Is Nothing Then
            If Me.Paquete.Contains("Paquete") Then
                miPaquete = Me.Paquete("Paquete")
                Me.ctrlPlantillaTextoCarta1.PlantillaCarta = miPaquete.PlantillaCarta
            ElseIf Me.Paquete.Contains("ID") Then
                Me.ctrlPlantillaTextoCarta1.PlantillaCarta = LNC.RecuperarPlantillaCarta(Me.Paquete("ID"))
            End If
        End If
    End Sub
#End Region


#Region "métodos"
    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Try
            If Not Me.Paquete Is Nothing Then
                Me.Paquete.Clear()
            End If
            Me.Close()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdGuardar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdGuardar.Click
        Try
            Dim miplantilla As FN.GestionPagos.DN.PlantillaCartaDN = Nothing
            If Not RecuperarDN(miplantilla) Then
                Exit Sub
            End If
            Using New AuxIU.CursorScope(Cursors.WaitCursor)
                Me.ctrlPlantillaTextoCarta1.PlantillaCarta = Me.mControlador.GuardarPlantilla(miplantilla)
            End Using
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAceptar.Click
        Try
            Dim miplantilla As FN.GestionPagos.DN.PlantillaCartaDN = Nothing
            If Not RecuperarDN(miplantilla) Then
                Exit Sub
            End If
            If miPaquete Is Nothing Then
                miPaquete = New PaquetePlantillaCarta
            End If
            miPaquete.PlantillaCarta = miplantilla
            Me.Paquete.Add("Paquete", miPaquete)
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    ''' <summary>
    ''' Recupera la Dn del control  muestra un mensaje de excepción si hay datos erróneos
    ''' </summary>
    ''' <param name="pPlantilla">Obtiene por referencia la DN</param>
    ''' <returns>true si existe, false si no</returns>
    Private Function RecuperarDN(ByRef pPlantilla As FN.GestionPagos.DN.PlantillaCartaDN) As Boolean
        Dim miplantilla As FN.GestionPagos.DN.PlantillaCartaDN = Me.ctrlPlantillaTextoCarta1.PlantillaCarta
        If miplantilla Is Nothing Then
            MessageBox.Show(Me.ctrlPlantillaTextoCarta1.MensajeError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return False
        End If
        pPlantilla = miplantilla
        Return True
    End Function
#End Region


End Class