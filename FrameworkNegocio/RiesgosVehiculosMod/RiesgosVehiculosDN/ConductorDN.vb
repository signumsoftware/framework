Imports Framework.DatosNegocio
Imports FN.Personas.DN

<Serializable()> _
Public Class ConductorDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mPersona As PersonaDN
    Protected mFechaCarnet As Date

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("Persona")> _
    Public Property Persona() As PersonaDN
        Get
            Return mPersona
        End Get
        Set(ByVal value As PersonaDN)
            CambiarValorRef(Of PersonaDN)(value, mPersona)
        End Set
    End Property

    Public Property FechaCarnet() As Date
        Get
            Return mFechaCarnet
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaCarnet)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarPersona(ByRef mensaje As String, ByVal persona As PersonaDN) As Boolean
        If persona Is Nothing OrElse persona.NIF Is Nothing OrElse String.IsNullOrEmpty(persona.NIF.Codigo) Then
            mensaje = "Un conductor debe tener a una persona con un NIF válido"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarPersona(pMensaje, mPersona) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        Me.mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class


<Serializable()> _
Public Class ColConductorDN
    Inherits ArrayListValidable(Of ConductorDN)

End Class
