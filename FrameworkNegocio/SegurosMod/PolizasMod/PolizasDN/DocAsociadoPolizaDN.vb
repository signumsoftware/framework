Imports Framework.DatosNegocio

''' <summary>
''' Esta entidad vincula una póliza con todos los documentos que pueda tener asociados
''' </summary>
''' <remarks></remarks>
'<Serializable()> _
'Public Class DocAsociadoPolizaDN
'    Inherits EntidadDN

'#Region "Atributos"

'    Protected mDocAsociado As Framework.Ficheros.FicherosDN.CajonDocumentoDN
'    Protected mPoliza As PolizaDN
'    Protected mPresupuesto As PresupuestoDN
'#End Region

'#Region "Propiedades"



'    <RelacionPropCampoAtribute("mPresupuesto")> _
'    Public Property Presupuesto() As PresupuestoDN

'        Get
'            Return mPresupuesto
'        End Get

'        Set(ByVal value As PresupuestoDN)
'            CambiarValorRef(Of PresupuestoDN)(value, mPresupuesto)

'        End Set
'    End Property







'    <RelacionPropCampoAtribute("mDocAsociado")> _
'    Public Property DocAsociado() As Framework.Ficheros.FicherosDN.CajonDocumentoDN
'        Get
'            Return mDocAsociado
'        End Get
'        Set(ByVal value As Framework.Ficheros.FicherosDN.CajonDocumentoDN)
'            CambiarValorRef(Of Framework.Ficheros.FicherosDN.CajonDocumentoDN)(value, mDocAsociado)
'        End Set
'    End Property

'    <RelacionPropCampoAtribute("mPoliza")> _
'    Public Property Poliza() As PolizaDN
'        Get
'            Return mPoliza
'        End Get
'        Set(ByVal value As PolizaDN)
'            CambiarValorRef(Of PolizaDN)(value, mPoliza)
'        End Set
'    End Property

'#End Region

'#Region "Métodos"

'    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
'        If mPoliza Is Nothing AndAlso Me.mPresupuesto Is Nothing Then
'            pMensaje = "La póliza y el presupuesto no pueden ser hambas nothing"
'            Return EstadoIntegridadDN.Inconsistente
'        End If

'        If mDocAsociado Is Nothing Then
'            pMensaje = "El documento asociado no puede ser nulo"
'            Return EstadoIntegridadDN.Inconsistente
'        End If

'        Return MyBase.EstadoIntegridad(pMensaje)
'    End Function

'#End Region

'End Class
