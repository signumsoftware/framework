#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

<Serializable()> Public Class HuellaEntidadTipadaDepartamento
    Inherits HuellaEntidadTipadaDN(Of DepartamentoDN)

#Region "Atributos"

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pEntidad As DepartamentoDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional)
        MyBase.New(pEntidad, pRelacionIntegridad)
    End Sub

    Public Sub New(ByVal pEntidad As DepartamentoDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional, ByVal toStringEntidad As String)
        MyBase.New(pEntidad, pRelacionIntegridad, toStringEntidad)
    End Sub

#End Region

#Region "Propiedades"

    Public ReadOnly Property EstadoModificacion() As Framework.DatosNegocio.EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property

#End Region

End Class
