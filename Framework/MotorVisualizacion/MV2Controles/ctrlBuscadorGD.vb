Public Class ctrlBuscadorGD
    Implements IctrlDinamico
    Public Event ComandoEjecutado(ByVal sender As Object, ByVal e As System.EventArgs) Implements IctrlDinamico.ComandoEjecutado
    Public Event ComandoSolicitado(ByVal sender As Object, ByRef autorizado As Boolean) Implements IctrlDinamico.ComandoSolicitado
    Public Event ControlSeleccionado(ByVal sender As Object, ByVal e As ControlSeleccioandoEventArgs) Implements IctrlDinamico.ControlSeleccionado


    Protected mFiltro As MotorBusquedaDN.FiltroDN


    Public Property DN() As Object Implements Framework.IU.IUComun.IctrlBasicoDN.DN
        Get
            Return Me.ctrlBuscadorGenerico21.Filtro
        End Get
        Set(ByVal value As Object)
            Me.ctrlBuscadorGenerico21.Filtro = mFiltro
        End Set
    End Property

    Public Sub DNaIUgd() Implements Framework.IU.IUComun.IctrlBasicoDN.DNaIUgd
        If Me.ctrlBuscadorGenerico21.DataGridViewXT.DatagridView.DataSource Is Nothing Then
            Me.ctrlBuscadorGenerico21.buscar()
        End If
    End Sub

    Public Sub IUaDNgd() Implements Framework.IU.IUComun.IctrlBasicoDN.IUaDNgd

    End Sub

    Public Sub Poblar() Implements Framework.IU.IUComun.IctrlBasicoDN.Poblar

    End Sub

    Public ReadOnly Property Comando() As MV2DN.ComandoInstancia Implements IctrlDinamico.Comando
        Get

        End Get
    End Property


    Public Property ControlDinamicoSeleccioando() As IctrlDinamico Implements IctrlDinamico.ControlDinamicoSeleccioando
        Get

        End Get
        Set(ByVal value As IctrlDinamico)

        End Set
    End Property


    Public Property DatosControl() As String Implements IctrlDinamico.DatosControl
        Get

        End Get
        Set(ByVal value As String)
            'Me.ctrlBuscadorGenerico21.car()
        End Set
    End Property

    Public ReadOnly Property ElementoVinc() As MV2DN.IVincElemento Implements IctrlDinamico.ElementoVinc
        Get

        End Get
    End Property

    Public Property IGestorPersistencia() As MV2DN.IGestorPersistencia Implements IctrlDinamico.IGestorPersistencia
        Get

        End Get
        Set(ByVal value As MV2DN.IGestorPersistencia)

        End Set
    End Property

    Public Property IRecuperadorInstanciaMap() As MV2DN.IRecuperadorInstanciaMap Implements IctrlDinamico.IRecuperadorInstanciaMap
        Get

        End Get
        Set(ByVal value As MV2DN.IRecuperadorInstanciaMap)

        End Set
    End Property

    Public Function RecuperarControlDinamico(ByVal pElementoMap As MV2DN.ElementoMapDN) As IctrlDinamico Implements IctrlDinamico.RecuperarControlDinamico

    End Function
End Class
