Public Class frmListaElementos
    Inherits MotorIU.FormulariosP.FormularioBase

    Friend WithEvents listbox1 As ListBox = Me.ListaResizeable1.ListBox

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        If Not Me.Paquete Is Nothing AndAlso Me.Paquete.Contains("Elementos") Then
            Me.ListaResizeable1.ListBox.Items.AddRange(Me.Paquete("Elementos"))
        End If
    End Sub

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAceptar.Click
        Try
            seleccionar(Me.ListaResizeable1.ListBox.SelectedItem)
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Private Sub seleccionar(ByVal pTelefono As FN.Localizaciones.DN.TelefonoDN)

        If Me.PropiedadesES.TipoControl = PropiedadesControles.TipoControl.Entrada Then
            If pTelefono Is Nothing Then
                Me.cMarco.MostrarAdvertencia("No se ha seleccionado ningún elemento", "Aceptar")
                Exit Sub
            End If
            If Me.Paquete.Contains("ElementoSeleccionado") Then
                Me.Paquete.Remove(Me.Paquete("ElementoSeleccionado"))
            End If
            Me.Paquete.Add("ElementoSeleccionado", pTelefono)
        End If

        Me.Close()

    End Sub

    Private Sub listbox1_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles listbox1.DoubleClick
        Try
            seleccionar(Me.ListaResizeable1.ListBox.SelectedItem)
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Try
            Me.Close()
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub
End Class