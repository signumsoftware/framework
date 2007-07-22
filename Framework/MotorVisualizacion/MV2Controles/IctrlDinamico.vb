
Public Interface IctrlDinamico
    Inherits Framework.iu.iucomun.IctrlBasicoDN



    Property DatosControl() As String
    ReadOnly Property Comando() As MV2DN.ComandoInstancia
    ReadOnly Property ElementoVinc() As MV2DN.IVincElemento
    Property IRecuperadorInstanciaMap() As MV2DN.IRecuperadorInstanciaMap
    Property IGestorPersistencia() As MV2DN.IGestorPersistencia
    Property ControlDinamicoSeleccioando() As IctrlDinamico

    Function RecuperarControlDinamico(ByVal pElementoMap As MV2DN.ElementoMapDN) As IctrlDinamico

    Event ControlSeleccionado(ByVal sender As Object, ByVal e As ControlSeleccioandoEventArgs)
    Event ComandoSolicitado(ByVal sender As Object, ByRef autorizado As Boolean)
    Event ComandoEjecutado(ByVal sender As Object, ByVal e As EventArgs)


End Interface

Public Class ControlSeleccioandoEventArgs
    Inherits System.EventArgs

    Public ControlSeleccioando As IctrlDinamico

    Public Sub New()

    End Sub
    Public Sub New(ByVal pControlSeleccioando As IctrlDinamico)
        ControlSeleccioando = pControlSeleccioando
    End Sub

End Class