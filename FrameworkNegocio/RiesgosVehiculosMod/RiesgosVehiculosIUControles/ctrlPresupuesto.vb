Public Class ctrlPresupuesto
    Inherits MotorIU.ControlesP.BaseControlP

    Private mControlador As RiesgosVehiculos.IU.Controladores.ctrlPresupuesto
    Private mPresupuesto As FN.Seguros.Polizas.DN.PresupuestoDN

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property Presupuesto() As FN.Seguros.Polizas.DN.PresupuestoDN
        Get
            If IUaDN() Then
                Return Me.mPresupuesto
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As FN.Seguros.Polizas.DN.PresupuestoDN)
            Me.mPresupuesto = value
            Me.DNaIU(value)
        End Set
    End Property

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = New RiesgosVehiculos.IU.Controladores.ctrlPresupuesto(Me.Marco, Me)
        Me.Controlador = Me.mControlador
    End Sub

#Region "establecer y rellenar datos"
    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Dim p As FN.Seguros.Polizas.DN.PresupuestoDN = pDN
        If p Is Nothing Then
            Me.lblFechaAnulacion.Text = "-"
            Me.lblFechaAnulacion.Visible = False
            Me.lblfechaAnulacionTitulo.Visible = False
            Me.lblEstado.Text = "-"
            Me.lblFuturoTomador.Text = "-"
            Me.lblEntidadEmisora.Text = "-"
            Me.dtpValidezDesde.Value = Now
            Me.dtpValidezHasta.Value = Now
            Me.ctrlTarifa1.Tarifa = Nothing
            Me.lstDocumentos.Items.Clear()
            Me.lstDocumentos.Enabled = False
        Else
            Me.lblFechaAnulacion.Visible = p.FechaAnulacion <> DateTime.MinValue
            Me.lblfechaAnulacionTitulo.Visible = p.FechaAnulacion <> DateTime.MinValue
            If p.FechaAnulacion <> DateTime.MinValue Then
                'está anulado
                Me.lblEstado.Text = "Anulado"
                Me.lblFechaAnulacion.Text = p.FechaAnulacion.ToShortDateString
            Else
                'no está anulado
                Me.lblEstado.Text = "Activo"
            End If
            Dim ft As FN.Seguros.Polizas.DN.FuturoTomadorDN = p.FuturoTomador
            If Not ft.Tomador Is Nothing Then
                Me.lblFuturoTomador.Text = ft.Tomador.ToString()
            Else
                Me.lblFuturoTomador.Text = ft.ToString()
            End If
            Me.lblEntidadEmisora.Text = p.Emisora.Nombre
            Me.dtpValidezDesde.Value = p.FI
            Me.dtpValidezHasta.Value = p.FF
            Me.ctrlTarifa1.Tarifa = p.Tarifa
            'obtenemos los documentos que haya relacioandos
            Me.lstDocumentos.Items.Clear()
            Dim colDocumentos As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN = Me.mControlador.RecuperarDocumentosAsociados(p.GUID)
            If colDocumentos Is Nothing OrElse colDocumentos.Count = 0 Then
                Me.lstDocumentos.Enabled = False
            Else
                Me.lstDocumentos.Enabled = True
                Me.lstDocumentos.DisplayMember = "TipoDocumento"
                Me.lstDocumentos.Items.AddRange(colDocumentos.ToArray())
            End If
        End If

    End Sub

    Protected Overrides Function IUaDN() As Boolean
        Dim p As FN.Seguros.Polizas.DN.PresupuestoDN
        If Me.mPresupuesto Is Nothing Then
            p = New FN.Seguros.Polizas.DN.PresupuestoDN()
        Else
            p = Me.mPresupuesto.Clone()
        End If
        p.Tarifa = Me.ctrlTarifa1.Tarifa
        If p.Tarifa Is Nothing Then
            Me.MensajeError = "No se ha definido correctamente la Tarifa: " & Me.ctrlTarifa1.MensajeError
            Return False
        End If
        p.FI = Me.dtpValidezDesde.Value
        p.FF = Me.dtpValidezHasta.Value
        Me.mPresupuesto = p
        Return True
    End Function
#End Region

End Class
