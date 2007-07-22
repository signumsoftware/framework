
<Serializable()> _
Public Class AutorizacionTalonDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

#End Region

#Region "Propiedades"

#End Region

#Region "Validaciones"

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

    Public Property Autorizado() As Boolean
        Get

        End Get
        Set(ByVal value As Boolean)

        End Set
    End Property




    Public Property AutorizacionNegocio() As AutorizacionDN
        Get

        End Get
        Set(ByVal value As AutorizacionDN)

        End Set
    End Property

    Public Property AutorizacionContabilidad() As AutorizacionDN
        Get

        End Get
        Set(ByVal value As AutorizacionDN)

        End Set
    End Property

    Public Property AutorizacionFinal() As AutorizacionDN
        Get

        End Get
        Set(ByVal value As AutorizacionDN)

        End Set
    End Property

    Public Property Anulado() As Boolean
        Get

        End Get
        Set(ByVal value As Boolean)

        End Set
    End Property
End Class
