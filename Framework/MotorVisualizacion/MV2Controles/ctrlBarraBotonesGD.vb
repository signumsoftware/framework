'Public Class ctrlBarraBotonesGD

'    Public Event ComandoSolicitado(ByVal sender As System.Object, ByVal e As System.EventArgs)

'    Protected mComandoAccioando As MV2DN.ComandoInstancia

'    Public ReadOnly Property ComandoAccioando() As MV2DN.ComandoInstancia
'        Get
'            Return Me.mComandoAccioando
'        End Get

'    End Property

'    Public Sub Poblar(ByVal pColComandoInstancia As MV2DN.ColComandoInstancia)

'        If pColComandoInstancia Is Nothing Then
'            Exit Sub
'        End If


'        For Each comando As MV2DN.ComandoInstancia In pColComandoInstancia
'            Dim tt As New ToolTip

'            Dim boton As New Button
'            ' boton.Text = comando.Map.NombreVis
'            tt.SetToolTip(boton, comando.Map.NombreVis)
'            tt.Active = True
'            boton.Padding = New Padding(3, 0, 3, 0)
'            boton.Margin = New Padding(3, 0, 3, 0)


'            boton.Width = 20
'            boton.Height = 20

'            Dim miimagen As Image = ProveedorImagenes.ObtenerImagen(comando.Map.Ico)
'            boton.Image = miimagen
'            boton.Tag = comando
'            Me.FlowLayoutPanel1.Controls.Add(boton)

'            ' Me.FlowLayoutPanel1.BackColor = Color.Blue
'            AddHandler boton.Click, AddressOf OnComandoSolicitado

'        Next
'        '   Me.FlowLayoutPanel1.Dock = DockStyle.None
'        'Me.FlowLayoutPanel1.Width = 26 * pColComandoInstancia.Count
'        ' Me.Width = Me.FlowLayoutPanel1.Width
'        ' Me.FlowLayoutPanel1.Height = 26
'    End Sub


'    Private Sub OnComandoSolicitado(ByVal sender As System.Object, ByVal e As System.EventArgs)
'        Dim miboton As Windows.Forms.Control = sender


'        mComandoAccioando = miboton.Tag

'        RaiseEvent ComandoSolicitado(Me, Nothing)
'    End Sub


'End Class
