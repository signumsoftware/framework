
'Imports Framework.DatosNegocio

'<Serializable()> _
'Public Class NotaPolizaDN
'    Inherits Framework.DatosNegocio.EntidadDN

'    Protected mPoliza As HEPolizaDN
'    Protected mPeridoRenocacion As HEPeriodoRenovacionPolizaDN




'    Protected mNota As Framework.Notas.NotasDN.NotaDN

'    <RelacionPropCampoAtribute("mNota")> _
'    Public Property Nota() As Framework.Notas.NotasDN.NotaDN

'        Get
'            Return mNota
'        End Get

'        Set(ByVal value As Framework.Notas.NotasDN.NotaDN)
'            CambiarValorRef(Of Framework.Notas.NotasDN.NotaDN)(value, mNota)

'        End Set
'    End Property







'    <RelacionPropCampoAtribute("mPeridoRenocacion")> _
'    Public Property PeridoRenocacion() As HEPeriodoRenovacionPolizaDN

'        Get
'            Return mPeridoRenocacion
'        End Get

'        Set(ByVal value As HEPeriodoRenovacionPolizaDN)
'            CambiarValorRef(Of HEPeriodoRenovacionPolizaDN)(value, mPeridoRenocacion)

'        End Set
'    End Property








'    <RelacionPropCampoAtribute("mPoliza")> _
'    Public Property Poliza() As HEPolizaDN

'        Get
'            Return mPoliza
'        End Get

'        Set(ByVal value As HEPolizaDN)
'            CambiarValorRef(Of HEPolizaDN)(value, mPoliza)

'        End Set
'    End Property


'    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

'        If mPoliza Is Nothing OrElse PeridoRenocacion Is Nothing Then
'            pMensaje = " Ni la poliza ni el perido de renovación pueden ser nulos "
'            Return EstadoIntegridadDN.Inconsistente

'        End If

'        'If Me.mPeridoRenocacion.Poliza.GUID <> Me.mPoliza.GUID Then
'        '    pMensaje = " Me.mPeridoRenocacion.Poliza.GUID <> Me.mPoliza.GUID "
'        '    Return EstadoIntegridadDN.Inconsistente

'        'End If

'        Return MyBase.EstadoIntegridad(pMensaje)
'    End Function

'    Public Function AsignarPeridoRenovacion(ByVal pPeriodoRenovacionPoliza As PeriodoRenovacionPolizaDN)
'        Me.PeridoRenocacion = New HEPeriodoRenovacionPolizaDN
'        Me.PeridoRenocacion.AsignarEntidadReferida(pPeriodoRenovacionPoliza)
'        Me.PeridoRenocacion.EliminarEntidadReferida()

'        Me.Poliza = New HEPolizaDN()
'        Me.Poliza.AsignarEntidadReferida(pPeriodoRenovacionPoliza.Poliza)
'        Me.Poliza.EliminarEntidadReferida()

'    End Function

'End Class
