Public Class frmSeleccionarTipo
    Dim mlistaTipoYMapVisAsociado As List(Of MV2DN.TipoYMapVisAsociadoDN)
    Private Sub frmSeleccionarTipo_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load



        If Me.Paquete.Contains("LsitaTipoYMapVisAsociadoDN") Then

            mlistaTipoYMapVisAsociado = Me.Paquete("LsitaTipoYMapVisAsociadoDN")
            Dim colmap As New MV2DN.ColInstanciaMapDN

            For Each miTipoYMapVisAsociado As MV2DN.TipoYMapVisAsociadoDN In mlistaTipoYMapVisAsociado
                colmap.Add(miTipoYMapVisAsociado.MapVis)

            Next

            Me.ListBox1.DisplayMember = "NombreVis"
            Me.ListBox1.DataSource = colmap




        End If
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        Dim selecionado As MV2DN.TipoYMapVisAsociadoDN
        For Each miTipoYMapVisAsociado As MV2DN.TipoYMapVisAsociadoDN In mlistaTipoYMapVisAsociado
            If Me.ListBox1.SelectedItem Is miTipoYMapVisAsociado.MapVis Then

                selecionado = miTipoYMapVisAsociado
                Exit For
            End If
        Next

        If Me.Paquete.Contains("TipoYMapVisAsociadoDNSeleccioando") Then
            Me.Paquete("TipoYMapVisAsociadoDNSeleccioando") = selecionado
        Else
            Me.Paquete.Add("TipoYMapVisAsociadoDNSeleccioando", selecionado)
        End If

        Me.Close()
    End Sub
End Class