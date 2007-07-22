#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

<Serializable()> _
Public Class HuellaEmpleadoDN
    Inherits HuellaEntidadTipadaDN(Of EmpleadoDN)

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

End Class
