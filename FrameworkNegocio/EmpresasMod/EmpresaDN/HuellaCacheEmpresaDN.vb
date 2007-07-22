#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Huella a empresa con el nombre de la empresa cacheado
''' </summary>
''' <remarks></remarks>
<Serializable()> Public Class HuellaCacheEmpresaDN
    Inherits HETCacheableDN(Of EmpresaDN)

#Region "Atributos"
    Protected mNombreEmpresa As String
#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pEntidad As EmpresaDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional)
        MyBase.New(pEntidad, pRelacionIntegridad)
    End Sub

    Public Sub New(ByVal pEntidad As EmpresaDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional, ByVal toStringEntidad As String)
        MyBase.New(pEntidad, pRelacionIntegridad, toStringEntidad)
    End Sub

#End Region

#Region "Propiedades"
    Public ReadOnly Property NombreEmpresa() As String
        Get
            Return Me.mNombreEmpresa
        End Get
    End Property
#End Region

#Region "Métodos"
    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN)
        Me.mNombreEmpresa = pEntidad.Nombre
        MyBase.AsignarEntidadReferida(pEntidad)
    End Sub
#End Region

End Class
