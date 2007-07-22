<Serializable()> _
Public Class AutorizacionDN
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

    Public Property Anulado() As Boolean
        Get

        End Get
        Set(ByVal value As Boolean)

        End Set
    End Property


    Public Property ComentarioAnulacion() As String
        Get

        End Get
        Set(ByVal value As String)

        End Set
    End Property





    Public Property FechaAutorizacion() As Date
        Get

        End Get
        Set(ByVal value As Date)

        End Set
    End Property

    Public Property UsuarioAnulacion() As Object
        Get

        End Get
        Set(ByVal value As Object)

        End Set
    End Property

    Public Property Autorizado() As Boolean
        Get

        End Get
        Set(ByVal value As Boolean)

        End Set
    End Property

    Public Property UsuarioAutorizacion() As Object
        Get

        End Get
        Set(ByVal value As Object)

        End Set
    End Property




    Public Property TipoAutorizacion() As TipoAutorizacion
        Get

        End Get
        Set(ByVal value As TipoAutorizacion)

        End Set
    End Property

End Class
