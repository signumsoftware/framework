Imports Framework.DatosNegocio

<Serializable()> _
Public Class ContactoPersonaDN
    Inherits EntidadDN

    Protected mPersona As PersonaDN
    Protected mContacto As FN.Localizaciones.DN.ContactoDN

#Region "Propiedades"

    Public Property Persona() As PersonaDN
        Get
            Return mPersona
        End Get
        Set(ByVal value As PersonaDN)
            CambiarValorRef(Of PersonaDN)(value, mPersona)
        End Set
    End Property

    Public Property Contacto() As FN.Localizaciones.DN.ContactoDN
        Get
            Return mContacto
        End Get
        Set(ByVal value As FN.Localizaciones.DN.ContactoDN)
            CambiarValorRef(Of FN.Localizaciones.DN.ContactoDN)(value, mContacto)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mPersona Is Nothing Then
            pMensaje = "El objeto persona no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mContacto Is Nothing Then
            pMensaje = "Los datos de contacto no pueden ser nulos"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class


<Serializable()> _
Public Class ColContactoPersonaDN
    Inherits ArrayListValidable(Of ContactoPersonaDN)

End Class