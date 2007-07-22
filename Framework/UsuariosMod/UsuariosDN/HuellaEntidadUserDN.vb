#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Clase no utilizada
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class HuellaEntidadUserDN
    Inherits HuellaEntidadTipadaDN(Of EntidadDN)

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pEntidad As EntidadDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional)
        MyBase.New(pEntidad, pRelacionIntegridad)
    End Sub

    Public Sub New(ByVal pEntidad As EntidadDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional, ByVal toStringEntidad As String)
        MyBase.New(pEntidad, pRelacionIntegridad, toStringEntidad)
    End Sub

#End Region

End Class
