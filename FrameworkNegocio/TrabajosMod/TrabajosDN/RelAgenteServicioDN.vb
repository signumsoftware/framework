Imports Framework.DatosNegocio

<Serializable()> _
Public Class RelAgenteServicioDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mAgente As AgenteDN
    Protected mServicio As ServicioDN

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mAgente")> _
    Public Property Agente() As AgenteDN
        Get
            Return mAgente
        End Get
        Set(ByVal value As AgenteDN)
            CambiarValorRef(Of AgenteDN)(value, mAgente)
        End Set
    End Property

    <RelacionPropCampoAtribute("mServicio")> _
    Public Property Servicio() As ServicioDN
        Get
            Return mServicio
        End Get
        Set(ByVal value As ServicioDN)
            CambiarValorRef(Of ServicioDN)(value, mServicio)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mAgente Is Nothing Then
            pMensaje = "El agente no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mServicio Is Nothing Then
            pMensaje = "El servicio no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mAgente.ColServicios Is Nothing OrElse mAgente.ColServicios.Contiene(mServicio, CoincidenciaBusquedaEntidadDN.Todos) Then
            pMensaje = "El servicio no puede ser realizado por el agente seleccionado"
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class
