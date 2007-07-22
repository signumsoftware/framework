#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Huella a la agrupación de concesionarios. Esta clase se emplea para mantener la integridad referencial
''' entre los concesionarios y la agrupación de los que forman parte.
''' </summary>
''' <remarks></remarks>
<Serializable()> Public Class HuellaEntidadTipadaDeAgrupacionDeEmpresasDN
    Inherits HuellaEntidadTipadaDN(Of AgrupacionDeEmpresasDN)

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pEntidad As AgrupacionDeEmpresasDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional)
        MyBase.New(pEntidad, pRelacionIntegridad)
    End Sub

    Public Sub New(ByVal pEntidad As AgrupacionDeEmpresasDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional, ByVal toStringEntidad As String)
        MyBase.New(pEntidad, pRelacionIntegridad, toStringEntidad)
    End Sub

#End Region

#Region "Propiedades"

    Public Overloads ReadOnly Property EstadoModificacion() As Framework.DatosNegocio.EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property

#End Region

End Class
