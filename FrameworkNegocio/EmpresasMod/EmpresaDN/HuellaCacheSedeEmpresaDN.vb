#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Huella a sede de empresa con el nombre de la sede de empresa cacheado
''' </summary>
''' <remarks></remarks>
<Serializable()> Public Class HuellaCacheSedeEmpresaDN
    Inherits HETCacheableDN(Of SedeEmpresaDN)

#Region "Atributos"
    Protected mNombreSedeEmpresa As String
    Protected mIdEmpresa As String
    Protected mDireccionSedeEmpresa As FN.Localizaciones.DN.DireccionNoUnicaDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pEntidad As SedeEmpresaDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional)
        MyBase.New(pEntidad, pRelacionIntegridad)
        mDireccionSedeEmpresa = pEntidad.Direccion

    End Sub

    Public Sub New(ByVal pEntidad As SedeEmpresaDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional, ByVal toStringEntidad As String)
        MyBase.New(pEntidad, pRelacionIntegridad, toStringEntidad)
        mDireccionSedeEmpresa = pEntidad.Direccion

    End Sub

#End Region

#Region "Propiedades"

    Public ReadOnly Property NombreSedeEmpresa() As String
        Get
            Return Me.mNombreSedeEmpresa
        End Get
    End Property

    Public ReadOnly Property IDEmpresaPadre() As String
        Get
            Return Me.mIdEmpresa
        End Get
    End Property

    Public ReadOnly Property DireccionSedeEmpresa() As FN.Localizaciones.DN.DireccionNoUnicaDN
        Get
            Return mDireccionSedeEmpresa
        End Get
    End Property

#End Region

#Region "Métodos"

    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN)
        Dim miSedeEmpresa As SedeEmpresaDN

        miSedeEmpresa = pEntidad
        Me.mNombreSedeEmpresa = miSedeEmpresa.Nombre
        Me.mIdEmpresa = miSedeEmpresa.Empresa.ID
        mDireccionSedeEmpresa = miSedeEmpresa.Direccion

        MyBase.AsignarEntidadReferida(miSedeEmpresa)
    End Sub

#End Region

End Class
