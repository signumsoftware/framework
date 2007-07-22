#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Huella a empleado con el nombre del empleado cacheado
''' </summary>
''' <remarks></remarks>
<Serializable()> Public Class HuellaCacheEmpleadoDN
    Inherits HETCacheableDN(Of EmpleadoDN)

#Region "Atributos"
    Protected mNombreEmpleado As String
    Protected mIdSedeEmpresa As String
#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pEntidad As EmpleadoDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional)
        MyBase.New(pEntidad, pRelacionIntegridad)
    End Sub

    Public Sub New(ByVal pEntidad As EmpleadoDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional, ByVal toStringEntidad As String)
        MyBase.New(pEntidad, pRelacionIntegridad, toStringEntidad)
    End Sub

#End Region

#Region "Propiedades"
    Public ReadOnly Property NombreEmpleado() As String
        Get
            Return Me.mNombreEmpleado
        End Get
    End Property

    Public ReadOnly Property IDSedeEmpresaPadre() As String
        Get
            Return Me.mIdSedeEmpresa
        End Get
    End Property

#End Region

#Region "Métodos"

    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN)
        Dim miEmpleado As EmpleadoDN
        miEmpleado = pEntidad

        Me.mIdSedeEmpresa = miEmpleado.SedeEmpresa.ID
        Me.mNombreEmpleado = miEmpleado.NombreYApellidos
        MyBase.AsignarEntidadReferida(miEmpleado)
    End Sub

#End Region

End Class
